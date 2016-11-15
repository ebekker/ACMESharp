using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.PKI;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace ACMESharp.Providers.Windows
{
    public class WindowsCertificateStoreInstaller : IInstaller
    {
        public StoreLocation StoreLocation
        { get; set; } = StoreLocation.CurrentUser;

        public StoreName StoreName
        { get; set; } = StoreName.My;

        public string FriendlyName
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        public void Install(PrivateKey pk, Crt crt, IEnumerable<Crt> chain, IPkiTool cp)
        {
            var store = new X509Store(StoreName, StoreLocation);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                var crtChain = new[] { crt }.Concat(chain);
                using (var ms = new MemoryStream())
                {
                    cp.ExportArchive(pk, crtChain, ArchiveFormat.PKCS12, ms);

                    var flag = X509KeyStorageFlags.UserKeySet;
                    if (StoreLocation == StoreLocation.LocalMachine)
                        flag = X509KeyStorageFlags.MachineKeySet;

                    var cert = new X509Certificate2(ms.ToArray(), string.Empty,
                            X509KeyStorageFlags.PersistKeySet | flag |
                            X509KeyStorageFlags.Exportable);

                    if (!string.IsNullOrEmpty(FriendlyName))
                        cert.FriendlyName = FriendlyName;

                    store.Add(cert);
                }
            }
            finally
            {
                store.Close();
            }
        }

        public void Uninstall(PrivateKey pk, Crt crt, IEnumerable<Crt> chain, IPkiTool cp)
        {
            throw new NotImplementedException();
        }

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
        // ~WindowsCertificateStoreInstaller() {
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
