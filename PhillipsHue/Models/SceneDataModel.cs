using System;
using System.Collections.Generic;

namespace PhillipsHue.Models
{
    
    public class SceneDataModel
    {
        public string name { get; set; }
        public List<string> lights { get; set; }
        public string owner { get; set; }
        public bool recycle { get; set; }
        public bool locked { get; set; }
        public Appdata appdata { get; set; }
        public string picture { get; set; }
        public DateTime lastupdated { get; set; }
        public int version { get; set; }
    }

    public class Appdata
    {
        public int version { get; set; }
        public string data { get; set; }
    }

    public class SceneID
    {
       public string Id { get; set; }
    }

    public class ScenesData
    {
        //I'm pretty sure this is a dictionary in the JSON data
        public Dictionary<SceneID, SceneDataModel> Scenes { get; set; }
    }
}
