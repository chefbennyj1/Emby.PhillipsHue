using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;

namespace PhillipsHue.Api
{
    [Authenticated(Roles = "Admin")]
    [Route("/EmbyDeviceList", "GET", Summary = "Sorted Emby Device List End Point")]
    public class EmbyDeviceList : IReturn<string>
    {
        public string Devices { get; set; }
    }

    [Route("/PhillipsHueButtonPressPng", "GET", Summary = "")]
    public class ButtonPressImage : IReturn<string>
    {
        public string image { get; set; }
    }


    [Route("/DiscoverPhillipsHue", "GET", Summary = "Get Phillips Hue Bridge Data")]
    public class DiscoverPhillipsHue : IReturn<string>
    {
       public string BridgeData { get; set; }
    }
    
    [Route("/GetUserToken", "GET", Summary = "Get User Token")]
    public class UserToken : IReturn<string>
    {
        [ApiMember(Name = "ipAddress", Description = "Bridge IP Address", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ipAddress { get; set; }
    }
    
    [Route("/GetScenes", "GET")]
    public class GetScenes : IReturn<string>
    {
        public string Scenes { get; set; }
    }
    
    public class PhillipsHueService : IService
    {
        private IJsonSerializer jsonSerializer { get; set; }
        private IHttpClient httpClient { get; set; }
        private IDeviceManager deviceManager { get; set; }
        private readonly ILogger logger;
        

        // ReSharper disable once TooManyDependencies
        public PhillipsHueService(ILogManager logManager, IHttpClient httpClient, IJsonSerializer jsonSerializer, IDeviceManager deviceManager)
        {
            logger = logManager.GetLogger(GetType().Name);

            this.httpClient = httpClient;
            this.jsonSerializer = jsonSerializer;
            this.deviceManager = deviceManager;
        }

        public string Get(ButtonPressImage request)
        {
            return jsonSerializer.SerializeToString(new ButtonPressImage
            {
                image =
                    "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"
            });
        }


        public string Get(EmbyDeviceList request)
        {
            var deviceInfo = deviceManager.GetDevices(new DeviceQuery());

            var deviceList = new List<DeviceInfo>();

            foreach (var device in deviceInfo.Items)
            {
                if (!deviceList.Exists(x =>
                    string.Equals(x.Name, device.Name, StringComparison.CurrentCultureIgnoreCase) &&
                    string.Equals(x.AppName, device.AppName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    deviceList.Add(device);
                }
            }

            return jsonSerializer.SerializeToString(deviceList);

        }
        
        public string Get(DiscoverPhillipsHue request)
        {
            try
            {
                var json = new StreamReader(httpClient.Get(new HttpRequestOptions()
                {
                    LogErrors = true,
                    Url = "https://discovery.meethue.com"
                }).Result).ReadToEnd();

                //var dataModel = jsonSerializer.DeserializeFromStream<BridgeDataModel>(json);
               
                return jsonSerializer.SerializeToString(new DiscoverPhillipsHue()
                {
                  BridgeData = json
                });
               
            }
            catch
            {
                return "[]";
            }
        }
        
        public string Get(UserToken request)
        {
            
            var deviceType = jsonSerializer.SerializeToString(new PhillipsHueRequestData { devicetype = "EmbySceneController2" });
            
            // ReSharper disable once ComplexConditionExpression
            var json = httpClient.Post(new HttpRequestOptions()
            {
                Url = "http://" + request.ipAddress + "/api",
                RequestContent = deviceType.AsMemory(),
                RequestContentType= "application/json"
            }).Result;
            
           return new StreamReader(json.Content).ReadToEnd();

        }

        private class PhillipsHueRequestData
        {
            public string devicetype { get; set; }
        }

        public string Get(GetScenes request)
        {
            var config    = Plugin.Instance.Configuration;
            var ip        = config.BridgeIpAddress;
            var userToken = config.UserToken;


            var json = new StreamReader(httpClient.Get(new HttpRequestOptions()
            {
                LogErrorResponseBody = true,
                Url = (config.IsSecureConnection ? "https://" : "http://") + ip + "/api/" + userToken + "/scenes",
                RequestContentType = "application/json"

            }).Result).ReadToEnd();
            
            return jsonSerializer.SerializeToString(new GetScenes() { Scenes = json });
        }

    }
 }
