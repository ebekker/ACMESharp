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
        private static readonly Type[] PROVIDER_CTOR_SIG = { typeof(IReadOnlyDictionary<string, string>) };

        private static readonly Dictionary<string, Type> _providers =
                new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        private static readonly List<Tuple<string, string>> _providerTypes =
                new List<Tuple<string, string>>
        {
            Tuple.Create("BouncyCastle",
                    "ACMESharp.PKI.Providers.BouncyCastleProvider, ACMESharp.PKI.Providers.BouncyCastle"),
            Tuple.Create("OpenSSL-LIB",
                    "ACMESharp.PKI.Providers.OpenSslLibProvider, ACMESharp.PKI.Providers.OpenSslLib"),
            Tuple.Create("OpenSSL-CLI",
                    "ACMESharp.PKI.Providers.OpenSslCliProvider, ACMESharp.PKI.Providers.OpenSslCli"),
        };

        // Flag indicates if registration of *supplemental* providers was attempted
        private static bool _providersRegistered = false;

        protected CertificateProvider(IReadOnlyDictionary<string, string> newParams)
        { }

        public virtual bool IsDisposed
        { get; private set; }

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
                return JsonConvert.DeserializeObject<RSA.RsaPrivateKey>(r.ReadToEnd());
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
                w.Write(JsonConvert.SerializeObject(csr));
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

        public abstract void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password = "");

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
        // ~CertificateProvider() {
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
                IReadOnlyDictionary<string, string> initParams = null)
        {
            Type t;
            lock (_providers)
            {
                if (!_providersRegistered)
                    RegisterProviders();
                t = _providers[name];
            }

            if (t == null)
                throw new KeyNotFoundException("unknown or unregistered provider name");

            if (initParams == null)
                initParams = new Dictionary<string, string>();

            return (CertificateProvider)t.GetConstructor(PROVIDER_CTOR_SIG)
                    .Invoke(new[] { initParams });
        }

        private static void RegisterProviders()
        {
            string defaultProvider = null;
            foreach (var p in _providerTypes)
            {
                var t = Type.GetType(p.Item2, false);
                if (t != null)
                {
                    _providers[p.Item1] = t;
                    if (defaultProvider == null)
                    {
                        _providers[DEFAULT_PROVIDER_NAME] = t;
                        defaultProvider = p.Item1;
                    }
                }
            }
            _providersRegistered = true;
        }
    }
}
