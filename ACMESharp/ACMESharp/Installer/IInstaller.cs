using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Installer
{
    /// <summary>
    /// Defines the interface needed to support implementations of
    /// Certificate Installers.
    /// </summary>
    /// <remarks>
    /// Certificate Installers install a private key and a certificate chain
    /// into some target system, such as a web server or endpoint service.
    /// </remarks>
    public interface IInstaller : IDisposable
    {
        #region -- Properties --

        bool IsDisposed { get; }

        #endregion -- Properties --

        #region -- Methods --

        void Install(PKI.PrivateKey pk, PKI.Crt crt, IEnumerable<PKI.Crt> chain,
                PKI.IPkiTool cp);

        void Uninstall(PKI.PrivateKey pk, PKI.Crt crt, IEnumerable<PKI.Crt> chain,
                PKI.IPkiTool cp);

        #endregion -- Methods --
    }
}
