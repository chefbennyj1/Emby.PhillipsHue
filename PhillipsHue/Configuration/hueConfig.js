define(["loading", "dialogHelper", "emby-checkbox", "emby-select", "emby-input", "alert"],
    function (loading, dialogHelper, alert) {

        var pluginId = "941C5E40-8CE3-47D8-847F-9D67ACB2BDB5";
       
        function openIpDialog(view) {
            var dlg = dialogHelper.createDialog({
                size: "medium-tall",
                removeOnClose: !0,
                scrollY: !1
            });

            dlg.classList.add("formDialog");
            dlg.classList.add("ui-body-a");
            dlg.classList.add("background-theme-a");
            dlg.classList.add("newSubscription");
            dlg.style = "max-width:65%;";

            var html = "";
            html += '<div class="formDialogHeader" style="display:flex">';
            html += '<button is="paper-icon-button-light" class="btnCloseDialog autoSize paper-icon-button-light" tabindex="-1"><i class="md-icon"></i></button><h3 class="formDialogHeaderTitle">Phillips Hue Ip</h3>';
            html += '</div>';

            html += '<div class="formDialogContent" style="margin:2em; height:35em">';
            html += '<div class="dialogContentInner dialog-content-centered scrollY" style="height:35em;">';

            html += '<div class="inputContainer" style="display: flex; align-items: center;">';
            html += '<div style="flex-grow:1;">';
            html += '<label class="inputLabel" for="threshold">Phillips Hue Ip Address</label>';
            html += '<input is="emby-input" type="text" id="ipAddress" name="threshold" placeholder="Ip Address" />';
            html += '</div>';
            html += '</div>';

            html += '<div class="formDialogFooter" style="margin:2em; padding-top:2%;">';
            html += '<button id="okButton" is="emby-button" type="submit" class="raised button-submit block formDialogFooterItem emby-button">Ok</button>';
            html += '</div>';
            html += '</div>';
            html += '</div>';

            dlg.innerHTML = html;
            dialogHelper.open(dlg);
            
            dlg.querySelector('#okButton').addEventListener('click',
                () => {
                    var ip = dlg.querySelector('#ipAddress').value;
                    createUserTokenAndConnect(ip, view);
                    dialogHelper.close(dlg);
                });
            dlg.querySelector('.btnCloseDialog').addEventListener('click',
                () => {
                    dialogHelper.close(dlg);
                });
        }

        function removeOptionsFromSelect(selectbox) {
            if (selectbox.options.length > 0) {
                for (var i = selectbox.options.length - 1; i >= 0; i--) {
                    selectbox.remove(i);
                }
            }
        }

        function getSavedOptionProfileHtml(optionProfile) {
            var html = "";
            html += '<div class="listItem listItem-border optionProfile" data-device="' +
                optionProfile.DeviceName +
                '" data-appName="' +
                optionProfile.AppName +
                '">';
            html += '<div class="listItemBody"> ';
            html += '<div class="flex">'
            html += '<img style="max-width:2em; padding-right:1em" src="' + deviceNameImage(optionProfile.DeviceName, optionProfile.AppName) + '"/>';
            html += '<div class="listItemBodyText">' + optionProfile.DeviceName + ' ' + optionProfile.AppName + '</div>';
            html += '</div>';
            html += '</div>';
            html +=
                '<i class="md-icon deleteProfile" style="font-size:1.5em">delete</i>';
            html += '</div>';
            return html;
        } 

        function loadConfig(view) {
            ApiClient.getPluginConfiguration(pluginId).then(
                (config) => {
                    loadPageData(view, config);
                    loading.hide();
                });
        }

        function createUserTokenAndConnect(ip, view) {
            
            ApiClient.getJSON(ApiClient.getUrl("GetUserToken?ipAddress=" + encodeURIComponent(ip))).then((result) => {

                //var json = JSON.parse(result);
                    //We have an error
                if (result[0].error) {
                    if (result[0].error.type == 101) {  //User didn't press the link button - remind them with alert
                        require(['alert'], function (alert) {
                            alert({
                                title: 'Please press the discover button on your phillips hue, and try again.',
                                text: ''
                            });
                        });

                    }
                    else if (result[0].error.type == 1) {
                        require(['alert'],
                            function(alert) {
                                alert({
                                    title: 'Unauthorized User Error',
                                    text: 'The user name is invalid.'
                                });
                            });
                    } else {
                        require(['alert'], function (alert) {
                            alert({
                                title: 'Connection Error',
                                text: 'Please report this error code: ' + result[0].error.type
                            });
                        });
                    }

                    //This is anyother error - reset the UI
                    view.querySelector('#BridgeStatus').innerText = "Not Connected";
                    var statusIcon = view.querySelector('#BridgeStatusIcon');
                    statusIcon.innerHTML = "error";
                    statusIcon.style.color = "slategray";
                    loading.hide();

                    return;
                }

                    //We are good to go!
                if (result[0].success) {
                        ApiClient.getPluginConfiguration(pluginId).then((config) => {
                            config.BridgeIpAddress = ip;
                            config.UserToken = result[0].success.username;
                            ApiClient.updatePluginConfiguration(pluginId, config).then((result) => {
                                Dashboard.processPluginConfigurationUpdateResult(result);  //Show config update success popup 
                                //Switch the button text back to 'discover'
                                view.querySelector('#BridgeStatus').innerText = "Connection Ok";
                                var statusIcon = view.querySelector('#BridgeStatusIcon');
                                statusIcon.innerHTML = "check_box";
                                statusIcon.style.color = "green";
                                loading.hide();
                                require(['alert'], function (alert) {
                                    alert({
                                        title: 'Congratulations!',
                                        text: 'Emby Server is now connected to Phillips Hue.'
                                    });
                                });
                               
                                ApiClient.getPluginConfiguration(pluginId).then((config) => {
                                    loadPageData(view, config);
                                });
                            });
                        });
                    }
                });
        }

        
        function deviceNameImage(deviceName, AppName) {
            if (deviceName.toLowerCase().indexOf("safari") > -1 || AppName.toLowerCase().indexOf("safari") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/safari.png";
            if (deviceName.toLowerCase().indexOf("kodi") > -1 || (AppName.toLowerCase().indexOf("kodi") > -1))
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/kodi.png";
            if (deviceName.toLowerCase().indexOf("ps4") > -1 || AppName.toLowerCase().indexOf("ps4") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/ps4.png";
            if (deviceName.toLowerCase().indexOf("roku") > -1 || AppName.toLowerCase().indexOf("roku") > -1)
                return  "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/roku.jpg";
            if (deviceName.toLowerCase().indexOf("xbox") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/xboxone.png";
            if (deviceName.toLowerCase().indexOf("chrome") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/chrome.png";
            if (deviceName.toLowerCase().indexOf("firefox") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/firefox.png";
            if (AppName.toLowerCase().indexOf("android") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/android.png";
            if (deviceName.toLowerCase().indexOf("edge") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/edge.png";
            if (deviceName.toLowerCase().indexOf("amazon") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/amazon.png";
            if (deviceName.toLowerCase().indexOf("apple") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/appletev.png";
            if (deviceName.toLowerCase().indexOf("windows") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/windowsrt.png";
            if (deviceName.toLowerCase().indexOf("dlna") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/dlna.png";
            if (deviceName.toLowerCase().indexOf("chromecast") > -1)
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/chromecast.png";
            return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/logoicon.png";
        }     

        function openDeviceEditorDialog(device, app, config, view) {
            var dlg = dialogHelper.createDialog({
                size: "medium-tall",
                removeOnClose: !0,
                scrollY: !1
            });

            dlg.classList.add("formDialog");
            dlg.classList.add("ui-body-a");
            dlg.classList.add("background-theme-a");
            dlg.classList.add("newSubscription");
            dlg.style = "max-width:65%;";

            var html = "";
            html += '<div class="formDialogHeader" style="display:flex">';
            html += '<button is="paper-icon-button-light" class="btnCloseDialog autoSize paper-icon-button-light" tabindex="-1"><i class="md-icon"></i></button><h3 class="formDialogHeaderTitle">Editing ' + device + ' ' + app + '</h3>';
            html += '</div>';

            html += '<div class="formDialogContent" style="margin:2em; height:35em">';
            html += '<div class="dialogContentInner dialog-content-centered scrollY" style="height:35em;">';


            html += '<div style="padding-bottom:3em"> ';
            html += '<h1 style="display: flex; align-items: center; margin: auto;">Device Control</h1> ';
            
            html += '<h1 style="display: flex; align-items: center; margin: auto;">Scenes & Events</h1> ';
            
            html += '<div class="sectionTitleContainer align-items-center">';
            html += '<h2 class="sectionTitle"><span>Movies</span></h2> ';
            html += '<p>These scene will be set when movies start, stop or resume playing.</p> ';
            html += '</div> ';

            html += '<!--Emby Events-->';
            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="MoviesPlaybackStarted">Movies Playback Started Scene:</label>';
            html += '<select is="emby-select" type="text" id="MoviesPlaybackStarted" name="MoviesPlaybackStarted" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>  ';
            html += '</div> ';
            html += '</div> ';

            html += '<div class="selectContainer">';
            html += '<label class="selectLabel" for="MoviesPlaybackStopped">Movies Playback Stopped Scene:</label>  ';
            html += '<select is="emby-select" type="text" id="MoviesPlaybackStopped" name="MoviesPlaybackStopped" class="emby-input"></select>  ';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>  ';
            html += '</div>  ';
            html += '</div>  ';

            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="MoviesPlaybackPaused">Movies Playback Paused Scene:</label>  ';
            html += '<select is="emby-select" type="text" id="MoviesPlaybackPaused" name="MoviesPlaybackPaused" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer"> ';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div>';
            html += '</div>';

            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="MoviesPlaybackUnPaused">Movies Playback UnPaused Scene:</label>';
            html += '<select is="emby-select" type="text" id="MoviesPlaybackUnPaused" name="MoviesPlaybackUnPaused" class="emby-input"></select>';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div> ';
            html += '</div>';

            html += '<div class="selectContainer">';
            html += '<label class="selectLabel" for="MediaItemCredits">Movies Credit Scene:</label>';
            html += '<select is="emby-select" type="text" id="MediaItemCredits" name="MediaItemCredits" class="emby-input"></select>';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div>';
            html += '</div>';

            html += '<div class="inputContainer">';
            html += '<label class="inputLabel inputLabelUnfocused" for="creditLength">Movies Credit Length</label>';
            html += '<input is="emby-input" type="number" id="creditLength" label="Media Item Credit Length" class="emby-input">';
            html += '<div class="fieldDescription">Estimated time to schedule the credit scene before the end of the media item in seconds.</div>';
            html += '</div>';

            html += '<div class="sectionTitleContainer align-items-center"> ';
            html += '<h2 class="sectionTitle"><span>Series</span></h2> ';
            html += '<p>These scene will be set when series/seasons/episodes start, stop or resume playing.</p>  ';
            html += '</div> ';

            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="MoviesPlaybackStarted">Tv Series Playback Started Scene:</label>';
            html += '<select is="emby-select" type="text" id="TvPlaybackStarted" name="MoviesPlaybackStarted" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer"> ';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i> ';
            html += '</div> ';
            html += '</div> ';

            html += '<div class="selectContainer">';
            html += '<label class="selectLabel" for="MoviesPlaybackStopped">Tv Series Playback Stopped Scene:</label> ';
            html += '<select is="emby-select" type="text" id="TvPlaybackStopped" name="MoviesPlaybackStopped" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>  ';
            html += '</div>';
            html += '</div>';

            html += '<div class="selectContainer">';
            html += '<label class="selectLabel" for="TvPlaybackPaused">Tv Series Playback Paused Scene:</label>';
            html += '<select is="emby-select" type="text" id="TvPlaybackPaused" name="TvPlaybackPaused" class="emby-input"></select>';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>  ';
            html += '</div> ';
            html += '</div>';

            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="TvPlaybackUnPaused">Tv Series Playback UnPaused Scene:</label>';
            html += '<select is="emby-select" type="text" id="TvPlaybackUnPaused" name="TvPlaybackUnPaused" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer"> ';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i> ';
            html += '</div> ';
            html += '</div>';

            html += '<div class="sectionTitleContainer align-items-center">';
            html += '<h2 class="sectionTitle"><span>Live Tv</span></h2> ';
            html += '<p>These scene will be set when live tv streams start, stop or resume playing.</p> ';
            html += '</div> ';

            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="LiveTvPlaybackStarted">LiveTv Playback Started Scene:</label>';
            html += '<select is="emby-select" type="text" id="LiveTvPlaybackStarted" name="LiveTvPlaybackStarted" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div> ';
            html += '</div>';

            html += '<div class="selectContainer"> ';
            html += '<label class="selectLabel" for="LiveTvPlaybackStopped">LiveTv Playback Stopped Scene:</label> ';
            html += '<select is="emby-select" type="text" id="LiveTvPlaybackStopped" name="LiveTvPlaybackStopped" class="emby-input"></select>';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div>';
            html += '</div>';

            html += '<div class="selectContainer">';
            html += '<label class="selectLabel" for="LiveTvPlaybackPaused">LiveTv Playback Paused Scene:</label>';
            html += '<select is="emby-select" type="text" id="LiveTvPlaybackPaused" name="LiveTvPlaybackPaused" class="emby-input"></select> ';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div>';
            html += '</div>';

            html += '<div class="selectContainer">';
            html += '<label class="selectLabel" for="LiveTvPlaybackUnPaused">LiveTv Playback UnPaused Scene:</label>';
            html += '<select is="emby-select" type="text" id="LiveTvPlaybackUnPaused" name="LiveTvPlaybackUnPaused" class="emby-input"></select>';
            html += '<div class="selectArrowContainer">';
            html += '<div style="visibility: hidden;">0</div><i class="selectArrow md-icon"></i>';
            html += '</div>';
            html += '</div> ';
           
            
            html += '<div class="sectionTitleContainer align-items-center">';
            html += '<h2 class="sectionTitle"><span>Schedule Time</span></h2> ';
            html += '<p>Scenes will be ignored before a certain time of day.</p> ';
            html += '</div> ';

            html += '<div class="inputContainer">';
            html += '<label class="inputLabel inputLabelUnfocused" for="scheduleTime">Schedule Time to trigger events</label>';
            html += '<input is="emby-input" type="time" id="scheduleTime" label="Schedule Time to trigger events" class="emby-input">';
            html += '<div class="fieldDescription">Determines when scenes should be run by Emby. Between the time above and 4 AM.</div>';
            html += '</div>';
            
            html += '<div class="formDialogFooter" style="margin:2em; padding-top:2%;">';
            html += '<button id="saveButton" is="emby-button" type="submit" class="raised button-submit block formDialogFooterItem emby-button">Save</button>';
            html += '</div>';

            html += '</div>';
            html += '</div>';

            dlg.innerHTML = html;
            dialogHelper.open(dlg);
               
            var moviePlaybackStartedSelect   = dlg.querySelector('#MoviesPlaybackStarted');
            var moviePlaybackStoppedSelect   = dlg.querySelector('#MoviesPlaybackStopped');
            var moviePlaybackPausedSelect    = dlg.querySelector('#MoviesPlaybackPaused');
            var moviePlaybackUnPausedSelect  = dlg.querySelector('#MoviesPlaybackUnPaused');

            var seriesPlaybackStartedSelect  = dlg.querySelector('#TvPlaybackStarted');
            var seriesPlaybackStoppedSelect  = dlg.querySelector('#TvPlaybackStopped');
            var seriesPlaybackPausedSelect   = dlg.querySelector('#TvPlaybackPaused');
            var seriesPlaybackUnPausedSelect = dlg.querySelector('#TvPlaybackUnPaused');

            var liveTvPlaybackStartedSelect  = dlg.querySelector('#LiveTvPlaybackStarted');
            var liveTvPlaybackStoppedSelect  = dlg.querySelector('#LiveTvPlaybackStopped');
            var liveTvPlaybackPausedSelect   = dlg.querySelector('#LiveTvPlaybackPaused');
            var liveTvPlaybackUnPausedSelect = dlg.querySelector('#LiveTvPlaybackUnPaused');

            var schedule                     = dlg.querySelector('#scheduleTime');

            var mediaItemCreditLength        = dlg.querySelector('#creditLength');
            var mediaItemCreditsSelect       = dlg.querySelector('#MediaItemCredits');


            removeOptionsFromSelect(mediaItemCreditsSelect);
            
            // Append an empty option for the purpose of the user selecting no scene events.
            mediaItemCreditsSelect.innerHTML            += ('<option value=""></option>');
            
            removeOptionsFromSelect(moviePlaybackStartedSelect);
            removeOptionsFromSelect(moviePlaybackStoppedSelect);
            removeOptionsFromSelect(moviePlaybackPausedSelect);
            removeOptionsFromSelect(moviePlaybackUnPausedSelect);
            // Append an empty option for the purpose of the user selecting no scene events.
            moviePlaybackStartedSelect.innerHTML  += ('<option value=""></option>');
            moviePlaybackStoppedSelect.innerHTML  += ('<option value=""></option>');
            moviePlaybackPausedSelect.innerHTML   += ('<option value=""></option>');
            moviePlaybackUnPausedSelect.innerHTML += ('<option value=""></option>');

            removeOptionsFromSelect(seriesPlaybackStartedSelect);
            removeOptionsFromSelect(seriesPlaybackStoppedSelect);
            removeOptionsFromSelect(seriesPlaybackPausedSelect);
            removeOptionsFromSelect(seriesPlaybackUnPausedSelect);
            // Append an empty option for the purpose of the user selecting no scene events.
            seriesPlaybackStartedSelect.innerHTML  += ('<option value=""></option>');
            seriesPlaybackStoppedSelect.innerHTML  += ('<option value=""></option>');
            seriesPlaybackPausedSelect.innerHTML   += ('<option value=""></option>');
            seriesPlaybackUnPausedSelect.innerHTML += ('<option value=""></option>');

            removeOptionsFromSelect(liveTvPlaybackStartedSelect);
            removeOptionsFromSelect(liveTvPlaybackStoppedSelect);
            removeOptionsFromSelect(liveTvPlaybackPausedSelect);
            removeOptionsFromSelect(liveTvPlaybackUnPausedSelect);
            // Append an empty option for the purpose of the user selecting no scene events.
            liveTvPlaybackStartedSelect.innerHTML  += ('<option value=""></option>');
            liveTvPlaybackStoppedSelect.innerHTML  += ('<option value=""></option>');
            liveTvPlaybackPausedSelect.innerHTML   += ('<option value=""></option>');
            liveTvPlaybackUnPausedSelect.innerHTML += ('<option value=""></option>');

            
            ApiClient.getJSON(ApiClient.getUrl("GetScenes")).then(
                (result) => {

                    if (result.Scenes) {
                        var sceneObjects = JSON.parse(result.Scenes);
                        for (var i = 0; i <= Object.keys(sceneObjects).length - 1; i++) {

                            moviePlaybackStartedSelect.innerHTML   += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            moviePlaybackStoppedSelect.innerHTML   += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            moviePlaybackPausedSelect.innerHTML    += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            moviePlaybackUnPausedSelect.innerHTML  += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            seriesPlaybackStartedSelect.innerHTML  += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            seriesPlaybackStoppedSelect.innerHTML  += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            seriesPlaybackPausedSelect.innerHTML   += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            seriesPlaybackUnPausedSelect.innerHTML += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            liveTvPlaybackStartedSelect.innerHTML  += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            liveTvPlaybackStoppedSelect.innerHTML  += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            liveTvPlaybackPausedSelect.innerHTML   += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            liveTvPlaybackUnPausedSelect.innerHTML += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                            mediaItemCreditsSelect.innerHTML       += ('<option value="' + Object.keys(sceneObjects)[i] + '">' + Object.values(Object.values(sceneObjects)[i])[0] + '</option>');
                        };
                           
                        if (config.SavedHueEmbyProfiles) {
                            config.SavedHueEmbyProfiles.forEach(option => { 
                                if (option.DeviceName == device && option.AppName == app) {
                                    moviePlaybackStartedSelect.value   = option.MoviesPlaybackStarted  || "";
                                    moviePlaybackStoppedSelect.value   = option.MoviesPlaybackStopped  || "";
                                    moviePlaybackPausedSelect.value    = option.MoviesPlaybackPaused   || "";
                                    moviePlaybackUnPausedSelect.value  = option.MoviesPlaybackUnPaused || "";
                                    seriesPlaybackStartedSelect.value  = option.TvPlaybackStarted      || "";
                                    seriesPlaybackStoppedSelect.value  = option.TvPlaybackStopped      || "";
                                    seriesPlaybackPausedSelect.value   = option.TvPlaybackPaused       || "";
                                    seriesPlaybackUnPausedSelect.value = option.TvPlaybackUnPaused     || "";
                                    liveTvPlaybackStartedSelect.value  = option.LiveTvPlaybackStarted  || "";
                                    liveTvPlaybackStoppedSelect.value  = option.LiveTvPlaybackStopped  || "";
                                    liveTvPlaybackPausedSelect.value   = option.LiveTvPlaybackPaused   || "";
                                    liveTvPlaybackUnPausedSelect.value = option.LiveTvPlaybackUnPaused || "";
                                    schedule.value                     = option.Schedule               || "";
                                    mediaItemCreditLength.value        = option.MediaItemCreditLength  || "";
                                    mediaItemCreditsSelect.value       = option.MediaItemCredits       || "";

                                }
                            });
                        }
                    }
                });
                    
            dlg.querySelector('#saveButton').addEventListener('click',
                () => {
                    loading.show();
                    var moviePlaybackStartedSelect   = dlg.querySelector('#MoviesPlaybackStarted');
                    var moviePlaybackStoppedSelect   = dlg.querySelector('#MoviesPlaybackStopped');
                    var moviePlaybackPausedSelect    = dlg.querySelector('#MoviesPlaybackPaused');
                    var moviePlaybackUnPausedSelect  = dlg.querySelector('#MoviesPlaybackUnPaused');

                    var seriesPlaybackStartedSelect  = dlg.querySelector('#TvPlaybackStarted');
                    var seriesPlaybackStoppedSelect  = dlg.querySelector('#TvPlaybackStopped');
                    var seriesPlaybackPausedSelect   = dlg.querySelector('#TvPlaybackPaused');
                    var seriesPlaybackUnPausedSelect = dlg.querySelector('#TvPlaybackUnPaused');

                    var liveTvPlaybackStartedSelect  = dlg.querySelector('#LiveTvPlaybackStarted');
                    var liveTvPlaybackStoppedSelect  = dlg.querySelector('#LiveTvPlaybackStopped');
                    var liveTvPlaybackPausedSelect   = dlg.querySelector('#LiveTvPlaybackPaused');
                    var liveTvPlaybackUnPausedSelect = dlg.querySelector('#LiveTvPlaybackUnPaused');

                    var schedule                     = dlg.querySelector('#scheduleTime');

                    var mediaItemCreditLength        = dlg.querySelector('#creditLength');
                    var mediaItemCreditsSelect       = dlg.querySelector('#MediaItemCredits');

                    var newSavedHueEmbyOptions = {
                        AppName: app,
                        DeviceName: device,
                        MoviesPlaybackStarted:
                            moviePlaybackStartedSelect.options[moviePlaybackStartedSelect.selectedIndex >= 0
                                ? moviePlaybackStartedSelect.selectedIndex
                                : 0].value,
                        MoviesPlaybackStopped:
                            moviePlaybackStoppedSelect.options[moviePlaybackStoppedSelect.selectedIndex >= 0
                                ? moviePlaybackStoppedSelect.selectedIndex
                                : 0].value,
                        MoviesPlaybackPaused:
                            moviePlaybackPausedSelect.options[moviePlaybackPausedSelect.selectedIndex >= 0
                                ? moviePlaybackPausedSelect.selectedIndex
                                : 0].value,
                        MoviesPlaybackUnPaused:
                            moviePlaybackUnPausedSelect.options[moviePlaybackUnPausedSelect.selectedIndex >= 0
                                ? moviePlaybackUnPausedSelect.selectedIndex
                                : 0].value,
                        TvPlaybackStarted:
                            seriesPlaybackStartedSelect.options[seriesPlaybackStartedSelect.selectedIndex >= 0
                                ? seriesPlaybackStartedSelect.selectedIndex
                                : 0].value,
                        TvPlaybackStopped:
                            seriesPlaybackStoppedSelect.options[seriesPlaybackStoppedSelect.selectedIndex >= 0
                                ? seriesPlaybackStoppedSelect.selectedIndex
                                : 0].value,
                        TvPlaybackPaused:
                            seriesPlaybackPausedSelect.options[seriesPlaybackPausedSelect.selectedIndex >= 0
                                ? seriesPlaybackPausedSelect.selectedIndex
                                : 0].value,
                        TvPlaybackUnPaused:
                            seriesPlaybackUnPausedSelect.options[seriesPlaybackUnPausedSelect.selectedIndex >= 0
                                ? seriesPlaybackUnPausedSelect.selectedIndex
                                : 0].value,
                        LiveTvPlaybackStarted:
                            liveTvPlaybackStartedSelect.options[liveTvPlaybackStartedSelect.selectedIndex >= 0
                                ? liveTvPlaybackStartedSelect.selectedIndex
                                : 0].value,
                        LiveTvPlaybackStopped:
                            liveTvPlaybackStoppedSelect.options[liveTvPlaybackStoppedSelect.selectedIndex >= 0
                                ? liveTvPlaybackStoppedSelect.selectedIndex
                                : 0].value,
                        LiveTvPlaybackPaused:
                            liveTvPlaybackPausedSelect.options[liveTvPlaybackPausedSelect.selectedIndex >= 0
                                ? liveTvPlaybackPausedSelect.selectedIndex
                                : 0].value,
                        LiveTvPlaybackUnPaused:
                            liveTvPlaybackUnPausedSelect.options[liveTvPlaybackUnPausedSelect.selectedIndex >= 0
                                ? liveTvPlaybackUnPausedSelect.selectedIndex
                                : 0].value,
                        MediaItemCredits:
                            mediaItemCreditsSelect.options[mediaItemCreditsSelect.selectedIndex >= 0
                                ? mediaItemCreditsSelect.selectedIndex
                                : 0].value,
                        Schedule: schedule.value,
                        MediaItemCreditLength : mediaItemCreditLength.value
                }

                    ApiClient.getPluginConfiguration(pluginId).then((config) => {
                        var listSavedOptions = [];
                        if (config.SavedHueEmbyProfiles) {
                            config.SavedHueEmbyProfiles.forEach(option => {
                                if (option.DeviceName != newSavedHueEmbyOptions.DeviceName) {
                                    listSavedOptions.push(option);
                                } 
                            });
                        }

                        listSavedOptions.push(newSavedHueEmbyOptions)

                        config.SavedHueEmbyProfiles = listSavedOptions;

                        ApiClient.updatePluginConfiguration(pluginId, config).then((result) => {
                            Dashboard.processPluginConfigurationUpdateResult(result);
                            loadPageData(view, config);
                            loading.hide();
                            dialogHelper.close(dlg);
                        }); 
                    });
                    
                });

            dlg.querySelector('.btnCloseDialog').addEventListener('click',
                () => {
                    dialogHelper.close(dlg);
                });
            
        }

        function loadPageData(view, config) {

            var btnImage = view.querySelector('#buttonPress');
            ApiClient.getJSON(ApiClient.getUrl("PhillipsHueButtonPressPng")).then((result) => {
                btnImage.style.backgroundImage =
                    "url('data:image/png;base64," + result.image + "')";
            });
            //Fill the Device List - This is a sorted list without duplications
            var embyDeviceList = view.querySelector('#selectEmbyDevice');
            embyDeviceList.innerHTML = "";
            ApiClient.getJSON(ApiClient.getUrl("EmbyDeviceList")).then(
                (devices) => {
                    devices.forEach(
                        (device) => {
                            embyDeviceList.innerHTML +=
                                ('<option value="' + device.Name + '" data-app="' + device.AppName + '" data-name="' + device.Name + '">' + device.Name + ' - ' + device.AppName + '</option>');
                        });
                });

            if (config.SavedHueEmbyProfiles) {
                var options = view.querySelector('#savedOptions');
                options.innerHTML = "";
                config.SavedHueEmbyProfiles.forEach(option => {
                    options.innerHTML += getSavedOptionProfileHtml(option);
                });

                view.querySelectorAll('.optionProfile').forEach(item => {
                    item.addEventListener('click',
                        (e) => {
                            var listItem = e.target.closest('.optionProfile');
                            if (e.target.innerText == ("delete")) {
                                ApiClient.getPluginConfiguration(pluginId).then((config) => {
                                    var listSavedOptions = [];
                                    if (config.SavedHueEmbyProfiles) {
                                        config.SavedHueEmbyProfiles.forEach(option => {
                                            if (option.DeviceName != listItem.dataset.device &&
                                                options.AppName   != listItem.dataset.appname) {
                                                listSavedOptions.push(option);
                                            }
                                        });
                                    }

                                    config.SavedHueEmbyProfiles = listSavedOptions;

                                    ApiClient.updatePluginConfiguration(pluginId, config).then((result) => {
                                        Dashboard.processPluginConfigurationUpdateResult(result);
                                        loadPageData(view, config);
                                        loading.hide();
                                    });

                                });
                            } else {
                                if (e.target.closest('div > .optionProfile')) {
                                    openDeviceEditorDialog(listItem.dataset.device,
                                        listItem.dataset.appname,
                                        config,
                                        view);
                                }
                            }
                        });
                });
            }


            if (config.BridgeIpAddress) {

                if (config.UserToken) { //If we have a user token then we can make requests to the Hue bridge
                    var statusIcon = view.querySelector('#BridgeStatusIcon');
                    view.querySelector('#BridgeStatus').innerText = "Connection Ok";
                    statusIcon.innerHTML = "check_box";
                    statusIcon.style.color = "green";
                    view.querySelector('.hueOptions').classList.remove('hide');
                    
                }
                
                //HTTPS check box state from config.
                view.querySelector('#chkHTTPS').checked = config.IsSecureConnection;
                
            }
        }

        return function (view) {
            view.addEventListener('viewshow',
                () => {

                    loadConfig(view);

                    view.querySelector('#chkHTTPS').addEventListener('change', (element) => {
                        ApiClient.getPluginConfiguration(pluginId).then(
                            (config) => {
                                config.IsSecureConnection = element.checked;
                            });
                    });
                    
                    
                    var statusIcon = view.querySelector('#BridgeStatusIcon');

                    //Let's discover the bridge
                    view.querySelector('#discoverBridge').addEventListener('click',
                        () => {
                            loading.show();

                            view.querySelector('#BridgeStatus').innerText = "Starting discovery...";
                            statusIcon.innerHTML = "wifi";
                            statusIcon.style.color = "lightblue"; 

                            try {

                                ApiClient.getJSON(ApiClient.getUrl("DiscoverPhillipsHue")).then((result) => {
                                    
                                    if (result) {
                                        var data = JSON.parse(result.BridgeData);
                                        if (data.length > 0) {
                                            var ip = data[0].internalipaddress;
                                            createUserTokenAndConnect(ip, view);
                                        } else {
                                           openIpDialog(view);
                                        }
                                    } 
                                
                                    
                                    loading.hide();
                                    
                                });

                            } catch (err) {
                                //Our connection has an error - rest the UI
                                view.querySelector('#BridgeStatus').innerText = "Not Connected";
                                statusIcon.innerHTML = "error";
                                statusIcon.style.color = "slategray"; 
                                loading.hide();
                            }
                           
                        });

                    view.querySelector('#addButton').addEventListener('click', (e) => {
                        e.preventDefault;
                        var deviceSelect = view.querySelector('#selectEmbyDevice');
                        var device = deviceSelect.options[deviceSelect.selectedIndex >= 0
                            ? deviceSelect.selectedIndex
                            : 0].dataset.name;
                        var app = deviceSelect.options[deviceSelect.selectedIndex >= 0
                            ? deviceSelect.selectedIndex
                            : 0].dataset.app;
                        ApiClient.getPluginConfiguration(pluginId).then(
                            (c) => {
                                openDeviceEditorDialog(device, app, c, view);
                            });

                    }); 
                });
        };

    });