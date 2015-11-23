using ACMESharp.POSH.Vault;
using System;
using System.IO;
using ACMESharp.JOSE;

namespace ACMESharp.POSH.Util
{
    public static class ClientHelper
    {
        public static AcmeClient GetClient(VaultConfig Config)
        {
            var p = Config.Proxy;
            var _Client = new AcmeClient();

            _Client.RootUrl = new Uri(Config.BaseURI);
            _Client.Directory = Config.ServerDirectory;

            if (Config.Proxy != null)
                _Client.Proxy = Config.Proxy.GetWebProxy();

            return _Client;
        }

        public static AcmeClient GetClient(VaultConfig config, RegistrationInfo reg)
        {
            var c = GetClient(config);

            c.Signer = GetSigner(reg.SignerProvider);
            c.Signer.Init();
            c.Registration = reg.Registration;

            if (reg.SignerState != null)
            {
                using (var s = new MemoryStream(Convert.FromBase64String(
                        reg.SignerState)))
                {
                    c.Signer.Load(s);
                }
            }
            else
            {
                using (var s = new MemoryStream())
                {
                    c.Signer.Save(s);
                    reg.SignerState = Convert.ToBase64String(s.ToArray());
                }
            }

            return c;
        }

        public static void Init(VaultConfig config, AcmeClient client)
        {
            client.Init();

            if (config.GetInitialDirectory)
                client.GetDirectory(config.UseRelativeInitialDirectory);
        }

        public static ISigner GetSigner(string signerProvider)
        {
            switch (signerProvider)
            {
                case "RS256":
                    return new RS256Signer();

                default:
                    return (ISigner)Type.GetType(signerProvider, true, true);
            }
        }
    }
}
