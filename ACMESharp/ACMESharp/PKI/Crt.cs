namespace ACMESharp.PKI
{

    public class Crt
    {
        public string Pem
        { get; set; }

        public enum MessageDigest
        {
            SHA256
        }
    }
}
