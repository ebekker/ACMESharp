using LetsEncrypt.ACME.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.DNS
{
    public class ManualDnsProvider : BaseManualProvider, IDnsProvider
    {
        public void EditTxtRecord(string dnsName, IEnumerable<string> dnsValues)
        {
            WriteRecord("TXT", dnsName, dnsValues.ToArray());
        }

        public void EditARecord(string dnsName, string dnsValue)
        {
            WriteRecord("A", dnsName, dnsValue);
        }

        public void EditCnameRecord(string dnsName, string dnsValue)
        {
            WriteRecord("CNAME", dnsName, dnsValue);
        }

        private void WriteRecord(string dnsType, string dnsName, params string[] dnsValues)
        {
            _writer.WriteLine("Manually Configure DNS Resource Record:");
            _writer.WriteLine("  *   Type:  [{0}]", dnsType);
            _writer.WriteLine("  *   Name:  [{0}]", dnsName);

            if (dnsValues == null || dnsValues.Length == 0)
                _writer.WriteLine("  *  Value:  (N/A)");
            else
            {
                foreach (var v in dnsValues)
                    _writer.WriteLine("  *  Value:  [{0}]", v);
            }
        }
    }
}
