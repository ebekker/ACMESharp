using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.PKI;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace ACMESharp.Providers.IIS
{
    /// <summary>
    /// Implements an <see cref="IInstaller">Installer</see> that configures
    /// a local IIS Site endpoint with an ACME certificate.
    /// </summary>
    /// <remarks>
    /// Much of the implementation of this handler was initially adapted from the
    /// similarly-intentioned <see
    /// cref="https://github.com/Lone-Coder/letsencrypt-win-simple/blob/master/letsencrypt-win-simple/Plugin/IISPlugin.cs"
    /// ><code>IISPlugin</code></see> class from the <see
    /// cref="https://github.com/Lone-Coder/letsencrypt-win-simple"
    /// >letsencrypt-win-simple</see> project -- thank you, <see
    /// cref="https://github.com/Lone-Coder">Bryan</see>!
    /// </remarks>
    public class IisInstaller : IInstaller
    {
        #region -- Properties --

        public string WebSiteRef
        { get; set; }

        public string BindingAddress
        { get; set; }

        public int BindingPort
        { get; set; } = 443;

        public string BindingHost
        { get; set; }

        public bool? BindingHostRequired
        { get; set; }

        public bool Force
        { get; set; }

        public string CertificateFriendlyName
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public void Install(PrivateKey pk, Crt crt, IEnumerable<Crt> chain, IPkiTool cp)
        {
            var bindings = IisHelper.ResolveSiteBindings(WebSiteRef);
            var existing = IisHelper.ResolveSiteBindings(
                    BindingAddress, BindingPort, BindingHost, bindings).ToArray();

            if (existing?.Length > 0 && !Force)
                throw new InvalidOperationException(
                        "found existing conflicting bindings for target site;"
                        + " use Force parameter to overwrite");

            // TODO: should we expose these as optional params to be overridden by user?
            var storeLocation = StoreLocation.LocalMachine;
            var storeName = StoreName.My;
            var cert = ImportCertificate(pk, crt, chain, cp,
                    storeName, storeLocation, CertificateFriendlyName);

            var certStore = Enum.GetName(typeof(StoreName), storeName);
            var certHash = cert.GetCertHash();


            if (existing?.Length > 0)
            {
                foreach (var oldBinding in existing)
                {
                    if (BindingHostRequired.HasValue)
                        oldBinding.BindingHostRequired = BindingHostRequired;
                    IisHelper.UpdateSiteBinding(oldBinding, certStore, certHash);
                }
            }
            else
            {
                var firstBinding = bindings.First();
                var newBinding = new IisWebSiteBinding
                {
                    // Copy over some existing site info
                    SiteId = firstBinding.SiteId,
                    SiteName = firstBinding.SiteName,
                    SiteRoot = firstBinding.SiteRoot,

                    // New binding specifics
                    BindingProtocol = "https",
                    BindingAddress = this.BindingAddress,
                    BindingPort = this.BindingPort.ToString(),
                    BindingHost = this.BindingHost,
                    BindingHostRequired = this.BindingHostRequired,
                };

                IisHelper.CreateSiteBinding(newBinding, certStore, certHash);
            }
        }

        public void Uninstall(PrivateKey pk, Crt crt, IEnumerable<Crt> chain, IPkiTool cp)
        {
            throw new NotImplementedException();
        }

        public static X509Certificate2 ImportCertificate(
                PrivateKey pk, Crt crt, IEnumerable<Crt> chain, IPkiTool cp,
                StoreName storeName, StoreLocation storeLocation, string friendlyName)
        {
            var store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                var crtChain = new[] { crt }.Concat(chain);
                using (var ms = new MemoryStream())
                {
                    cp.ExportArchive(pk, crtChain, ArchiveFormat.PKCS12, ms);

                    var flag = X509KeyStorageFlags.UserKeySet;
                    if (storeLocation == StoreLocation.LocalMachine)
                        flag = X509KeyStorageFlags.MachineKeySet;

                    var cert = new X509Certificate2(ms.ToArray(), string.Empty,
                            X509KeyStorageFlags.PersistKeySet | flag |
                            X509KeyStorageFlags.Exportable);

					if (!string.IsNullOrEmpty(friendlyName))
						cert.FriendlyName = friendlyName;

                    store.Add(cert);
                    return cert;
                }
            }
            finally
            {
                store.Close();
            }
        }

        #endregion -- Methods --

        #region -- IDisposable Support --
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IisInstaller() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion -- IDisposable Support --
    }
}
