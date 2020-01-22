using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using PhillipsHue.Configuration;

namespace PhillipsHue
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private ILogger logger { get; set; }
        private ILogManager LogManager { get; set; }
        private static IJsonSerializer JsonSerializer { get; set; }
        private static IHttpClient Client { get; set; }
        private static ServerEntryPoint Instance { get; set; }
        private static ISessionManager SessionManager { get; set; }

        // ReSharper disable once TooManyDependencies
        public ServerEntryPoint(IJsonSerializer jsonSerializer, IHttpClient client, ISessionManager sessionManager, ILogManager logManager)
        {
            JsonSerializer = jsonSerializer;
            Client = client;
            Instance = this;
            SessionManager = sessionManager;
            LogManager = logManager;
            logger = LogManager.GetLogger(Plugin.Instance.Name);
        }


        public void Dispose()
        {
            SessionManager.PlaybackStart -= PlaybackStart;
            SessionManager.PlaybackStopped -= PlaybackStopped;
            SessionManager.PlaybackProgress -= PlaybackProgress;
        }

        public void Run()
        {
            SessionManager.PlaybackStart += PlaybackStart;
            SessionManager.PlaybackStopped += PlaybackStopped;
            SessionManager.PlaybackProgress += PlaybackProgress;
        }

        private List<string> PausedSessionsIds = new List<string>();

        private void PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            //No paused Session and no flagged sessions paused
            // ReSharper disable once ComplexConditionExpression
            if (!SessionManager.Sessions.Any(s => s.PlayState.IsPaused) && !PausedSessionsIds.Any()) return;

            var config = Plugin.Instance.Configuration;

            foreach (var session in SessionManager.Sessions)
            {
                switch (session.PlayState.IsPaused || e.IsPaused)
                {
                    case true:
                        // We've already flagged this session, move on
                        if (PausedSessionsIds.Exists(s => s.Equals(session.Id))) continue;

                        //We don't have a profile for this paused session, move on
                        if (!config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(session.DeviceName))) continue;

                        PausedSessionsIds.Add(session.Id);

                        PlaybackPaused(e, config, session, config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(session.DeviceName)));

                        continue;

                    case false:

                        if (PausedSessionsIds.Exists(s => s.Equals(session.Id)))
                        {
                            PlaybackUnPaused(e, config, config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(session.DeviceName)));
                            PausedSessionsIds.RemoveAll(s => s.Equals(session.Id));
                        }

                        continue;
                }
            }
        }

        // ReSharper disable once TooManyArguments
        private void PlaybackUnPaused(PlaybackProgressEventArgs e, PluginConfiguration config, PhillipsHueSceneEmbyProfile profile)
        {
            if (config.BridgeIpAddress == null) return;

            logger.Info("Phillips Hue Reports Playback UnPaused...");
            
            logger.Info("Phillips Hue Found Profile Device: " + profile.DeviceName);

            if (!ScheduleAllowScene(profile))
            {
                logger.Info("Phillips Hue profile not allowed to run at this time: " + profile.DeviceName);
                return;
            }

            var sceneName = string.Empty;
            switch (e.MediaInfo.Type)
            {
                case "Movie":
                    sceneName = profile.MoviesPlaybackUnPaused;
                    break;
                case "TvChannel":
                    sceneName = profile.LiveTvPlaybackUnPaused;
                    break;
                case "Series":
                    sceneName = profile.TvPlaybackUnPaused;
                    break;
                case "Season":
                    sceneName = profile.TvPlaybackUnPaused;
                    break;
                case "Episode":
                    sceneName = profile.TvPlaybackUnPaused;
                    break;
            }

            logger.Info($"Phillips Hue Reports {e.MediaInfo.Type} will trigger Playback UnPaused Scene for {e.DeviceName}");

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);

        }

        // ReSharper disable once TooManyArguments
        private void PlaybackPaused(PlaybackProgressEventArgs e, PluginConfiguration config, SessionInfo session, PhillipsHueSceneEmbyProfile profile)
        {
            if (config.BridgeIpAddress == null) return;

            logger.Info("Phillips Hue Reports Playback Paused...");

            logger.Info($"Phillips Hue Found Session Device: { session.DeviceName }");

            if (!ScheduleAllowScene(profile))
            {
                logger.Info($"Phillips Hue profile not allowed to run at this time: { profile.DeviceName }");
                return;
            }

            var sceneName = string.Empty;

            switch (e.MediaInfo.Type)
            {
                case "Movie":
                    sceneName = profile.MoviesPlaybackPaused;
                    break;
                case "TvChannel":
                    sceneName = profile.LiveTvPlaybackPaused;
                    break;
                case "Series":
                    sceneName = profile.TvPlaybackPaused;
                    break;
                case "Season":
                    sceneName = profile.TvPlaybackPaused;
                    break;
                case "Episode":
                    sceneName = profile.TvPlaybackPaused;
                    break;
            }

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);


        }

        private void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            logger.Info("Phillips Hue Reports Playback Stopped");

            var config = Plugin.Instance.Configuration;

            if (config.BridgeIpAddress == null) return;

            if (e.IsPaused) return;

            //We check here if a profile exists or return
            if (!config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(e.DeviceName) &&
                                                         p.AppName.Equals(e.ClientName))) return;

            //If the profile exists above and we get here, then we can assume this will not be null, even though he have to assert it is not null below "profile?.{property}"
            var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(e.DeviceName) &&
                                                                          p.AppName.Equals(e.ClientName));

            logger.Info($"Phillips Hue Found Profile Device: { e.DeviceName } ");

            if (!ScheduleAllowScene(profile))
            {
                logger.Info($"Phillips Hue profile not allowed to run at this time: { profile?.DeviceName }");
                return;
            }

            var sceneName = string.Empty;
            switch (e.MediaInfo.Type)
            {
                case "Movie":
                    sceneName = profile?.MoviesPlaybackStopped;
                    break;
                case "TvChannel":
                    sceneName = profile?.LiveTvPlaybackStopped;
                    break;
                case "Series":
                    sceneName = profile?.TvPlaybackStopped;
                    break;
                case "Season":
                    sceneName = profile?.TvPlaybackStopped;
                    break;
                case "Episode":
                    sceneName = profile?.TvPlaybackStopped;
                    break;
            }

            logger.Info("Phillips Hue Reports " + e.MediaInfo.Type + " will trigger Playback Stopped Scene on " + e.DeviceName);

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);

        }

        private void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var config = Plugin.Instance.Configuration;
            if (config.BridgeIpAddress == null) return;

            //No profile, move on
            if (!config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(e.DeviceName) && p.AppName.Equals(e.ClientName))) return;

            logger.Info("Phillips Hue Reports Playback Started");

            var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(e.DeviceName) &&
                                                                          p.AppName.Equals(e.ClientName));

            logger.Info($"Phillips Hue Found Profile Device: { e.DeviceName }");

            if (!ScheduleAllowScene(profile))
            {
                logger.Info($"Phillips Hue profile not allowed to run at this time: { profile?.DeviceName }");
                return;
            }

            var sceneName = string.Empty;

            switch (e.MediaInfo.Type)
            {
                case "Movie":
                    sceneName = profile?.MoviesPlaybackStarted;
                    break;
                case "TvChannel":
                    sceneName = profile?.LiveTvPlaybackStarted;
                    break;
                case "Series":
                    sceneName = profile?.TvPlaybackStarted;
                    break;
                case "Season":
                    sceneName = profile?.TvPlaybackStarted;
                    break;
                case "Episode":
                    sceneName = profile?.TvPlaybackStarted;
                    break;
            }

            logger.Info($"Phillips Hue Reports { e.MediaInfo.Type } will trigger Playback Started Scene on { e.DeviceName }");

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);
            
        }

        private class SceneRequest
        {
            public string scene { get; set; }
        }

        private static bool ScheduleAllowScene(PhillipsHueSceneEmbyProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Schedule)) return true;

            return (DateTime.Now.TimeOfDay >= TimeSpan.Parse(profile.Schedule + ":00")) &&
                   (DateTime.Now <= DateTime.Now.Date.AddDays(1).AddHours(4));
        }

        private void RunScene(string data, PluginConfiguration config)
        {
            try
            {
                var sceneUrl = $"http://{config.BridgeIpAddress}/api/{config.UserToken}/groups/0/action";
                Client.SendAsync(new HttpRequestOptions
                {
                    Url = sceneUrl,
                    RequestContent = data.AsMemory(),
                    RequestContentType = "application/json"
                }, "PUT");
                logger.Info("Phillips Hue Reports Scene Trigger Success");
            }
            catch (Exception e)
            {
                logger.Error($"Phillips Hue Reports Scene Trigger Error:  { e.Data }");
            }
        }
    }
}
