using System;

namespace ACMESharp.Vault.Model
{
    public class RegistrationInfo : IIdentifiable
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
