using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ACMESharp.PKI
{
    /// <summary>
    /// A Certificate Provider implements the basic operations needed to support working with
    /// PKI certificates.
    /// </summary>
    /// <remarks>
    /// A Certificate Provider is used to generate, export and import a certificate, as well
    /// as various supporting elements such as Private Keys and Certificate Signing Requests
    /// (CSR), in all their various incarnations.
    /// </remarks>
    public abstract class CertificateProvider : IDisposable
    {
        private const string DEFAULT_PROVIDER_NAME = "";
        private static readonly Type[] PROVIDER_CTOR_SIG = { typeof(IDictionary<string, string>) };

        private static Dictionary<string, Type> _providers = new Dictionary<string, Type>
        {
            { DEFAULT_PROVIDER_NAME, typeof(Providers.OpenSslLibProvider) },
        };

        protected CertificateProvider(IDictionary<string, string> newParams)
        { }

        public virtual void Dispose()
        { }

        public abstract PrivateKey GeneratePrivateKey(PrivateKeyParams pkp);

        /// <summary>
        /// Default implementation of saving a private key serializes as a JSON object.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pk"></param>
        public virtual void SavePrivateKey(PrivateKey pk, Stream target)
        {
            using (var w = new StreamWriter(target))
            {
                w.Write(JsonConvert.SerializeObject(pk));
            }
        }

        /// <summary>
        /// Default implementation of loading a JSON-serialized private key.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public virtual PrivateKey LoadPrivateKey(Stream source)
        {
            using (var r = new StreamReader(source))
            {
                return JsonConvert.DeserializeObject<RsaPrivateKey>(r.ReadToEnd());
            }
        }

        public abstract void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target);

        public abstract PrivateKey ImportPrivateKey<PK>(EncodingFormat fmt, Stream source)
                where PK : PrivateKey;

        public virtual void SaveCsrParams(CsrParams csrParams, Stream target)
        {
            using (var w = new StreamWriter(target))
            {
                w.Write(JsonConvert.SerializeObject(csrParams));
            }
        }

        public virtual CsrParams LoadCsrParams(Stream source)
        {
            using (var r = new StreamReader(source))
            {
                return JsonConvert.DeserializeObject<CsrParams>(r.ReadToEnd());
            }
        }

        public abstract Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md);

        public virtual void SaveCsr(Csr csr, Stream target)
        {
            using (var w = new StreamWriter(target))
            {
                w.Write(JsonConvert.SerializeObject(this));
            }
        }

        public virtual Csr LoadCsr(Stream source)
        {
            using (var r = new StreamReader(source))
            {
                return JsonConvert.DeserializeObject<Csr>(r.ReadToEnd());
            }
        }

        public abstract Csr ImportCsr(EncodingFormat fmt, Stream source);

        public abstract void ExportCsr(Csr csr, EncodingFormat fmt, Stream target);

        public abstract Crt ImportCertificate(EncodingFormat fmt, Stream source);

        public abstract void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target);

        public abstract void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target);

        public static void RegisterProvider<CP>(string name = DEFAULT_PROVIDER_NAME) where CP : CertificateProvider
        {
            lock (_providers)
            {
                _providers[name] = typeof(CP);
            }
        }

        public static void RegisterProvider(Type cpType, string name = DEFAULT_PROVIDER_NAME)
        {
            if (!typeof(CertificateProvider).IsAssignableFrom(cpType))
                throw new InvalidOperationException("type is not a subclass of certificate provider type");

            lock (_providers)
            {
                _providers[name] = cpType;
            }
        }

        /// <summary>
        /// Returns the system default provider.
        /// </summary>
        /// <returns></returns>
        public static CertificateProvider GetProvider(string name = DEFAULT_PROVIDER_NAME,
                IDictionary<string, string> initParams = null)
        {
            Type t;
            lock (_providers)
            {
                t = _providers[name];
            }

            if (t == null)
                throw new KeyNotFoundException("unknown or unregistered provider name");

            if (initParams == null)
                initParams = new Dictionary<string, string>();

            return (CertificateProvider)t.GetConstructor(PROVIDER_CTOR_SIG)
                    .Invoke(new[] { initParams });
        }
    }
}
