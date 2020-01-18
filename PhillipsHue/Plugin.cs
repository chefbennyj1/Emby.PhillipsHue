using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using PhillipsHue.Configuration;

namespace PhillipsHue
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Phillips Hue";
           

        public override string Description => "Phillips Hue for Emby";
           

        public static Plugin Instance { get; private set; }

        private Guid _id = new Guid("941C5E40-8CE3-47D8-847F-9D67ACB2BDB5"); 
        public override Guid Id => _id;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;
            

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "hueConfig",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.hueConfig.html"
                },
                new PluginPageInfo
                {
                    Name = "hueConfigJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.hueConfig.js"
                }
                

            };
        }
    }
}