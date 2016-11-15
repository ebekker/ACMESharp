using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI
{
    /// <summary>
    /// Defines the interface needed to support implementations of
    /// PKI Tools.
    /// </summary>
    /// <remarks>
    /// A PKI Tool is used to perform basic PKI functions like private
    /// key generation, generation of Certificate Signing Requests (CSR),
    /// and Certificate management (exporting/importing).
    /// </remarks>
    public interface IPkiTool : IDisposable
    {
        #region -- Properties --

        bool IsDisposed { get; }

        #endregion -- Properties --

        #region -- Methods --

        PrivateKey GeneratePrivateKey(PrivateKeyParams pkp);

        void SavePrivateKey(PrivateKey pk, Stream target);

        PrivateKey LoadPrivateKey(Stream source);

        void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target);

        PrivateKey ImportPrivateKey<PK>(EncodingFormat fmt, Stream source)
                where PK : PrivateKey;

        void SaveCsrParams(CsrParams csrParams, Stream target);

        CsrParams LoadCsrParams(Stream source);

        Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md);

        void SaveCsr(Csr csr, Stream target);

        Csr LoadCsr(Stream source);

        Csr ImportCsr(EncodingFormat fmt, Stream source);

        void ExportCsr(Csr csr, EncodingFormat fmt, Stream target);

        Crt ImportCertificate(EncodingFormat fmt, Stream source);

        void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target);

        void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password = "");

        //void RegisterProvider<CP>(string name = DEFAULT_PROVIDER_NAME) where CP : CertificateProvider;
        #endregion -- Methods --
    }
}
