namespace PhillipsHue.Models
{
    public class Success
    {
        public string username { get; set; }
    }
    public class Error
    {
        public string type { get; set; }
        public string address { get; set; }
        public string description { get; set; }
    }
    public class UserDataModel
    {
        public Success success { get; set; }
        public Error error { get; set; }
    }
    
}
