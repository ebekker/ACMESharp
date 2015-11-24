namespace ACMESharp.Messages
{
    public class NewAuthzRequest : RequestMessage
    {
        public NewAuthzRequest() : base("new-authz")
        { }

        public IdentifierPart Identifier
        { get; set; }
    }
}
