using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
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
