using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenSSL.Crypto;
using OpenSSL.Core;
using System.Text;
using System.IO;
using OpenSSL.X509;

namespace ACMESharp.OpenSSL
{
    [TestClass]
    public class OpenSslUnitTests
    {
        [TestMethod]
        [Ignore]
        public void TestGenRSA()
        {
            BigNumber e = null;
            //if (options.IsSet("3"))
            //    e = 3;
            //else if (options.IsSet("f4"))
            //    e = 0x10001;
            e = 0x10001;

            var rsagen = new RSA();
            rsagen.GenerateKeys(2048, e, GeneratorHandler, null);

            Cipher enc = null;
            //if (options.IsSet("des"))
            //    enc = Cipher.DES_CBC;
            //else if (options.IsSet("des3"))
            //    enc = Cipher.DES_EDE3_CBC;
            //else if (options.IsSet("idea"))
            //    enc = Cipher.Idea_CBC;
            //else if (options.IsSet("aes128"))
            //    enc = Cipher.AES_128_CBC;
            //else if (options.IsSet("aes192"))
            //    enc = Cipher.AES_192_CBC;
            //else if (options.IsSet("aes256"))
            //    enc = Cipher.AES_256_CBC;

            string passwd = null;

            using (var bio = BIO.MemoryBuffer())
            {
                rsagen.WritePrivateKey(bio, enc, OnPassword, passwd);

                var outfile = "openssl-rsagen-privatekey.txt";
                if (string.IsNullOrEmpty(outfile))
                    Console.WriteLine(bio.ReadString());
                else
                    File.WriteAllText(outfile, bio.ReadString());
            }

            using (var bio = BIO.MemoryBuffer())
            {
                rsagen.WritePublicKey(bio);

                var outfile = "openssl-rsagen-publickey.txt";
                if (string.IsNullOrEmpty(outfile))
                    Console.WriteLine(bio.ReadString());
                else
                    File.WriteAllText(outfile, bio.ReadString());
            }
        }

        [TestMethod]
        [Ignore]
        public void TestGenCSR()
        {
            var pem = File.ReadAllText("openssl-rsagen-privatekey.txt");
            var rsa = CryptoKey.FromPrivateKey(pem, null);
            //pem = File.ReadAllText("openssl-rsagen-publickey.txt");
            //rsa = CryptoKey.FromPublicKey(pem, null);

            var nam = new X509Name();
            nam.Common = "FOOBAR";
            nam.Country = "US";

            

            var csr = new X509Request();
            csr.PublicKey = rsa;
            csr.Subject = nam;
            csr.Sign(rsa, MessageDigest.SHA256);

            File.WriteAllText("openssl-requ-csr.txt", csr.PEM);
            using (var bioOut = BIO.MemoryBuffer())
            {
                csr.Write_DER(bioOut);
                var arr = bioOut.ReadBytes((int)bioOut.BytesPending);

                File.WriteAllBytes("openssl-requ-csr.der", arr.Array);
            }

            //using (var bioIn = BIO.MemoryBuffer())
            //{
            //    var pem2 = File.ReadAllText("openssl-requ-csr.txt");
            //    bioIn.Write(pem2);

            //    var csr = new X509Request()
            //    var x509 = new X509Certificate(bioIn);

            //}
        }

        int GeneratorHandler(int p, int n, object arg)
        {
            var cout = Console.Error;

            switch (p)
            {
                case 0: cout.Write('.'); break;
                case 1: cout.Write('+'); break;
                case 2: cout.Write('*'); break;
                case 3: cout.WriteLine(); break;
            }

            return 1;
        }

        private static string ReadPassword()
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

        public static string OnPassword(bool verify, object arg)
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
    }
}
