using System.Collections.Generic;

namespace PhillipsHue.Models
{
    public class BridgeDataModel
    {
        public List<PhillipsHueBridgeData> PhillipsHueBridges { get; set; }
    }

    public class PhillipsHueBridgeData
    {
        public string id { get; set; }
        public string internalipaddress { get; set; }
        
    }
}
