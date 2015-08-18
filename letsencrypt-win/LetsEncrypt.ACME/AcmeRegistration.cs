using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    public class AcmeRegistration
    {
        public IEnumerable<string> Contacts
        { get; set; }

        public string PublicKey
        { get; set; }

        public string RecoveryKey
        { get; set; }

        public string RegistrationUri
        { get; set; }
    }
}
