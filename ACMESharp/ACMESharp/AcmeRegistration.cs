using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ACMESharp
{
    public class AcmeRegistration
    {
        public IEnumerable<string> Contacts
        { get; set; }

        public object PublicKey
        { get; set; }

        public object RecoveryKey
        { get; set; }

        public string RegistrationUri
        { get; set; }

        public IEnumerable<string> Links
        { get; set; }

        public string TosLinkUri
        { get; set; }

        public string TosAgreementUri
        { get; set; }

        public string AuthorizationsUri
        { get; set; }

        public string CertificatesUri
        { get; set; }

        public void Save(Stream s)
        {
            using (var w = new StreamWriter(s))
            {
                w.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }

        public static AcmeRegistration Load(Stream s)
        {
            using (var r = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<AcmeRegistration>(r.ReadToEnd());
            }
        }
    }
}
