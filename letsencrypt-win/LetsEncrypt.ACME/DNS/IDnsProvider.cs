using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.DNS
{

    public interface IDnsProvider
    {
        void EditTxtRecord(string dnsNames, IEnumerable<string> dnsValues);
    }
}
