using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    public class AuthorizationState
    {
        public string IdentifierType
        { get; set; }

        public string Identifier
        { get; set; }

        public string Uri
        { get; set; }

        public string Status
        { get; set; }

        public DateTime? Expires
        { get; set; }

        public IEnumerable<AuthorizeChallenge> Challenges
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

        public static AuthorizationState Load(Stream s)
        {
            using (var r = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<AuthorizationState>(r.ReadToEnd());
            }
        }
    }
}
