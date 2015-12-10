using System.Text;

namespace ACMESharp.WebServer
{
    public class WebServerInfo
    {
        // TOOD: this is repeated from DnsInfo, clean this up!
        private static Newtonsoft.Json.JsonSerializerSettings JSS =
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                };

        public XXXIWebServerProvider Provider
        { get; set; }

        public void Save(System.IO.Stream s)
        {
            using (var w = new System.IO.StreamWriter(s))
            {
                w.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this, JSS));
            }
        }

        public string Save()
        {
            using (var w = new System.IO.MemoryStream())
            {
                Save(w);
                return Encoding.UTF8.GetString(w.ToArray());
            }
        }

        public static WebServerInfo Load(System.IO.Stream s)
        {
            using (var r = new System.IO.StreamReader(s))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<WebServerInfo>(
                        r.ReadToEnd(), JSS);
            }
        }

        public static WebServerInfo Load(string json)
        {
            using (var r = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return Load(r);
            }
        }
    }
}
