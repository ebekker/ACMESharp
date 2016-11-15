using ACMESharp.Installer;
using ACMESharp.PKI;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.AWS
{
    public class AwsIamCertificateInstaller : IInstaller
    {
        public const string PATH_REQUIRED_PREFIX = "/";
        public const string PATH_REQUIRED_SUFFIX = "/";
        public const string PATH_REQUIRED_CLOUDFRONT_PREFIX = "/cloudfront/";

        public string ServerCertificateName
        { get; set; }

        public string Path
        { get; set; }

        public bool UseWithCloudFront
        { get; set; }

        public AwsCommonParams CommonParams
        { get; set; } = new AwsCommonParams();

        public bool IsDisposed
        { get; private set; }

        public void Install(PrivateKey pk, Crt crt, IEnumerable<PKI.Crt> chain,
                IPkiTool cp)
        {
            AssertNotDisposed();

            string pkPem;
            using (var ms = new MemoryStream())
            {
                cp.ExportPrivateKey(pk, EncodingFormat.PEM, ms);
                pkPem = Encoding.UTF8.GetString(ms.ToArray());
            }

            string crtPem;
            using (var ms = new MemoryStream())
            {
                cp.ExportCertificate(crt, EncodingFormat.PEM, ms);
                crtPem = Encoding.UTF8.GetString(ms.ToArray());
            }

            string chainPem = null;
            if (chain != null)
            {
                using (var ms = new MemoryStream())
                {
                    foreach (var c in chain)
                    {
                        cp.ExportCertificate(c, EncodingFormat.PEM, ms);
                    }
                    chainPem = Encoding.UTF8.GetString(ms.ToArray());
                }
            }

            using (var client = new AmazonIdentityManagementServiceClient(
                CommonParams.ResolveCredentials(),
                CommonParams.RegionEndpoint))
            {
                var iamRequ = new UploadServerCertificateRequest
                {
                    PrivateKey = pkPem,
                    CertificateBody = crtPem,
                    CertificateChain = chainPem,

                    ServerCertificateName = this.ServerCertificateName,
                    Path = this.Path
                };

                var iamResp = client.UploadServerCertificate(iamRequ);
                // TODO:  any checks we should do?
            }
        }

        public void Uninstall(PrivateKey pk, Crt crt, IEnumerable<PKI.Crt> chain,
                IPkiTool cp)
        {
            AssertNotDisposed();

            using (var client = new AmazonIdentityManagementServiceClient(
                CommonParams.ResolveCredentials(),
                CommonParams.RegionEndpoint))
            {
                var iamRequ = new DeleteServerCertificateRequest
                {
                    ServerCertificateName = this.ServerCertificateName,
                };

                var iamResp = client.DeleteServerCertificate(iamRequ);
                // TODO:  any checks we should do?
            }
        }

        public static IEnumerable<ServerCertificateMetadata> GetServerCertificates(AwsCommonParams commonParams)
        {
            using (var client = new AmazonIdentityManagementServiceClient(
                commonParams.ResolveCredentials(),
                commonParams.RegionEndpoint))
            {
                var iamRequ = new ListServerCertificatesRequest();

                do
                {
                    var iamResp = client.ListServerCertificates(iamRequ);
                    foreach (var r in iamResp.ServerCertificateMetadataList)
                        yield return r;

                    iamRequ.Marker = iamResp.Marker;
                    if (!iamResp.IsTruncated)
                        iamRequ = null;
                } while (iamRequ != null);
            }
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("AWS Challenge Handler is disposed");
        }

        #region IDisposable Support
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
        // ~AwsElbInstaller() {
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
        #endregion
    }
}
