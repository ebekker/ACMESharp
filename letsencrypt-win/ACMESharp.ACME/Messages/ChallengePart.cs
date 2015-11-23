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

        public string Status
        { get; set; }

        public bool? Tls
        { get; set; }

        public DateTime? Validated
        { get; set; }

        public object ValidationRecord
        { get; set; }
    }
}
