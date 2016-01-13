using System;
using System.Net;
using System.Text;

namespace ACMESharp.Vault.Model
{
    public class ProxyConfig
    {

        public bool UseNoProxy
        { get; set; }

        public string ProxyUri
        { get; set; }

        public bool UseDefCred
        { get; set; }

        public string Username
        { get; set; }

        public string PasswordEncoded
        { get; set; }

        /// <summary>
        /// Computes a <see cref="IWebProxy">web proxy</see> resolver instance
        /// based on the combination of proxy-related settings in this vault
        /// configuration.
        /// </summary>
        /// <returns></returns>
        public IWebProxy GetWebProxy()
        {
            IWebProxy wp = null;

            if (UseNoProxy)
            {
                wp = GlobalProxySelection.GetEmptyWebProxy();
            }
            else if (!string.IsNullOrEmpty(ProxyUri))
            {
                var newwp = new WebProxy(ProxyUri);
                if (UseDefCred)
                {
                    newwp.UseDefaultCredentials = true;
                }
                else if (!string.IsNullOrEmpty(Username))
                {
                    var pw = PasswordEncoded;
                    if (!string.IsNullOrEmpty(pw))
                        pw = Encoding.Unicode.GetString(Convert.FromBase64String(pw));
                    newwp.Credentials = new NetworkCredential(Username, pw);
                }
            }

            return wp;
        }
    }
}
