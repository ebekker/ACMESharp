using ACMESharp.PKI.RSA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ACMESharp.PKI.Providers
{
    /// <summary>
    /// Implementation of a <see cref="CertificateProvider"/> that uses process calls
    /// out to the command-line interface (CLI) OpenSSL executable (openssl.exe).
    /// </summary>
    /// <remarks>
    /// The documentation to the expected CLI interface can be found
    /// <see cref="https://www.openssl.org/docs/manmaster/apps/openssl.html">here</see>.
    /// <para>
    /// This provider should work with any callable executable or script that accepts
    /// the same CLI interface as the standard OpenSSL executable.  This includes
    /// derivative projects such as the <see
    /// cref="http://www.openbsd.org/cgi-bin/man.cgi?query=openssl&sektion=1"
    /// >CLI tool</see> of the <see cref="http://www.libressl.org/">LibreSSL project</see>.
    /// It may also include batch or shell scripts that can act as wrappers to a native tool.
    /// </para>
    /// </remarks>
    public class OpenSslCliProvider : CertificateProvider
    {
        public const string PROVIDER_NAME = "OpenSSL-CLI";

        public const int RSA_BITS_DEFAULT = 2048;
        public const int RSA_BITS_MINIMUM = 1024 + 1; // LE no longer allows 1024-bit PrvKeys


        private static readonly Regex PKEY_BITS_REGEX =
                new Regex("Public-Key: \\(([0-9]+) bit\\)");
        private static readonly Regex PKEY_PUBEXP_REGEX =
                new Regex("Exponent: ([0-9]+) \\((0x[0-9]+)\\)");

        public const string PARAM_CLI_PATH = nameof(CliPath);
        public const string PARAM_CLI_WAIT_TIMEOUT = nameof(CliWaitTimeout);

        public const string THIS_ASM_DIR_SUBST = "%THIS_ASM_DIR%";

        /// <summary>
        /// The full path to the CLI binary executable.
        /// </summary>
        public string CliPath
        { get; set; } = $"{THIS_ASM_DIR_SUBST}\\openssl-win32-bin\\openssl.exe";

        /// <summary>
        /// The amount of time (in ms) to wait for any invocation of the CLI before timing
        /// out and terminating.  Specify zero (0) to wait indefinitely.
        /// </summary>
        public int CliWaitTimeout
        { get; set; } = 30 * 1000;


        public OpenSslCliProvider(IReadOnlyDictionary<string, string> initParams)
            : base(initParams)
        {
            if (initParams.ContainsKey(PARAM_CLI_PATH))
                CliPath = initParams[PARAM_CLI_PATH];

            if (initParams.ContainsKey(PARAM_CLI_WAIT_TIMEOUT))
                CliWaitTimeout = int.Parse(initParams[PARAM_CLI_WAIT_TIMEOUT]);
        }


        // http://blog.endpoint.com/2014/10/openssl-csr-with-alternative-names-one.html

        public override PrivateKey GeneratePrivateKey(PrivateKeyParams pkp)
        {
            var rsaPkp = pkp as RsaPrivateKeyParams;

            if (rsaPkp != null)
            {
                var tempKeyFile = Path.GetTempFileName();

                try
                {
                    var args = $"genpkey -algorithm RSA -out {tempKeyFile}";
                    if (!string.IsNullOrEmpty(rsaPkp.PubExp))
                        args += $" -pkeyopt rsa_keygen_pubexp:{rsaPkp.PubExp}";

                    var numBits = RSA_BITS_DEFAULT;
                    if (rsaPkp.NumBits >= RSA_BITS_MINIMUM)
                        numBits = rsaPkp.NumBits;
                    args += $" -pkeyopt rsa_keygen_bits:{numBits}";

                    RunCli(args);
                    var rsaPk = new RsaPrivateKey(rsaPkp.NumBits, rsaPkp.PubExp, File.ReadAllText(tempKeyFile));
                    return rsaPk;
                }
                finally
                {
                    File.Delete(tempKeyFile);
                }
            }

            throw new NotSupportedException("unsupported private key parameters type");
        }

        public override void ExportPrivateKey(PrivateKey pk, EncodingFormat fmt, Stream target)
        {
            var rsaPk = pk as RsaPrivateKey;

            if (rsaPk != null)
            {
                switch (fmt)
                {
                    case EncodingFormat.PEM:
                        var pemBytes = Encoding.UTF8.GetBytes(rsaPk.Pem);
                        target.Write(pemBytes, 0, pemBytes.Length);
                        break;
                    case EncodingFormat.DER:
                        var tempPem = Path.GetTempFileName();
                        var tempDer = Path.GetTempFileName();
                        try
                        {
                            File.WriteAllText(tempPem, rsaPk.Pem);
                            RunCli($"pkey -inform PEM -outform DER -in {tempPem} -out {tempDer}");
                            var derBytes = File.ReadAllBytes(tempDer);
                            target.Write(derBytes, 0, derBytes.Length);
                        }
                        finally
                        {
                            File.Delete(tempPem);
                            File.Delete(tempDer);
                        }
                        break;
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
                var tempSource = Path.GetTempFileName();
                var tempParams = Path.GetTempFileName();
                var tempTarget = Path.GetTempFileName();

                try
                {
                    using (var fs = new FileStream(tempSource, FileMode.Create))
                    {
                        source.CopyTo(fs);
                    }

                    string inform;
                    switch (fmt)
                    {
                        case EncodingFormat.PEM:
                            inform = "PEM";
                            break;
                        case EncodingFormat.DER:
                            inform = "DER";
                            break;
                        default:
                            throw new NotSupportedException("unsupported encoding format");
                    }

                    RunCli($"pkey -inform {inform} -in {tempSource} -out {tempTarget} -outform PEM");
                    RunCli($"pkey -inform {inform} -in {tempSource} -out {tempParams} -text_pub -noout");

                    var pemAllText = File.ReadAllText(tempTarget);
                    var paramsLines = File.ReadAllLines(tempParams);
                    var bits = 0;
                    var pubExp = string.Empty;

                    // The "-text_pub -noout" switches will dump the Modulus and some
                    // meta data; we sift through the all of it looking for the meta
                    // data and extracting it out using regular expression patterns
                    //    Public-Key: (2048 bit)
                    //    Modulus:
                    //        00:94:01:45:c4:d1:76:6f:2b:4c:1d:20:c3:73:55:
                    //        e2:69:ea:1e:c2:cc:22:26:93:31:45:b8:cc:95:bc:
                    //        ba:42:19:ab:1c:31:a3:6b:59:80:5a:e4:ee:5b:be:
                    //        f3:51:00:89:e3:da:f1:a4:f3:de:2b:27:13:e4:ff:
                    //        3a:67:8c:e9:63:9c:3b:3f:8e:48:cf:18:05:90:a4:
                    //        20:ad:8f:12:b6:17:87:4f:1e:fb:85:da:97:97:02:
                    //        e5:1c:71:f2:7b:68:86:b5:7e:05:d9:fa:51:11:1a:
                    //        58:ef:39:c8:60:5c:78:a6:c3:47:26:78:2b:51:e6:
                    //        4c:40:03:7a:36:b2:a0:7e:55:d4:28:0d:ea:b9:24:
                    //        b0:91:d8:35:96:6f:8d:0a:9e:2a:5d:ac:fa:7c:2f:
                    //        78:2e:20:39:a4:33:0b:bd:30:b9:39:cf:85:bb:7a:
                    //        d6:5e:6d:32:2b:ba:03:1c:f8:bb:cc:36:1b:72:02:
                    //        ba:ba:ad:eb:a0:c0:b1:64:45:09:12:ab:a7:2c:de:
                    //        e2:cc:5b:02:b9:b9:2e:73:11:63:39:99:1b:d4:6e:
                    //        1d:24:a5:e5:0f:04:2a:27:e4:01:2a:ea:b7:51:fc:
                    //        b8:63:c1:98:4b:b9:da:f8:67:44:aa:3c:c2:60:99:
                    //        f6:26:7e:fc:08:b5:10:e4:c3:e2:ac:d8:d4:4a:2d:
                    //        5f:0f
                    //    Exponent: 65537 (0x10001)


                    Match m;
                    foreach (var line in paramsLines)
                    {
                        if ((m = PKEY_BITS_REGEX.Match(line)).Success)
                            bits = int.Parse(m.Groups[1].Value);
                        else if ((m = PKEY_PUBEXP_REGEX.Match(line)).Success)
                            pubExp = m.Groups[2].Value;
                    }

                    return new RsaPrivateKey(bits, pubExp, File.ReadAllText(tempTarget));

                }
                finally
                {
                    File.Delete(tempSource);
                    File.Delete(tempTarget);
                }
            }
            else
            {
                throw new NotSupportedException("unsupported private key type");
            }
        }

        public override Csr GenerateCsr(CsrParams csrParams, PrivateKey pk, Crt.MessageDigest md)
        {
            var rsaPk = pk as RsaPrivateKey;

            if (rsaPk != null)
            {
                var tempCfgFile = Path.GetTempFileName();
                var tempKeyFile = Path.GetTempFileName();
                var tempCsrFile = Path.GetTempFileName();

                try
                {
                    var mdVal = Enum.GetName(typeof(Crt.MessageDigest), md);

                    var args = $"req -batch -new -keyform PEM -key {tempKeyFile} -{mdVal}";
                    args += $" -config {tempCfgFile} -outform PEM -out {tempCsrFile} -subj \"";

                    var subjParts = new[]
                    {
                        new { name = "C",            value = csrParams?.Details?.Country },
                        new { name = "ST",           value = csrParams?.Details?.StateOrProvince },
                        new { name = "L",            value = csrParams?.Details?.Locality },
                        new { name = "O",            value = csrParams?.Details?.Organization },
                        new { name = "OU",           value = csrParams?.Details?.OrganizationUnit},
                        new { name = "CN",           value = csrParams?.Details?.CommonName },
                        new { name = "emailAddress", value = csrParams?.Details?.Email },
                    };

                    // Escape any non-standard character
                    var re = new Regex("[^A-Za-z0-9\\._-]");
                    foreach (var sp in subjParts)
                    {
                        if (!string.IsNullOrEmpty(sp.value))
                        {
                            var spVal = re.Replace(sp.value, "\\$0");
                            args += $"/{sp.name}={spVal}";
                        }
                    }
                    args += "\"";

                    File.WriteAllText(tempKeyFile, rsaPk.Pem);
                    using (Stream source = Assembly.GetExecutingAssembly()
                            .GetManifestResourceStream(typeof(OpenSslCliProvider),
                                    "OpenSslCliProvider_Config.txt"),
                            target = new FileStream(tempCfgFile, FileMode.Create))
                    {
                        source.CopyTo(target);
                    }

                        RunCli(args);
                    var csr = new Csr(File.ReadAllText(tempCsrFile));
                    return csr;
                }
                finally
                {
                    File.Delete(tempCfgFile);
                    File.Delete(tempKeyFile);
                    File.Delete(tempCsrFile);
                }
            }

            throw new NotSupportedException("unsupported private key type");
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

            var tempSource = Path.GetTempFileName();
            var tempTarget = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempSource, csr.Pem);
                RunCli($"req -inform PEM -in {tempSource} -outform {outform} -out {tempTarget}");
                var targetBytes = File.ReadAllBytes(tempTarget);
                target.Write(targetBytes, 0, targetBytes.Length);
            }
            finally
            {
                File.Delete(tempSource);
                File.Delete(tempTarget);
            }
        }

        public override Csr ImportCsr(EncodingFormat fmt, Stream source)
        {
            string inform;
            switch (fmt)
            {
                case EncodingFormat.PEM:
                    inform = "PEM";
                    break;
                case EncodingFormat.DER:
                    inform = "DER";
                    break;
                default:
                    throw new NotSupportedException("unsupported encoding format");
            }

            var tempSource = Path.GetTempFileName();
            var tempTarget = Path.GetTempFileName();

            try
            {
                using (var fs = new FileStream(tempSource, FileMode.Create))
                {
                    source.CopyTo(fs);
                }

                RunCli($"req -inform {inform} -in {tempSource} -outform PEM -out {tempTarget}");
                var csr = new Csr(File.ReadAllText(tempTarget));
                return csr;
            }
            finally
            {
                File.Delete(tempSource);
                File.Delete(tempTarget);
            }
        }

        public override void ExportCertificate(Crt cert, EncodingFormat fmt, Stream target)
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

            var tempSource = Path.GetTempFileName();
            var tempTarget = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempSource, cert.Pem);
                RunCli($"x509 -inform PEM -in {tempSource} -outform {outform} -out {tempTarget}");
                var targetBytes = File.ReadAllBytes(tempTarget);
                target.Write(targetBytes, 0, targetBytes.Length);
            }
            finally
            {
                File.Delete(tempSource);
                File.Delete(tempTarget);
            }
        }

        public override Crt ImportCertificate(EncodingFormat fmt, Stream source)
        {
            string inform;
            switch (fmt)
            {
                case EncodingFormat.PEM:
                    inform = "PEM";
                    break;
                case EncodingFormat.DER:
                    inform = "DER";
                    break;
                default:
                    throw new NotSupportedException("unsupported encoding format");
            }

            var tempSource = Path.GetTempFileName();
            var tempTarget = Path.GetTempFileName();

            try
            {
                using (var fs = new FileStream(tempSource, FileMode.Create))
                {
                    source.CopyTo(fs);
                }

                RunCli($"x509 -inform {inform} -in {tempSource} -outform PEM -out {tempTarget}");
                var crt = new Crt
                {
                    Pem = File.ReadAllText(tempTarget)
                };
                return crt;
            }
            finally
            {
                File.Delete(tempSource);
                File.Delete(tempTarget);
            }
        }

        public override void ExportArchive(PrivateKey pk, IEnumerable<Crt> certs,
                ArchiveFormat fmt, Stream target, string password = "")
        {
            var rsaPk = pk as RsaPrivateKey;
            var certArr = certs.ToArray();

            if (fmt != ArchiveFormat.PKCS12)
                throw new NotSupportedException("unsupported archive format");

            if (rsaPk != null)
            {
                var tempPfxFile = Path.GetTempFileName();
                var tempInpFile = Path.GetTempFileName();

                try
                {
                    File.WriteAllText(tempInpFile, rsaPk.Pem);
                    foreach (var c in certs)
                        File.AppendAllText(tempInpFile, c.Pem);

                    RunCli($"pkcs12 -export -in {tempInpFile} -out {tempPfxFile} -nodes -passout pass:{password}");

                    var pfxBytes = File.ReadAllBytes(tempPfxFile);
                    target.Write(pfxBytes, 0, pfxBytes.Length);
                }
                finally
                {
                    File.Delete(tempPfxFile);
                    File.Delete(tempInpFile);
                }
            }
            else
            {
                throw new NotSupportedException("unsupported private key type");
            }
        }

        private void RunCli(string args)
        {
            var cliPath = CliPath;
            if (cliPath.Contains(THIS_ASM_DIR_SUBST))
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var asmDir = Path.GetDirectoryName(asmPath);
                cliPath = cliPath.Replace(THIS_ASM_DIR_SUBST, asmDir);
            }

            var psi = new ProcessStartInfo(cliPath, args);
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            using (var proc = Process.Start(psi))
            {
                if (CliWaitTimeout == 0)
                    proc.WaitForExit();
                else
                    proc.WaitForExit(CliWaitTimeout);

                if (!proc.HasExited)
                {
                    proc.Kill();
                    throw new TimeoutException("process was terminated due to timeout");
                }

                if (proc.ExitCode != 0)
                {
                    throw new ApplicationException("process returned failure");
                }

                proc.StandardError.ReadToEnd();
                proc.StandardOutput.ReadToEnd();
            }
        }
    }
}
