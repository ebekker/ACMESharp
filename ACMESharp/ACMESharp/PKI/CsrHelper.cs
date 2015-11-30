namespace ACMESharp.PKI
{
    public class CsrHelper
    {
        /*
        public static readonly BigNumber E_3 = 3;
        public static readonly BigNumber E_F4 = 0x10001;

        public const int CSR_FORMAT_PEM = 0;
        public const int CSR_FORMAT_DER = 1;
        public const int CSR_FORMAT_PRINT = 2;

        public delegate int RsaKeyGeneratorCallback(int p, int n, object cbArg);

        public static RsaPrivateKey XXGenerateRsaPrivateKey(int bits = 2048, BigNumber e = null,
                RsaKeyGeneratorCallback cb = null, object cbArg = null)
        {
            if (e == null)
                e = E_F4;

            using (var rsa = new RSA())
            {
                BigNumber.GeneratorHandler cbWrapper = null;
                if (cb != null)
                    cbWrapper = (x,y,z) => cb(x,y,z);

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
                rsa.GenerateKeys(bits, e, cbWrapper, cbArg);

                using (var bio = BIO.MemoryBuffer())
                {
                    // Ref:  http://openssl.org/docs/manmaster/crypto/PEM_write_bio_RSAPrivateKey.html
                    rsa.WritePrivateKey(bio, enc, pwdCb, pwd);
                    return new RsaPrivateKey(bits, e.ToHexString(), bio.ReadString());
                }
            }
        }

        public static Csr XXGenerateCsr(CsrDetails csrDetails, RsaPrivateKey rsaKeyPair, string messageDigest = "SHA256")
        {
            var rsaKeys = CryptoKey.FromPrivateKey(rsaKeyPair.Pem, null);

            // Translate from our external form to our OpenSSL internal form
            // Ref:  https://www.openssl.org/docs/manmaster/crypto/X509_NAME_new.html
            var xn = new X509Name();
            if (!string.IsNullOrEmpty(csrDetails.CommonName         /** /)) xn.Common           = csrDetails.CommonName;       // CN;
            if (!string.IsNullOrEmpty(csrDetails.Country            /** /)) xn.Country          = csrDetails.Country;          // C;
            if (!string.IsNullOrEmpty(csrDetails.StateOrProvince    /** /)) xn.StateOrProvince  = csrDetails.StateOrProvince;  // ST;
            if (!string.IsNullOrEmpty(csrDetails.Locality           /** /)) xn.Locality         = csrDetails.Locality;         // L;
            if (!string.IsNullOrEmpty(csrDetails.Organization       /** /)) xn.Organization     = csrDetails.Organization;     // O;
            if (!string.IsNullOrEmpty(csrDetails.OrganizationUnit   /** /)) xn.OrganizationUnit = csrDetails.OrganizationUnit; // OU;
            if (!string.IsNullOrEmpty(csrDetails.Description        /** /)) xn.Description      = csrDetails.Description;      // D;
            if (!string.IsNullOrEmpty(csrDetails.Surname            /** /)) xn.Surname          = csrDetails.Surname;          // S;
            if (!string.IsNullOrEmpty(csrDetails.GivenName          /** /)) xn.Given            = csrDetails.GivenName;        // G;
            if (!string.IsNullOrEmpty(csrDetails.Initials           /** /)) xn.Initials         = csrDetails.Initials;         // I;
            if (!string.IsNullOrEmpty(csrDetails.Title              /** /)) xn.Title            = csrDetails.Title;            // T;
            if (!string.IsNullOrEmpty(csrDetails.SerialNumber       /** /)) xn.SerialNumber     = csrDetails.SerialNumber;     // SN;
            if (!string.IsNullOrEmpty(csrDetails.UniqueIdentifier   /** /)) xn.UniqueIdentifier = csrDetails.UniqueIdentifier; // UID;

            var xr = new X509Request(0, xn, rsaKeys);
            var md = MessageDigest.CreateByName(messageDigest); ;
            xr.Sign(rsaKeys, md);
            using (var bio = BIO.MemoryBuffer())
            {
                xr.Write(bio);
                return new Csr(bio.ReadString());
            }
        }

        public static int DefaultRsaKeyGeneratorCallback(int p, int n, object cbArg)
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


        private static string DefaultPasswordHandler(bool verify, object arg)
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
        */

    }
}
