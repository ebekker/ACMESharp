using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;

namespace ACMESharp.PKI.Providers
{
    /// <summary>
    /// Implementation of a <see cref="CertificateProvider"/> that uses the native
    /// <see cref="https://msdn.microsoft.com/en-us/library/aa374863(v=vs.85).aspx"
    /// >Certificate Enrollment API</see> in Windows (Vista and later).
    /// </summary>
    public class CertEnrollProvider : CertificateProvider
    {
        // IMPL NOTE:  lots of help and ideas from:
        //    https://gallery.technet.microsoft.com/scriptcenter/Self-signed-certificate-5920a7c6
        //    https://msdn.microsoft.com/en-us/library/aa374850(v=vs.85).aspx

        public CertEnrollProvider(IDictionary<string, string> initParams)
            : base(initParams)
        { }

        /// <summary>
        /// String name constant indicates an export format type that includes the
        /// private key component of a key pair.  Thanks to:
        ///     http://www.dotnetframework.org/default.aspx/4@0/4@0/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Core/System/Security/Cryptography/CngKeyBlobFormat@cs/1305376/CngKeyBlobFormat@cs
        /// </summary>
        private const string BCRYPT_PRIVATE_KEY_BLOB = "PRIVATEBLOB";

        // From:
        //    http://blogs.interfacett.com/selecting-a-cryptographic-key-provider-in-windows-server-2012-ad-cs
        //    https://msdn.microsoft.com/en-us/library/windows/desktop/aa386983(v=vs.85).aspx
        //
        // Microsoft Base Smart Card Crypto Provider
        // Microsoft Enhanced Cryptographic Provider v1.0
        // ECDA_P256#Microsoft Smart Card Key Storage Provider
        // ECDA_P521#Microsoft Smart Card Key Storage Provider
        // RSA#Microsoft Software Key Storage Provider
        // Microsoft Base Cryptographic Provider v1.0
        // ECDA_P256#Microsoft Software Key Storage Provider
        // ECDA_P521#Microsoft Software Key Storage Provider
        // Microsoft Strong Cryptographic Provider
        // ECDA_P384#Microsoft Software Key Storage Provider
        // Microsoft Base DSS Cryptographic Provider
        // RSA#Microsoft Smart Card Key Storage Provider
        // DSA#Microsoft Software Key Storage Provider
        // ECDA_P384#Microsoft Smart Card Key Storage Provider


        public override PrivateKey GeneratePrivateKey(PrivateKeyParams pkp)
        {
            var rsaPkp = pkp as RsaPrivateKeyParams;
            var ecPkp = pkp as EcPrivateKeyParams;

            var algId = new CERTENROLLLib.CObjectId();

            if (rsaPkp != null)
            {
                var oid = new System.Security.Cryptography.Oid("RSA");
                algId.InitializeFromValue(oid.Value);
            }
            else if (ecPkp != null)
            {
                throw new NotImplementedException("EC keys not implemented YET!");
            }
            else
            {
                throw new NotSupportedException("unsupported private key parameters type");
            }


            var cePk = new CERTENROLLLib.CX509PrivateKey();

            // MS_DEF_PROV
            //cePk.ProviderName = "Microsoft Base Cryptographic Provider";
            cePk.ProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";

            //cePk.ProviderType = CERTENROLLLib.X509ProviderType.XCN_PROV_RSA_FULL;
            cePk.Algorithm = algId;
            cePk.KeySpec = CERTENROLLLib.X509KeySpec.XCN_AT_KEYEXCHANGE;
            cePk.Length = rsaPkp.NumBits;

            // Don't store in the machine's local cert store and allow exporting of private key
            cePk.MachineContext = false;
            cePk.ExportPolicy = CERTENROLLLib.X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;
            cePk.Create();

            var pk = new CeRsaPrivateKey(rsaPkp.NumBits, null, null)
            {
                Exported = cePk.Export(BCRYPT_PRIVATE_KEY_BLOB),
            };

            return pk;
        }

        public override void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target)
        {
            var rsaPk = pk as CeRsaPrivateKey;
            if (rsaPk == null)
                throw new NotSupportedException("unsupported private key type");

            var cePk = new CERTENROLLLib.CX509PrivateKey();

            // MS_DEF_PROV
            //cePk.ProviderName = "Microsoft Base Cryptographic Provider";
            cePk.ProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";

            // Don't store in the machine's local cert store and allow exporting of private key
            cePk.MachineContext = false;
            cePk.ExportPolicy = CERTENROLLLib.X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;



            cePk.Import(BCRYPT_PRIVATE_KEY_BLOB, rsaPk.Exported);

            if (fmt == EncodingFormat.PEM)
            {
                var pem = cePk.Export(BCRYPT_PRIVATE_KEY_BLOB,
                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64HEADER);
                var pemBytes = Encoding.UTF8.GetBytes(pem);
                target.Write(pemBytes, 0, pemBytes.Length);
            }
            else if (fmt == EncodingFormat.DER)
            {
                // TODO: Verify this is DER, not quite sure
                var der = cePk.Export(BCRYPT_PRIVATE_KEY_BLOB,
                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BINARY);
                var derBytes = new byte[der.Length];
                for (int i = 0; i < derBytes.Length; ++i)
                    derBytes[i] = (byte)der[i];
                target.Write(derBytes, 0, derBytes.Length);
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
        }

        public override PrivateKey ImportPrivateKey<PK>(EncodingFormat fmt, Stream source)
        {
            if (typeof(PK) == typeof(RsaPrivateKey))
            {
                byte[] keyBytes;
                string encodedKey;
                CERTENROLLLib.EncodingType encodingType;

                using (var ms = new MemoryStream())
                {
                    source.CopyTo(ms);
                    keyBytes = ms.ToArray();
                }

                switch (fmt)
                {
                    case EncodingFormat.PEM:
                        encodedKey = Encoding.UTF8.GetString(keyBytes);
                        encodingType = CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_ANY;
                        break;
                    case EncodingFormat.DER:
                        var buff = new StringBuilder();
                        for (int i = 0; i < keyBytes.Length; ++i)
                            buff.Append((char)keyBytes[i]);
                        encodedKey = buff.ToString();
                        encodingType = CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BINARY;
                        break;
                    default:
                        throw new NotSupportedException("unsupported encoding format");
                }

                var cePk = new CERTENROLLLib.CX509PrivateKey();

                // MS_DEF_PROV
                //cePk.ProviderName = "Microsoft Base Cryptographic Provider";
                //cePk.ProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";
                cePk.ProviderName = "Microsoft Strong Cryptographic Provider";

                // Don't store in the machine's local cert store and allow exporting of private key
                cePk.MachineContext = false;
                cePk.ExportPolicy = CERTENROLLLib.X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;


                cePk.Import(BCRYPT_PRIVATE_KEY_BLOB, encodedKey, encodingType);

                var pk = new CeRsaPrivateKey(cePk.Length, null, null)
                {
                    Exported = cePk.Export(BCRYPT_PRIVATE_KEY_BLOB),
                };

                return pk;
            }
            else
            {
                throw new NotSupportedException("unsupported private key type");
            }
        }

        public override Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md)
        {
            var rsaPk = pk as CeRsaPrivateKey;
            if (rsaPk != null)
            {
                var cePk = new CERTENROLLLib.CX509PrivateKey();

                // MS_DEF_PROV
                //cePk.ProviderName = "Microsoft Base Cryptographic Provider";
                cePk.ProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";

                // Don't store in the machine's local cert store and allow exporting of private key
                cePk.MachineContext = false;
                cePk.ExportPolicy = CERTENROLLLib.X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;

                cePk.Import(BCRYPT_PRIVATE_KEY_BLOB, rsaPk.Exported);

                var ceReq = new CERTENROLLLib.CX509CertificateRequestCertificate();
                ceReq.InitializeFromPrivateKey(
                        CERTENROLLLib.X509CertificateEnrollmentContext.ContextUser,
                        cePk, "");

                // CN=Test Cert, OU=Sandbox
                var subjParts = new[]
                {
                        new { name = "C",  value = csrParams?.Details?.Country },
                        new { name = "ST", value = csrParams?.Details?.StateOrProvince },
                        new { name = "L",  value = csrParams?.Details?.Locality },
                        new { name = "O",  value = csrParams?.Details?.Organization },
                        new { name = "OU", value = csrParams?.Details?.OrganizationUnit},
                        new { name = "CN", value = csrParams?.Details?.CommonName },
                        new { name = "E",  value = csrParams?.Details?.Email },
                    };

                // Escape any non-standard character
                var re = new Regex("[^A-Za-z0-9\\._-]");
                var subj = "";
                foreach (var sp in subjParts)
                {
                    if (!string.IsNullOrEmpty(sp.value))
                    {
                        var spVal = re.Replace(sp.value, "\\$0");
                        subj += $",{sp.name}={spVal}";
                    }
                }
                if (string.IsNullOrEmpty(subj))
                    throw new InvalidOperationException("invalid CSR details");
                subj = subj.Substring(1); // Skip over the first comma

	            // http://msdn.microsoft.com/en-us/library/aa377051(VS.85).aspx
	            var subjDN = new CERTENROLLLib.CX500DistinguishedName();
                subjDN.Encode(subj);
                ceReq.Subject = subjDN;

                if (csrParams.NotBefore != null)
                    ceReq.NotBefore = csrParams.NotBefore.Value;
                if (csrParams.NotAfter != null)
                    ceReq.NotAfter = csrParams.NotAfter.Value;

                var mdVal = Enum.GetName(typeof(Crt.MessageDigest), md);
                var mdOid = new System.Security.Cryptography.Oid(mdVal);
                var mdAlg = new CERTENROLLLib.CObjectId();
                mdAlg.InitializeFromValue(mdOid.Value);
                ceReq.SignatureInformation.HashAlgorithm = mdAlg;
                ceReq.Encode();

                var csr = new Csr(ceReq.RawData);
                return csr;
            }
            else
            {
                throw new NotSupportedException("unsuppored private key type");
            }
        }

        public override void ExportCsr(Csr csr, EncodingFormat fmt, Stream target)
        {
            string outform;
            switch (fmt)
            {
                case EncodingFormat.PEM:
                    outform = "PEM";
                    break;
                case EncodingFormat.DER:
                    outform = "DER";
                    break;
                default:
                    throw new NotSupportedException("unsupported encoding format");
            }

            var ceReq = new CERTENROLLLib.CX509CertificateRequestCertificate();
            ceReq.InitializeDecode(csr.Pem);

            if (fmt == EncodingFormat.DER)
            {
                var bytes = EncodeRaw(ceReq.RawData[CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BINARY]);
                target.Write(bytes, 0, bytes.Length);
            }
            else if (fmt == EncodingFormat.PEM)
            {
                var ceCsr = new CERTENROLLLib.CX509Enrollment();
                ceCsr.InitializeFromRequest(ceReq);
                var pem = ceCsr.CreateRequest(
                        CERTENROLLLib.EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER);
                var bytes = Encoding.UTF8.GetBytes(pem);
                target.Write(bytes, 0, bytes.Length);
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
        }

        public override Csr ImportCsr(EncodingFormat fmt, Stream source)
        {
            throw new NotImplementedException();
        }

        public override Crt ImportCertificate(EncodingFormat fmt, Stream source)
        {
            if (fmt == EncodingFormat.DER)
            {
                var x509 = new X509Certificate2();
                using (var ms = new MemoryStream())
                {
                    source.CopyTo(ms);
                    x509.Import(ms.ToArray());
                }

                var crt = new Crt
                {
                    Pem = Convert.ToBase64String(x509.RawData),
                };
                return crt;
            }

            throw new NotSupportedException("unsupported encoding format");
        }

        public override void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target)
        {
            throw new NotImplementedException();
        }

        public override void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password = "")
        {
            throw new NotImplementedException();
        }

        private static byte[] EncodeRaw(string s)
        {
            var sBytes = new byte[s.Length];
            for (int i = 0; i < sBytes.Length; ++i)
                sBytes[i] = (byte)s[i];
            return sBytes;
        }

        private static string DecodeRaw(byte[] b)
        {
            var buff = new StringBuilder();
            for (int i = 0; i < b.Length; ++i)
                buff.Append((char)b[i]);
            return buff.ToString();
        }

        public class CeRsaPrivateKey : RsaPrivateKey
        {
            public CeRsaPrivateKey(int bits, string e, string pem)
                : base(bits, e, pem)
            { }

            public string Exported
            { get; set; }
        }
    }
}
