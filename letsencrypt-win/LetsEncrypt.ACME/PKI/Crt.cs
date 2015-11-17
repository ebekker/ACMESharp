using OpenSSL.Core;
using OpenSSL.Crypto;
using OpenSSL.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.PKI
{

    public class Crt
    {
        public string Pem
        { get; set; }

        public enum MessageDigest
        {
            SHA256
        }

        //public static void ConvertDerToPem(Stream source, Stream target)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        source.CopyTo(ms);
        //        using (var bio = BIO.MemoryBuffer())
        //        {
        //            bio.Write(ms.ToArray());
        //            using (var crt = X509Certificate.FromDER(bio))
        //            {
        //                var pemBytes = Encoding.UTF8.GetBytes(crt.PEM);
        //                target.Write(pemBytes, 0, pemBytes.Length);
        //            }
        //        }
        //    }
        //}

        //public static void ConvertDerToPem(string sourcePath, string targetPath,
        //        FileMode fileMode = FileMode.Create)
        //{
        //    using (FileStream source = new FileStream(sourcePath, FileMode.Open),
        //            target = new FileStream(targetPath, fileMode))
        //    {
        //        ConvertDerToPem(source, target);
        //    }
        //}

        /*
        /// <summary>
        /// Converts a certificate and private key to a PKCS#12 (.PFX) file.
        /// </summary>
        public static void ConvertToPfx(Stream keyPemSource, Stream crtPemSource, Stream isrPemSource, Stream pfxTarget)
        {
            using (BIO keyBio = BIO.MemoryBuffer(),
                    crtBio = BIO.MemoryBuffer(),
                    isrBio = BIO.MemoryBuffer())
            {
                using (var ms = new MemoryStream())
                {
                    keyPemSource.CopyTo(ms);
                    keyBio.Write(ms.ToArray());
                }
                using (var ms = new MemoryStream())
                {
                    crtPemSource.CopyTo(ms);
                    crtBio.Write(ms.ToArray());
                }
                using (var ms = new MemoryStream())
                {
                    isrPemSource.CopyTo(ms);
                    isrBio.Write(ms.ToArray());
                }

                using (var key = CryptoKey.FromPrivateKey(keyBio, null))
                {
                    using (var crt = new X509Certificate(crtBio))
                    {
                        using (var isr = new X509Certificate(isrBio))
                        {
                            var isrStack = new OpenSSL.Core.Stack<X509Certificate>();
                            isrStack.Add(isr);

                            using (var pfx = new PKCS12(null, key, crt, isrStack))
                            {
                                using (var pfxBio = BIO.MemoryBuffer())
                                {
                                    pfx.Write(pfxBio);
                                    var arr = pfxBio.ReadBytes((int)pfxBio.BytesPending);
                                    pfxTarget.Write(arr.Array, arr.Offset, arr.Count);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ConvertToPfx(string keyPemFile, string crtPemFile, string isrPemFile, string pfxFile,
                FileMode fileMode = FileMode.Create)
        {
            using (FileStream keyFs = new FileStream(keyPemFile, FileMode.Open),
                    crtFs = new FileStream(crtPemFile, FileMode.Open),
                    isrFs = new FileStream(isrPemFile, FileMode.Open),
                    pfxFs = new FileStream(pfxFile, fileMode))
            {
                ConvertToPfx(keyFs, crtFs, isrFs, pfxFs);
            }
        }
        */
    }
}
