using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace PhillipsHue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string BridgeIpAddress { get; set; }
        
        public string UserToken { get; set; } //Phillips Hue calls this UserName but it's actually a token

        public bool IsSecureConnection { get; set; } = false;

        public bool NightTimeOnly { get; set; } = false;

        public List<PhillipsHueSceneEmbyProfile> SavedHueEmbyProfiles{ get; set; } = new List<PhillipsHueSceneEmbyProfile>();
       
        
    }
    public class PhillipsHueSceneEmbyProfile
    {
        //These are emby client names
        //Warning! do not compare client Ids because they could always change
        public string AppName { get; set; }
        public string DeviceName { get; set; }

        //These are the names of the scenes that we'll compare when an event is triggered
        public string MoviesPlaybackStarted { get; set; }
        public string MoviesPlaybackStopped { get; set; }
        public string MoviesPlaybackPaused { get; set; }
        public string MoviesPlaybackUnPaused { get; set; }

        public string TvPlaybackStarted { get; set; }
        public string TvPlaybackStopped { get; set; }
        public string TvPlaybackPaused { get; set; }
        public string TvPlaybackUnPaused { get; set; }

        public string LiveTvPlaybackStarted { get; set; }
        public string LiveTvPlaybackStopped { get; set; }
        public string LiveTvPlaybackPaused { get; set; }
        public string LiveTvPlaybackUnPaused { get; set; }

        public string Schedule { get; set; }

        public string MediaItemCredits { get; set; }
        public int MediaItemCreditLength { get; set; }

    }
}
