using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class ChallengePart
    {
        public string Type
        { get; set; }

        public string Uri
        { get; set; }

        public string Token
        { get; set; }
    }
}
