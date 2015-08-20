using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    public class DnsIdentifierValidation
    {
        public string Dns
        { get; set; }

        public string Status
        { get; set; }

        public IEnumerable<ValidationChallenge> Challenges
        { get; set; }

        public IEnumerable<IEnumerable<int>> Combinations
        { get; set; }

        public void Save(Stream s)
        {
            using (var w = new StreamWriter(s))
            {
                w.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }

        public static DnsIdentifierValidation Load(Stream s)
        {
            using (var r = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<DnsIdentifierValidation>(r.ReadToEnd());
            }
        }
    }
}
