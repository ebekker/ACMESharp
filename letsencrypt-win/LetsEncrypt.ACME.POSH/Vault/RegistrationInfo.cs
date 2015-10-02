using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public class RegistrationInfo
    {
        public Guid Id
        { get; set; }

        public string Alias
        { get; set; }

        public string Label
        { get; set; }

        public string Memo
        { get; set; }

        public string SignerProvider
        { get; set; }

        public string SignerState
        { get; set; }

        public AcmeRegistration Registration
        { get; set; }
    }
}
