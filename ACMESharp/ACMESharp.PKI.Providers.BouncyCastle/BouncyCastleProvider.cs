using ACMESharp.PKI.EC;
using ACMESharp.PKI.RSA;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
//using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.Providers
{
    /// <summary>
    /// Implementation of a <see cref="CertificateProvider"/> implemented using
    /// the <see cref="http://www.bouncycastle.org/csharp/index.html"
    /// >Bouncy Castle C# library</see>.
    /// </summary>
    /// <remarks>
    /// Using the Bouncy Castle (BC) library allows us to implement a purely
    /// managed provider that should work in most environments with minimum
    /// dependencies.
    /// </remarks>
    public class BouncyCastleProvider : CertificateProvider, IPkiTool
    {
        // Useful references and examples for BC:
        //  CSR:
        //    http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation
        //    https://gist.github.com/Venomed/5337717aadfb61b09e58
        //    http://codereview.stackexchange.com/questions/84752/net-bouncycastle-csr-and-private-key-generation
        //  Other:
        //    https://www.txedo.com/blog/java-read-rsa-keys-pem-file/

        #region -- Constants --

        public const string PROVIDER_NAME = "BouncyCastle";

        public const int RSA_BITS_DEFAULT = 2048;
        public const int RSA_BITS_MINIMUM = 1024 + 1; // LE no longer allows 1024-bit PrvKeys

        public static readonly BigInteger RSA_E_3 = BigInteger.Three;
        public static readonly BigInteger RSA_E_F4 = BigInteger.ValueOf(0x10001);

        // This is based on the BC RSA Generator code:
        //    https://github.com/bcgit/bc-csharp/blob/fba5af528ce7dcd0ac0513363196a62639b82a86/crypto/src/crypto/generators/RsaKeyPairGenerator.cs#L37
        protected const int DEFAULT_CERTAINTY = 100;

        #endregion -- Constants --

        #region -- Constructors --

        public BouncyCastleProvider(IReadOnlyDictionary<string, string> initParams)
            : base(initParams)
        { }

        #endregion -- Constructors --

        #region -- Methods --

        public override PrivateKey GeneratePrivateKey(PrivateKeyParams pkp)
        {
            var rsaPkParams = pkp as RsaPrivateKeyParams;
            var ecPkParams = pkp as EcPrivateKeyParams;

            if (rsaPkParams != null)
            {
                int bits;
                // Bits less than 1024 are weak Ref: http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html
                if (rsaPkParams.NumBits < RSA_BITS_MINIMUM)
                    bits = RSA_BITS_DEFAULT;
                else
                    bits = rsaPkParams.NumBits;

                BigInteger e;
                if (string.IsNullOrEmpty(rsaPkParams.PubExp))
                    e = RSA_E_F4;
                else if (rsaPkParams.PubExp.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    e = new BigInteger(rsaPkParams.PubExp, 16);
                else
                    e = new BigInteger(rsaPkParams.PubExp);

                var rsaKgp = new RsaKeyGenerationParameters(
                        e, new SecureRandom(), bits, DEFAULT_CERTAINTY);
                var rkpg = new RsaKeyPairGenerator();
                rkpg.Init(rsaKgp);
                AsymmetricCipherKeyPair ackp = rkpg.GenerateKeyPair();

                return new RsaPrivateKey(bits, e.ToString(16), ToPrivatePem(ackp));
            }
            else if (ecPkParams != null)
            {
                throw new NotImplementedException("EC private keys have not yet been implemented");

                //var curveName = Asn1Object.FromShortName("P-256");
                ////var curveName = new Asn1Object("P-256");
                //using (var ec =OpenSSL.Crypto.EC.Key.FromCurveName(curveName))
                //{
                //    ec.GenerateKey();
                //}
            }
            else
            {
                throw new NotSupportedException("unsupported private key parameter type");
            }
        }

        public override void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target)
        {
            var rsaPk = pk as RsaPrivateKey;

            if (rsaPk != null)
            {
                switch (fmt)
                {
                    case EncodingFormat.PEM:
                        var bytes = Encoding.UTF8.GetBytes(rsaPk.Pem);
                        target.Write(bytes, 0, bytes.Length);
                        break;
                    case EncodingFormat.DER:
                        using (var tr = new StringReader(rsaPk.Pem))
                        {
                            var pr = new PemReader(tr);
                            var pem = pr.ReadObject();
                            var ackp = pem as AsymmetricCipherKeyPair;

                            if (ackp != null)
                            {
                                var prv = ackp.Private as RsaPrivateCrtKeyParameters;
                                var der = PrivateKeyInfoFactory.CreatePrivateKeyInfo(prv).GetDerEncoded();
                                target.Write(der, 0, der.Length);
                                break;
                            }
                        }
                        throw new NotSupportedException("unsupported or invalid private key format");
                    default:
                        throw new NotSupportedException("unsupported encoding format");
                }
            }
            else
            {
                throw new NotSupportedException("unsupported private key type");
            }
        }

        public override PrivateKey ImportPrivateKey<PK>(EncodingFormat fmt, Stream source)
        {
            if (typeof(PK) == typeof(RsaPrivateKey))
            {
                using (var tr = new StreamReader(source))
                {
                    var pr = new PemReader(tr);
                    var pem = pr.ReadObject();
                    var ackp = pem as AsymmetricCipherKeyPair;

                    if (ackp != null)
                    {
                        var rsa = ackp.Private as RsaPrivateCrtKeyParameters;
                        if (rsa != null)
                        {
                            return new RsaPrivateKey(
                                    rsa.Modulus.BitLength,
                                    rsa.Exponent.ToString(16),
                                    ToPrivatePem(ackp));
                        }
                    }
                }

                throw new NotSupportedException("unsupported or invalid private key PEM content");
            }
            else
            {
                throw new NotSupportedException("unsupported private key type");
            }
        }

        public override Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md)
        {
            var csrDetails = csrParams.Details;
            var mdVal = Enum.GetName(typeof(Crt.MessageDigest), md);

            var attrs = new Dictionary<DerObjectIdentifier, string>();
            if (!string.IsNullOrEmpty(csrDetails.CommonName         /**/)) attrs.Add(X509Name.CN, csrDetails.CommonName);       // CN;
            if (!string.IsNullOrEmpty(csrDetails.Country            /**/)) attrs.Add(X509Name.C, csrDetails.Country);           // C;
            if (!string.IsNullOrEmpty(csrDetails.StateOrProvince    /**/)) attrs.Add(X509Name.ST, csrDetails.StateOrProvince);  // ST;
            if (!string.IsNullOrEmpty(csrDetails.Locality           /**/)) attrs.Add(X509Name.L, csrDetails.Locality);          // L;
            if (!string.IsNullOrEmpty(csrDetails.Organization       /**/)) attrs.Add(X509Name.O, csrDetails.Organization);      // O;
            if (!string.IsNullOrEmpty(csrDetails.OrganizationUnit   /**/)) attrs.Add(X509Name.OU, csrDetails.OrganizationUnit); // OU;
            if (!string.IsNullOrEmpty(csrDetails.Surname            /**/)) attrs.Add(X509Name.Surname, csrDetails.Surname);     // S;
            if (!string.IsNullOrEmpty(csrDetails.GivenName          /**/)) attrs.Add(X509Name.GivenName, csrDetails.GivenName); // G;
            if (!string.IsNullOrEmpty(csrDetails.Initials           /**/)) attrs.Add(X509Name.Initials, csrDetails.Initials);   // I;
            if (!string.IsNullOrEmpty(csrDetails.Title              /**/)) attrs.Add(X509Name.T, csrDetails.Title);                           // T;
            if (!string.IsNullOrEmpty(csrDetails.SerialNumber       /**/)) attrs.Add(X509Name.SerialNumber, csrDetails.SerialNumber);         // SN;
            if (!string.IsNullOrEmpty(csrDetails.UniqueIdentifier   /**/)) attrs.Add(X509Name.UniqueIdentifier, csrDetails.UniqueIdentifier); // UID;

            var subj = new X509Name(attrs.Keys.ToList(), attrs.Values.ToList());

            var rsaPk = pk as RsaPrivateKey;
            if (rsaPk != null)
            {
                using (var tr = new StringReader(rsaPk.Pem))
                {
                    var pr = new PemReader(tr);
                    var pem = pr.ReadObject();
                    var ackp = pem as AsymmetricCipherKeyPair;

                    if (ackp != null)
                    {
                        var sigAlg = $"{mdVal}withRSA";
                        var csrAttrs = new List<Asn1Encodable>();

                        if (csrDetails.AlternativeNames != null)
                        {
                            var gnames = new List<GeneralName>();

                            // Start off with the common name as the first alternative name
                            gnames.Add(new GeneralName(GeneralName.DnsName, csrDetails.CommonName));
                            // Combine with all subsequent alt names
                            foreach (var n in csrDetails.AlternativeNames)
                                gnames.Add(new GeneralName(GeneralName.DnsName, n));

                            var altNames = new GeneralNames(gnames.ToArray());
#pragma warning disable CS0612 // Type or member is obsolete
                            var x509Ext = new X509Extensions(new Hashtable
                            {
                                [X509Extensions.SubjectAlternativeName] = new X509Extension(false, new DerOctetString(altNames))
                            });
#pragma warning restore CS0612 // Type or member is obsolete

                            csrAttrs.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                                    PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                                    new DerSet(x509Ext)));
                        }

#pragma warning disable CS0618 // Type or member is obsolete
                        var csr = new Pkcs10CertificationRequest(sigAlg,
                                subj, ackp.Public, new DerSet(csrAttrs.ToArray()), ackp.Private);
#pragma warning restore CS0618 // Type or member is obsolete

                        var csrPem = ToCsrPem(csr);
                        return new Csr(csrPem);
                    }
                }
            }

            throw new NotSupportedException("unsupported private key type");
        }

        /// <summary>
        /// Support exporting CSR to <see cref="EncodingFormat.PEM">PEM</see>
        /// and <see cref="EncodingFormat.DER">DER</see> formats.
        /// </summary>
        public override void ExportCsr(Csr csr, EncodingFormat fmt, Stream target)
        {
            if (fmt == EncodingFormat.PEM)
            {
                var bytes = Encoding.UTF8.GetBytes(csr.Pem);
                target.Write(bytes, 0, bytes.Length);
            }
            else if (fmt == EncodingFormat.DER)
            {
                using (var tr = new StringReader(csr.Pem))
                {
                    PemReader pr = new PemReader(tr);
                    var bcCsr = (Pkcs10CertificationRequest)pr.ReadObject();
                    var derBytes = bcCsr.GetDerEncoded();
                    target.Write(derBytes, 0, derBytes.Length);
                }
            }
            else
            {
                throw new NotSupportedException("encoding format has not been implemented");
            }
        }

        public override Csr ImportCsr(EncodingFormat fmt, Stream source)
        {
            if (fmt == EncodingFormat.PEM)
            {
                using (var tr = new StreamReader(source))
                {
                    PemReader pr = new PemReader(tr);
                    var csr = (Pkcs10CertificationRequest)pr.ReadObject();
                    return new Csr(ToCsrPem(csr));
                }
            }
            else if (fmt == EncodingFormat.DER)
            {
                using (var ms = new MemoryStream())
                {
                    source.CopyTo(ms);
                    var csr = new Pkcs10CertificationRequest(ms.ToArray());
                    return new Csr(ToCsrPem(csr));
                }
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
        }

        public override Crt ImportCertificate(EncodingFormat fmt, Stream source)
        {
            if (false)
                throw new NotImplementedException();

            X509Certificate bcCert = null;

            if (fmt == EncodingFormat.DER)
            {
                var certParser = new X509CertificateParser();
                bcCert = certParser.ReadCertificate(source);
            }
            else if (fmt == EncodingFormat.PEM)
            {
                using (var tr = new StreamReader(source))
                {
                    var pr = new PemReader(tr);
                    bcCert = (X509Certificate)pr.ReadObject();
                }
            }
            else
            {
                throw new NotSupportedException("encoding format has not been implemented");
            }

            using (var tw = new StringWriter())
            {
                var pw = new PemWriter(tw);
                pw.WriteObject(bcCert);
                return new Crt { Pem = tw.GetStringBuilder().ToString() };
            }
        }

        public override void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target)
        {
            if (false)
                throw new NotImplementedException();

            if (fmt == EncodingFormat.PEM)
            {
                var bytes = Encoding.UTF8.GetBytes(cert.Pem);
                target.Write(bytes, 0, bytes.Length);
            }
            else if (fmt == EncodingFormat.DER)
            {
                X509Certificate bcCert = FromCertPem(cert.Pem);
                var der = bcCert.GetEncoded();
                target.Write(der, 0, der.Length);
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
        }

        public override void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password)
        {
            var rsaPk = pk as RsaPrivateKey;
            if (rsaPk == null)
                throw new NotSupportedException("unsupported private key type");

            if (fmt == ArchiveFormat.PKCS12)
            {
                var bcCerts = certs.Select(x =>
                        new X509CertificateEntry(FromCertPem(x.Pem))).ToArray();
                var bcPk = FromPrivatePem(rsaPk.Pem);

                var pfx = new Pkcs12Store();
                pfx.SetCertificateEntry(bcCerts[0].Certificate.ToString(), bcCerts[0]);
                pfx.SetKeyEntry(bcCerts[0].Certificate.ToString(),
                        new AsymmetricKeyEntry(bcPk.Private), new[] { bcCerts[0] });

                for (int i = 1; i < bcCerts.Length; ++i)
                {
                    pfx.SetCertificateEntry(bcCerts[i].Certificate.SubjectDN.ToString(),
                            bcCerts[i]);
                }

                pfx.Save(target, password?.ToCharArray(), new SecureRandom());
            }
            else
            {
                throw new NotSupportedException("unsupported archive format");
            }
        }

        private static string ToPrivatePem(AsymmetricCipherKeyPair ackp)
        {
            string pem;
            using (var tw = new StringWriter())
            {
                var pw = new PemWriter(tw);
                pw.WriteObject(ackp.Private);
                pem = tw.GetStringBuilder().ToString();
                tw.GetStringBuilder().Clear();
            }

            return pem;
        }

        private static AsymmetricCipherKeyPair FromPrivatePem(string pem)
        {
            using (var tr = new StringReader(pem))
            {
                var pr = new PemReader(tr);
                var ackp = (AsymmetricCipherKeyPair)pr.ReadObject();
                return ackp;
            }
        }

        private static string ToCsrPem(Pkcs10CertificationRequest csr)
        {
            string pem;
            using (var tw = new StringWriter())
            {
                var pw = new PemWriter(tw);
                pw.WriteObject(csr);
                pem = tw.GetStringBuilder().ToString();
                tw.GetStringBuilder().Clear();
            }

            return pem;
        }

        private static X509Certificate FromCertPem(string pem)
        {
            using (var tr = new StringReader(pem))
            {
                var pr = new PemReader(tr);
                return (X509Certificate)pr.ReadObject();
            }
        }

        #endregion -- Methods --
    }
}
