using System.Collections.Generic;

namespace ACMESharp.Messages
{
    public class NewRegRequest : RequestMessage
    {
        public NewRegRequest() : base("new-reg")
        { }

        protected NewRegRequest(string resource) : base(resource)
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
