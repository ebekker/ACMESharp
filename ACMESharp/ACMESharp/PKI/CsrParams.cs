using System;

namespace ACMESharp.PKI
{
    public class CsrParams
    {
        public CsrDetails Details
        { get; set; }

        public DateTime? NotBefore
        { get; set; }

        public DateTime? NotAfter
        { get; set; }
    }
}
