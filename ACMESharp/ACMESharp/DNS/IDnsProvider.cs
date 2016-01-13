using System.Collections.Generic;

namespace ACMESharp.DNS
{

    public interface XXXIDnsProvider
    {
        void EditTxtRecord(string dnsName, IEnumerable<string> dnsValues);

        void EditARecord(string dnsName, string dnsValue);

        void EditCnameRecord(string dnsName, string dnsValue);
    }
}
