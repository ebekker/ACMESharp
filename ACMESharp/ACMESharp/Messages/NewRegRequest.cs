using System.Collections.Generic;

namespace ACMESharp.Messages
{
    public class NewRegRequest : RequestMessage
    {
        public NewRegRequest() : base("new-reg")
        { }

        protected NewRegRequest(string resource) : base(resource)
        { }

        //  Extremely Important FIX! The LetsEncrypt API at this stage does not accept CONTACT as field but CONTACTS (in plural) will be accepted
        public IEnumerable<string> Contacts 
        { get; set; }

        public string Agreement
        { get; set; }

        public string Authorizations
        { get; set; }

        public string Certificates
        { get; set; }
    }
}
