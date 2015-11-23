using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.PKI
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
