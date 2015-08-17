using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LetsEncrypt.ACME.Messages
{
    public class NewRegRequest : RequestMessage
    {
        public NewRegRequest()
            : base("new-reg")
        { }

        public IEnumerable<string> Contact
        { get; set; }

        public string Agreement
        { get; set; }

        public string Authorizations
        { get; set; }

        public string Certificates
        { get; set; }
    }
}
