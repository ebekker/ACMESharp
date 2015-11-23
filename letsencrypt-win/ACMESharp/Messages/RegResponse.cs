using System.Collections.Generic;

namespace ACMESharp.Messages
{
    public class RegResponse
    {
        public object Key
        { get; set; }

        public IEnumerable<string> Contact
        { get; set; }

        public string Agreement
        { get; set; }

        public string Authorizations
        { get; set; }

        public string Certificates
        { get; set; }

        public object RecoveryKey
        { get; set; }
    }
}
