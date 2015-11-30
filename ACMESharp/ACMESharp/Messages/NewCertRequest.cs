namespace ACMESharp.Messages
{
    public class NewCertRequest : RequestMessage
    {
        public NewCertRequest() : base("new-cert")
        { }

        public string Csr
        { get; set; }
    }
}
