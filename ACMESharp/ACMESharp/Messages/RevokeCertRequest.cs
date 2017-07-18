namespace ACMESharp.Messages
{
    public class RevokeCertRequest : RequestMessage
    {
        public RevokeCertRequest() : base("revoke-cert")
        { }

        public string Certificate
        { get; set; }

        public int Reason
        { get; set; }
    }
}