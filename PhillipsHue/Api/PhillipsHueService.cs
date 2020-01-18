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
            
            var deviceType = jsonSerializer.SerializeToString(new PhillipsHueRequestData { devicetype = "EmbySceneController" });
            
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
