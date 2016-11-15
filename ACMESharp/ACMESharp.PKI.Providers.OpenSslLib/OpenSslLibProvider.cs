using System;
using System.Collections.Generic;
using System.IO;

namespace ACMESharp.PKI.Providers
{
    public class OpenSslLibProvider : CertificateProvider
    {
        public const string PROVIDER_NAME = "OpenSSL-LIB";

        private static Type _cpType;
        private readonly CertificateProvider _cp;

        static OpenSslLibProvider()
        {
            if (System.Environment.Is64BitProcess)
                _cpType = Type.GetType("ACMESharp.PKI.Providers.OpenSslLib64Provider, ACMESharp.PKI.Providers.OpenSslLib64");
            else
                _cpType = Type.GetType("ACMESharp.PKI.Providers.OpenSslLib32Provider, ACMESharp.PKI.Providers.OpenSslLib32");
        }

        public OpenSslLibProvider(IReadOnlyDictionary<string, string> newParams)
            : base(newParams)
        {
            if (_cpType == null)
                throw new InvalidOperationException("unresolved architecture-specific implementation");

            var argTypes = new[] { typeof(IReadOnlyDictionary<string, string>) };
            var cons = _cpType.GetConstructor(argTypes);
            if (cons == null)
                throw new InvalidOperationException("unresolved paramterized constructor");

            _cp = (CertificateProvider)cons.Invoke(new object[] { newParams });
        }

        public override PrivateKey GeneratePrivateKey(PrivateKeyParams pkp)
        {
            return _cp.GeneratePrivateKey(pkp);
        }

        public override void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target)
        {
            _cp.ExportPrivateKey(pk, fmt, target);
        }

        public override PrivateKey ImportPrivateKey<PK>(EncodingFormat fmt, Stream source)
        {
            return _cp.ImportPrivateKey<PK>(fmt, source);
        }

        public override Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md)
        {
            return _cp.GenerateCsr(csrParams, pk, md);
        }

        public override Csr ImportCsr(EncodingFormat fmt, Stream source)
        {
            return _cp.ImportCsr(fmt, source);
        }

        public override void ExportCsr(Csr csr, EncodingFormat fmt, Stream target)
        {
            _cp.ExportCsr(csr, fmt, target);
        }

        public override Crt ImportCertificate(EncodingFormat fmt, Stream source)
        {
            return _cp.ImportCertificate(fmt, source);
        }

        public override void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target)
        {
            _cp.ExportCertificate(cert, fmt, target);
        }

        public override void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password = "")
        {
            _cp.ExportArchive(pk, certs, fmt, target, password);
        }
    }
}
