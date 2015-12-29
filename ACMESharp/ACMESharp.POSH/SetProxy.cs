using System;
using System.Management.Automation;
using System.Text;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Set, "Proxy", DefaultParameterSetName = PSET_USE_SYSTEM)]
    public class SetProxy : Cmdlet
    {
        public const string PSET_USE_SYSTEM = "UseSystem";
        public const string PSET_USE_NOPROXY = "UseNoProxy";
        public const string PSET_USE_PROXY_NO_CRED = "UseProxyNoCred";
        public const string PSET_USE_PROXY_DEF_CRED = "UseProxyDefCred";
        public const string PSET_USE_PROXY_WITH_CRED = "UseProxyWithCred";

        [Parameter(ParameterSetName = PSET_USE_SYSTEM, Mandatory = true)]
        public SwitchParameter UseSystem
        { get; set; }

        [Parameter(ParameterSetName = PSET_USE_NOPROXY, Mandatory = true)]
        public SwitchParameter UseNoProxy
        { get; set; }

        [Parameter(ParameterSetName = PSET_USE_PROXY_NO_CRED, Mandatory = true)]
        [Parameter(ParameterSetName = PSET_USE_PROXY_DEF_CRED, Mandatory = true)]
        [Parameter(ParameterSetName = PSET_USE_PROXY_WITH_CRED, Mandatory = true)]
        public string UseProxy
        { get; set; }

        [Parameter(ParameterSetName = PSET_USE_PROXY_DEF_CRED, Mandatory = true)]
        public SwitchParameter DefaultCredential
        { get; set; }

        [Parameter(ParameterSetName = PSET_USE_PROXY_WITH_CRED, Mandatory = true)]
        public PSCredential Credential
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage();
                var v = vlt.LoadVault();

                if (UseSystem)
                {
                    v.Proxy = null;
                }
                else
                {
                    v.Proxy = new Vault.Model.ProxyConfig
                    {
                        UseNoProxy = UseNoProxy,
                        ProxyUri = UseProxy,
                        UseDefCred = DefaultCredential,
                        Username = Credential?.UserName,
                        PasswordEncoded = Credential?.GetNetworkCredential()?.Password,
                    };

                    if (!string.IsNullOrEmpty(v.Proxy.PasswordEncoded))
                        v.Proxy.PasswordEncoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(
                                v.Proxy.PasswordEncoded));
                };

                vlt.SaveVault(v);
            }
        }
    }
}
