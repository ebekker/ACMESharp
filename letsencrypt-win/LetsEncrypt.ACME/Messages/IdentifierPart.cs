using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class IdentifierPart
    {
        public string Type
        { get; set; }

        public string Value
        { get; set; }
    }
}
