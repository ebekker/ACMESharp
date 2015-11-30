using System;

namespace ACMESharp.Messages
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
