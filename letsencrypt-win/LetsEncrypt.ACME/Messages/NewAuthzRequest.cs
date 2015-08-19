using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class NewAuthzRequest : RequestMessage
    {
        public NewAuthzRequest() : base("new-authz")
        { }

        public IdentifierPart Identifier
        { get; set; }
    }
}
