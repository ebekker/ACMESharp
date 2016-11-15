using ACMESharp.PKI.EC;
using ACMESharp.PKI.RSA;
using OpenSSL.Core;
using OSSL_RSA = OpenSSL.Crypto.RSA;
using OpenSSL.Crypto;
using OpenSSL.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ACMESharp.PKI.Providers
{
    // NOTE!  This same class is used in both the 32-bit and 64-bit OpenSSL
    //        Lib Certificate Providers via a project "linked" resource.

    public class OpenSslLibBaseProvider : CertificateProvider
    {
        public const int RSA_BITS_DEFAULT = 2048;
        public const int RSA_BITS_MINIMUM = 1024 + 1; // LE no longer allows 1024-bit PrvKeys

        public static readonly BigNumber RSA_E_3 = 3;
        public static readonly BigNumber RSA_E_F4 = 0x10001;

        public const int CSR_FORMAT_PEM = 0;
        public const int CSR_FORMAT_DER = 1;
        public const int CSR_FORMAT_PRINT = 2;

        /// <summary>
        /// Name of <code>subjectAlternativeName</code> (SAN) X509 extension.
        /// </summary>
        /// <remarks>
        /// For more details about SAN extension in OpenSSL, see this
        /// <see cref="https://www.openssl.org/docs/manmaster/apps/x509v3_config.html#Subject-Alternative-Name">man page</see>.
        /// </remarks>
        public const string EXT_NAME_SAN = "subjectAltName";

        // A subset of SAN Name Prefixes
        public const string EXT_SAN_PREFIX_DNS = "DNS";
        public const string EXT_SAN_PREFIX_EMAIL = "email";
        public const string EXT_SAN_PREFIX_IPADDR = "IP";

        public delegate int RsaKeyGeneratorCallback(int p, int n, object cbArg);

        public OpenSslLibBaseProvider(IReadOnlyDictionary<string, string> initParams)
            : base(initParams)
        { }

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

                BigNumber e;
                if (string.IsNullOrEmpty(rsaPkParams.PubExp))
                    e = RSA_E_F4;
                else if (rsaPkParams.PubExp.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    e = BigNumber.FromHexString(rsaPkParams.PubExp);
                else
                    e = BigNumber.FromDecimalString(rsaPkParams.PubExp);

                using (var rsa = new OSSL_RSA())
                {
                    BigNumber.GeneratorHandler cbWrapper = null;
                    if (rsaPkParams.Callback != null)
                        cbWrapper = (x, y, z) => rsaPkParams.Callback(x, y, z);

                    Cipher enc = null;
                    string pwd = null;
                    PasswordHandler pwdCb = null;
                    // If we choose to encrypt:
                    //      Cipher.DES_CBC;
                    //      Cipher.DES_EDE3_CBC;
                    //      Cipher.Idea_CBC;
                    //      Cipher.AES_128_CBC;
                    //      Cipher.AES_192_CBC;
                    //      Cipher.AES_256_CBC;
                    //   and pwd != null || pwdCb != null
                    // We can use a pwdCb to get a password interactively or we can
                    // simply pass in a fixed password string (no cbPwd, just pwd)
                    if (pwd != null)
                        pwdCb = DefaultPasswordHandler;

                    // Ref:  http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html
                    rsa.GenerateKeys(bits, e, cbWrapper, rsaPkParams.CallbackArg);

                    using (var bio = BIO.MemoryBuffer())
                    {
                        // Ref:  http://openssl.org/docs/manmaster/crypto/PEM_write_bio_RSAPrivateKey.html
                        rsa.WritePrivateKey(bio, enc, pwdCb, pwd);
                        return new RsaPrivateKey(bits, e.ToHexString(), bio.ReadString());
                    }
                }
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

        /// <summary>
        /// Support exporting CSR to <see cref="EncodingFormat.PEM">PEM</see> format.
        /// </summary>
        public override void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target)
        {
            var rsaPk = pk as RsaPrivateKey;
            if (rsaPk == null)
                throw new NotSupportedException("unsupported private key type");

            if (fmt == EncodingFormat.PEM)
            {
                var bytes = Encoding.UTF8.GetBytes(rsaPk.Pem);
                target.Write(bytes, 0, bytes.Length);
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
                using (BIO keyBio = BIO.MemoryBuffer())
                {
                    using (var ms = new MemoryStream())
                    {
                        source.CopyTo(ms);
                        keyBio.Write(ms.ToArray());
                    }

                    using (var key = CryptoKey.FromPrivateKey(keyBio, null))
                    {
                        var rsa = key.GetRSA();
                        return new RsaPrivateKey(rsa.Size,
                                rsa.PrivateExponent.ToHexString(),
                                rsa.PrivateKeyAsPEM);
                    }
                }
            }
            else
            {
                throw new NotSupportedException("unsupported private key type");
            }
        }

        public override Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md)
        {
            var mdVal = Enum.GetName(typeof(Crt.MessageDigest), md);
            var rsaPk = pk as RsaPrivateKey;

            if (rsaPk != null)
                return GenerateCsr(csrParams.Details, (RsaPrivateKey)pk, mdVal);

            throw new NotSupportedException("unsupported private key type");
        }

        /// <summary>
        /// Support importing CSR from <see cref="EncodingFormat.PEM">PEM</see>
        /// and <see cref="EncodingFormat.DER">DER</see> formats.
        /// </summary>
        public override Csr ImportCsr(EncodingFormat fmt, Stream source)
        {
            if (fmt == EncodingFormat.PEM)
            {
                using (var r = new StreamReader(source))
                {
                    using (var x509 = new X509Request(r.ReadToEnd()))
                    {
                        var csr = new Csr(x509.PEM);
                        return csr;
                    }
                }
            }
            else if (fmt == EncodingFormat.DER)
            {
                // TODO: Managed OpenSSL Library has not implemented
                // d2i_X509_REQ_bio(...) routine yet
                throw new NotImplementedException("x509 CSR import to DER has not yet been implemented");
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
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
                using (var xr = new X509Request(csr.Pem))
                {
                    using (var bio = BIO.MemoryBuffer())
                    {
                        xr.Write_DER(bio);
                        var arr = bio.ReadBytes((int)bio.BytesPending);
                        target.Write(arr.Array, arr.Offset, arr.Count);
                    }
                }
            }
            else
            {
                throw new NotSupportedException("encoding format has not been implemented");
            }
        }

        /// <summary>
        /// Supports importing certificates from <see cref="EncodingFormat.PEM">PEM</see>
        /// and <see cref="EncodingFormat.DER">DER</see> formats.
        /// </summary>
        public override Crt ImportCertificate(EncodingFormat fmt, Stream source)
        {
            if (fmt == EncodingFormat.DER)
            {
                using (var ms = new MemoryStream())
                {
                    source.CopyTo(ms);
                    using (var bio = BIO.MemoryBuffer())
                    {
                        bio.Write(ms.ToArray());
                        using (var x509 = X509Certificate.FromDER(bio))
                        {
                            return new Crt
                            {
                                Pem = x509.PEM
                            };
                        }
                    }
                }
            }
            else if (fmt == EncodingFormat.PEM)
            {
                using (var r = new StreamReader(source))
                {
                    using (var bio = BIO.MemoryBuffer())
                    {
                        bio.Write(r.ReadToEnd());
                        using (var x509 = new X509Certificate(bio))
                        {
                            return new Crt
                            {
                                Pem = x509.PEM
                            };
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException("encoding format has not been implemented");
            }
        }

        /// <summary>
        /// Support exporting certificates to <see cref="EncodingFormat.PEM">PEM</see>
        /// and <see cref="EncodingFormat.DER">DER</see> formats.
        /// </summary>
        public override void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target)
        {
            if (fmt == EncodingFormat.PEM)
            {
                var bytes = Encoding.UTF8.GetBytes(cert.Pem);
                target.Write(bytes, 0, bytes.Length);
            }
            else if (fmt == EncodingFormat.DER)
            {
                using (BIO bioPem = BIO.MemoryBuffer())
                {
                    bioPem.Write(cert.Pem);
                    using (var x509 = new X509Certificate(bioPem))
                    {
                        var bytes = x509.DER;
                        target.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
        }

        public override void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs, ArchiveFormat fmt, Stream target, string password = "")
        {
            var rsaPk = pk as RsaPrivateKey;
            if (rsaPk == null)
                throw new NotSupportedException("unsupported private key type");

            if (fmt == ArchiveFormat.PKCS12)
            {
                var x509Arr = certs.Select(x =>
                {
                    using (var bio = BIO.MemoryBuffer())
                    {
                        bio.Write(x.Pem);
                        return new X509Certificate(bio);
                    }
                }).ToArray();

                using (var key = CryptoKey.FromPrivateKey(rsaPk.Pem, null))
                {
                    var caStack = new OpenSSL.Core.Stack<X509Certificate>();
                    for (int i = 1; i < x509Arr.Length; ++i)
                        caStack.Add(x509Arr[i]);

                    using (var pfx = new PKCS12(password == string.Empty ? null : password, key, x509Arr[0], caStack))
                    {
                        using (var bio = BIO.MemoryBuffer())
                        {
                            pfx.Write(bio);
                            var count = (int)bio.BytesPending;
                            var array = bio.ReadBytes(count);
                            target.Write(array.Array, 0, count);
                        }
                    }
                }

                foreach (var x in x509Arr)
                    x.Dispose();
            }
            else
            {
                throw new NotSupportedException("unsupported archive format");
            }
        }

        protected Csr GenerateCsr(CsrDetails csrDetails, RsaPrivateKey rsaKeyPair, string messageDigest = "SHA256")
        {
            var rsaKeys = CryptoKey.FromPrivateKey(rsaKeyPair.Pem, null);

            // Translate from our external form to our OpenSSL internal form
            // Ref:  https://www.openssl.org/docs/manmaster/crypto/X509_NAME_new.html
            var xn = new X509Name();
            if (!string.IsNullOrEmpty(csrDetails.CommonName         /**/)) xn.Common = csrDetails.CommonName;       // CN;
            if (!string.IsNullOrEmpty(csrDetails.Country            /**/)) xn.Country = csrDetails.Country;          // C;
            if (!string.IsNullOrEmpty(csrDetails.StateOrProvince    /**/)) xn.StateOrProvince = csrDetails.StateOrProvince;  // ST;
            if (!string.IsNullOrEmpty(csrDetails.Locality           /**/)) xn.Locality = csrDetails.Locality;         // L;
            if (!string.IsNullOrEmpty(csrDetails.Organization       /**/)) xn.Organization = csrDetails.Organization;     // O;
            if (!string.IsNullOrEmpty(csrDetails.OrganizationUnit   /**/)) xn.OrganizationUnit = csrDetails.OrganizationUnit; // OU;
            if (!string.IsNullOrEmpty(csrDetails.Description        /**/)) xn.Description = csrDetails.Description;      // D;
            if (!string.IsNullOrEmpty(csrDetails.Surname            /**/)) xn.Surname = csrDetails.Surname;          // S;
            if (!string.IsNullOrEmpty(csrDetails.GivenName          /**/)) xn.Given = csrDetails.GivenName;        // G;
            if (!string.IsNullOrEmpty(csrDetails.Initials           /**/)) xn.Initials = csrDetails.Initials;         // I;
            if (!string.IsNullOrEmpty(csrDetails.Title              /**/)) xn.Title = csrDetails.Title;            // T;
            if (!string.IsNullOrEmpty(csrDetails.SerialNumber       /**/)) xn.SerialNumber = csrDetails.SerialNumber;     // SN;
            if (!string.IsNullOrEmpty(csrDetails.UniqueIdentifier   /**/)) xn.UniqueIdentifier = csrDetails.UniqueIdentifier; // UID;

            var xr = new X509Request(0, xn, rsaKeys);
            if (csrDetails.AlternativeNames != null)
            {
                // Format the common name as the first alternative name
                var commonName = $"{EXT_SAN_PREFIX_DNS}:{xn.Common}";

                // Concat with all subsequent alternative names
                var altNames = commonName + string.Join("", csrDetails.AlternativeNames.Select(
                        x => $",{EXT_SAN_PREFIX_DNS}:{x}"));

                // Assemble and add the SAN extension value
                var extensions = new OpenSSL.Core.Stack<X509Extension>();
                extensions.Add(new X509Extension(xr, EXT_NAME_SAN, false, altNames));
                xr.AddExtensions(extensions);
            }

            var md = MessageDigest.CreateByName(messageDigest);
            xr.Sign(rsaKeys, md);
            using (var bio = BIO.MemoryBuffer())
            {
                xr.Write(bio);
                return new Csr(bio.ReadString());
            }
        }

        protected int DefaultRsaKeyGeneratorCallback(int p, int n, object cbArg)
        {
            var cout = cbArg as TextWriter;
            if (cout == null)
                cout = Console.Error;

            switch (p)
            {
                case 0: cout.Write('.'); break;
                case 1: cout.Write('+'); break;
                case 2: cout.Write('*'); break;
                case 3: cout.WriteLine(); break;
            }

            return 1;
        }

        protected string DefaultPasswordHandler(bool verify, object arg)
        {
            var passout = arg as string;

            if (!string.IsNullOrEmpty(passout))
                return File.ReadAllText(passout);

            while (true)
            {
                Console.Error.Write("Enter pass phrase:");
                var strPassword = ReadPassword();
                Console.Error.WriteLine();

                if (strPassword.Length == 0)
                    continue;

                if (!verify)
                    return strPassword;

                Console.Error.Write("Verifying - Enter pass phrase:");
                var strVerify = ReadPassword();
                Console.Error.WriteLine();

                if (strPassword == strVerify)
                    return strPassword;

                Console.Error.WriteLine("Passwords don't match, try again.");
            }
        }

        protected string ReadPassword()
        {
            Console.TreatControlCAsInput = true;
            var sb = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                    break;

                if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
                {
                    Console.Error.WriteLine();
                    throw new Exception("Canceled");
                }

                sb.Append(key.KeyChar);
            }

            Console.TreatControlCAsInput = false;

            return sb.ToString();
        }
    }
}
