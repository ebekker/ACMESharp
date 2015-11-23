using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class NewCertRequest : RequestMessage
    {
        public NewCertRequest() : base("new-cert")
        { }

        public string Csr
        { get; set; }
    }
}
