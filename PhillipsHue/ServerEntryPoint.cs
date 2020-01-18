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
            var config = Plugin.Instance.Configuration;

            foreach (var session in SessionManager.Sessions)
            {
                
                switch (session.PlayState.IsPaused)
                {
                    case true:

                        if (PausedSessionsIds.Exists(s => s.Equals(session.Id))) continue;
                        
                        PausedSessionsIds.Add(session.Id);
                        PlaybackPaused(e, config, session);

                        continue;

                    case false:

                        if (PausedSessionsIds.Exists(s => s.Equals(session.Id)))
                        {
                            PlaybackUnPaused(e, config, session);
                            PausedSessionsIds.RemoveAll(s => s.Equals(session.Id));
                        }
                        continue;
                }
            }
        }

        private void PlaybackUnPaused(PlaybackProgressEventArgs e, PluginConfiguration config, SessionInfo session)
        {
            if (config.BridgeIpAddress == null) return;

            logger.Info("Phillips Hue Reports Playback UnPaused...");

            var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(session.DeviceName) && session.Client.Equals(p.AppName));

            if (profile == null) return;

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

        private void PlaybackPaused(PlaybackProgressEventArgs e, PluginConfiguration config, SessionInfo session)
        {
            if (config.BridgeIpAddress == null) return;

            logger.Info("Phillips Hue Reports Playback Paused...");

            var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(session.DeviceName) && session.Client.Equals(p.AppName));
            if (profile == null) return;
            
            logger.Info($"Phillips Hue Found Profile Device: { profile.DeviceName }");

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

            foreach (var profile in config.SavedHueEmbyProfiles)
            {
                if (e.DeviceName != profile.DeviceName || e.ClientName != profile.AppName) continue;

                logger.Info($"Phillips Hue Found Profile Device: { e.DeviceName } ");

                if (!ScheduleAllowScene(profile))
                {
                    logger.Info($"Phillips Hue profile not allowed to run at this time: { profile.DeviceName }");
                    return;
                }

                var sceneName = string.Empty;
                switch (e.MediaInfo.Type)
                {
                    case "Movie":
                        sceneName = profile.MoviesPlaybackStopped;
                        break;
                    case "TvChannel":
                        sceneName = profile.LiveTvPlaybackStopped;
                        break;
                    case "Series":
                        sceneName = profile.TvPlaybackStopped;
                        break;
                    case "Season":
                        sceneName = profile.TvPlaybackStopped;
                        break;
                    case "Episode":
                        sceneName = profile.TvPlaybackStopped;
                        break;
                }

                logger.Info("Phillips Hue Reports " + e.MediaInfo.Type + " will trigger Playback Stopped Scene on " + e.DeviceName);

                RunScene(JsonSerializer.SerializeToString(new SceneRequest
                {
                    scene = sceneName

                }), config);

            }
        }

        private void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var config = Plugin.Instance.Configuration;
            if (config.BridgeIpAddress == null) return;

            logger.Info("Phillips Hue Reports Playback Started");

            foreach (var profile in config.SavedHueEmbyProfiles)
            {
                if (!string.Equals(e.DeviceName, profile.DeviceName, StringComparison.InvariantCultureIgnoreCase) ||
                    
                    !string.Equals(e.ClientName,profile.AppName, StringComparison.InvariantCultureIgnoreCase)) continue;

                logger.Info($"Phillips Hue Found Profile Device: { e.DeviceName }");

                if (!ScheduleAllowScene(profile))
                {
                    logger.Info($"Phillips Hue profile not allowed to run at this time: { profile.DeviceName }");
                    return;
                }

                var sceneName = string.Empty;

                switch (e.MediaInfo.Type)
                {
                    case "Movie":
                        sceneName = profile.MoviesPlaybackStarted;
                        break;
                    case "TvChannel":
                        sceneName = profile.LiveTvPlaybackStarted;
                        break;
                    case "Series":
                        sceneName = profile.TvPlaybackStarted;
                        break;
                    case "Season":
                        sceneName = profile.TvPlaybackStarted;
                        break;
                    case "Episode":
                        sceneName = profile.TvPlaybackStarted;
                        break;
                }

                logger.Info($"Phillips Hue Reports { e.MediaInfo.Type } will trigger Playback Stopped Scene on { e.DeviceName }");

                RunScene(JsonSerializer.SerializeToString(new SceneRequest
                {
                    scene = sceneName

                }), config);

            }
        }

        private class SceneRequest
        {
            public string scene { get; set; }
        }

        private static bool ScheduleAllowScene(PluginConfiguration.PhillipsHueSceneEmbyProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Schedule)) return true;

            return (DateTime.Now.TimeOfDay >= TimeSpan.Parse(profile.Schedule + ":00") && DateTime.Now.TimeOfDay <= TimeSpan.Parse("4:00:00"));
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
