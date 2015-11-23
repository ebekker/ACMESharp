using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.DNS
{

    public interface IDnsProvider : IChallengeHandlingProvider
    {
        void EditTxtRecord(string dnsName, IEnumerable<string> dnsValues);

        void EditARecord(string dnsName, string dnsValue);

        void EditCnameRecord(string dnsName, string dnsValue);
    }
}
