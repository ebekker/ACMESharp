namespace ACMESharp.Messages
{
    public class RevokeCertRequest : RequestMessage
    {
        public RevokeCertRequest() : base(AcmeServerDirectory.RES_REVOKE_CERT)
        { }

        public string Certificate
        { get; set; }
    }
}
