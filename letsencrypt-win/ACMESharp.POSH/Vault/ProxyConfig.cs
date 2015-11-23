using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public class ProxyConfig
    {

        public bool UseNoProxy
        { get; set; }

        public string ProxyURI
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
            else if (!string.IsNullOrEmpty(ProxyURI))
            {
                var newwp = new WebProxy(ProxyURI);
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
