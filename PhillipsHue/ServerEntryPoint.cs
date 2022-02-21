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

        private static List<string> StartedSessionIds = new List<string>();

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

        private class PausedSession
        {

            public string SessionId  { get; set; }
            public DateTime PausedAt { get; set; }

        }

        private static List<PausedSession> PausedSessionsIds = new List<PausedSession>();

        private void PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            var config = Plugin.Instance.Configuration;
            if (!config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(e.DeviceName))) 
                return;

            if (config.BridgeIpAddress == null) 
                return;

            if (config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(e.Session.DeviceName)))
            {
                var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(e.Session.DeviceName));
                if (profile?.MediaItemCredits != null)
                {
                    if (e.MediaInfo.Type.Equals("Movie") &&
                        e.Session.PlayState.PositionTicks > (e.Item.RunTimeTicks - (profile?.MediaItemCreditLength * 10000000)))
                    {
                        PlaybackCredits(e, profile, config);
                        return;
                    }
                }
            }

            switch (e.Session.PlayState.IsPaused)
            {
                case true:
                    logger.Debug($"Session is pausing:  Device: { e.DeviceName }  Session: { e.Session.Id }");

                    // We've already flagged this session, move on
                    lock (PausedSessionsIds)
                    {
                        if (PausedSessionsIds.Exists(s => s.SessionId.Equals(e.Session.Id)))
                        {
                            logger.Debug($"Session already paused: { e.Session.Id }");
                        }
                        else
                        {
                            PausedSessionsIds.Add(new PausedSession()
                            {
                                SessionId = e.Session.Id,
                                PausedAt = DateTime.Now
                            });
                        }

                        PlaybackPaused(e, config, e.Session,
                            config.SavedHueEmbyProfiles.FirstOrDefault(p =>
                                p.DeviceName.Equals(e.Session.DeviceName)));
                    }
                       
                    break;

                case false:
                    logger.Debug($"Session is un pausing:  Device: { e.DeviceName }  Session: { e.Session.Id }");
                    lock (PausedSessionsIds)
                    {
                        if (PausedSessionsIds.Exists(s => s.SessionId.Equals(e.Session.Id)))
                        {
                            PausedSessionsIds.RemoveAll(s => s.SessionId.Equals(e.Session.Id));
                            PlaybackUnPaused(e, config, config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(e.Session.DeviceName)));
                        }
                    }

                    break;
            }
        }

        private void PlaybackUnPaused(PlaybackProgressEventArgs e, PluginConfiguration config, PhillipsHueSceneEmbyProfile profile)
        {
            if (config.BridgeIpAddress == null) 
                return;

            logger.Debug("Phillips Hue Reports Playback UnPaused...");

            logger.Debug("Phillips Hue Profile Device: " + profile.DeviceName);

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

            logger.Debug($"Phillips Hue Reports {e.MediaInfo.Type} will trigger Playback UnPaused Scene for {e.DeviceName}");

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);

        }

        private void PlaybackPaused(PlaybackProgressEventArgs e, PluginConfiguration config, SessionInfo session, PhillipsHueSceneEmbyProfile profile)
        {
            if (config.BridgeIpAddress == null) 
                return;

            logger.Debug("Phillips Hue Reports Playback Paused...");

            logger.Debug($"Phillips Hue Found Session Device: { session.DeviceName }");

            if (!ScheduleAllowScene(profile))
            {
                logger.Debug($"Phillips Hue profile not allowed to run at this time: { profile.DeviceName }");
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
            var config = Plugin.Instance.Configuration;
            //We check here if a profile exists or return
            if (!config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(e.DeviceName) &&
                                                         p.AppName.Equals(e.ClientName))) 
                return;

            if (config.BridgeIpAddress == null) 
                return;

            lock (StartedSessionIds)
            {
                if (!StartedSessionIds.Exists(s => s.Equals(e.Session.Id)))
                    return;

                StartedSessionIds.RemoveAll(s => s.Equals(e.Session.Id));
            }

            logger.Debug($"Phillips Hue Reports Playback Stopped.  Device: { e.DeviceName }  Session: { e.Session.Id }");


            //The item was in a paused state when the user stopped it, clean up the paused session list.
            lock (PausedSessionsIds)
            {
                if (PausedSessionsIds.Exists(s => s.Equals(e.Session.Id)))
                    PausedSessionsIds.RemoveAll(s => s.Equals(e.Session.Id));
            }

            //The item might appear in the credit session list remove it if it does.
            lock (CreditSessions)
            {
                if (CreditSessions.Exists(s => s.Equals(e.Session.Id)))
                    CreditSessions.RemoveAll(s => s.Equals(e.Session.Id));
            }

            //We can assume this will not be null, even though he have to assert it is not null below "profile?.{property}"
            var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(e.DeviceName) &&
                                                                          p.AppName.Equals(e.ClientName));
            logger.Debug($"Phillips Hue Found Profile Device: { e.DeviceName } ");

            if (!ScheduleAllowScene(profile))
            {
                logger.Debug($"Phillips Hue profile not allowed to run at this time: { profile?.DeviceName }");
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

            logger.Debug("Phillips Hue Reports " + e.MediaInfo.Type + " will trigger Playback Stopped Scene on " + e.DeviceName);

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);

        }

        private void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var config = Plugin.Instance.Configuration;

            if (config.BridgeIpAddress == null) 
                return;

            lock (StartedSessionIds)
            {
                if (StartedSessionIds.Exists(s => s.Equals(e.Session.Id)))
                    return;

                StartedSessionIds.Add(e.Session.Id);
            }

            //No profile, move on
            if (!config.SavedHueEmbyProfiles.Exists(p => p.DeviceName.Equals(e.DeviceName) && p.AppName.Equals(e.ClientName))) 
                return;

            logger.Debug($"Phillips Hue Reports Playback Started.  Device: { e.DeviceName }  Session: { e.Session.Id }");

            var profile = config.SavedHueEmbyProfiles.FirstOrDefault(p => p.DeviceName.Equals(e.DeviceName) &&
                                                                          p.AppName.Equals(e.ClientName));

            logger.Debug($"Phillips Hue Profile Device: { e.DeviceName }");

            if (!ScheduleAllowScene(profile))
            {
                logger.Info($"Phillips Hue profile not allowed to run at this time: { profile?.DeviceName }");

                lock (StartedSessionIds)
                {
                    StartedSessionIds.RemoveAll(s => s.Equals(e.Session.Id));
                }

                return;
            }

            lock (PausedSessionsIds)
            {
                PausedSessionsIds.RemoveAll(s => s.SessionId.Equals(e.Session.Id));
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

            logger.Debug($"Phillips Hue Reports { e.MediaInfo.Type } will trigger Playback Started Scene on { e.DeviceName }");

            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = sceneName

            }), config);

        }

        private List<string> CreditSessions = new List<string>();
        private void PlaybackCredits(PlaybackProgressEventArgs e, PhillipsHueSceneEmbyProfile profile, PluginConfiguration config)
        {
            lock (CreditSessions)
            {
                //We've already triggered the event, it's in the list - move on
                if (CreditSessions.Exists(s => s.Equals(e.Session.Id))) 
                    return;

                //Add the session ID to the list so this event doesn't trigger again
                CreditSessions.Add(e.Session.Id); 
            }

            logger.Debug($"Phillips Hue Reports trigger Credit Scene on {e.DeviceName}"); 
            RunScene(JsonSerializer.SerializeToString(new SceneRequest
            {
                scene = profile.MediaItemCredits
            }), config);

        }

        private class SceneRequest
        {
            public string scene { get; set; }
        }

        private static bool ScheduleAllowScene(PhillipsHueSceneEmbyProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Schedule)) return true;
            return (DateTime.Now.TimeOfDay >= TimeSpan.Parse(profile.Schedule + ":00") || DateTime.Now.TimeOfDay <= TimeSpan.Parse("6:00:00"));
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
                logger.Debug("Phillips Hue Reports Scene Trigger Success");
            }
            catch (Exception e)
            {
                logger.Error($"Phillips Hue Reports Scene Trigger Error:  { e.Data }");
            }
        }
    }
}
