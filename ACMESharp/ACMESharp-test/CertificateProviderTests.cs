using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ACMESharp.PKI;
using ACMESharp.PKI.RSA;

namespace ACMESharp
{
    [TestClass]
    public class CertificateProviderTests
    {
        private static IPkiTool GetCP()
        {
            //CertificateProvider.RegisterProvider<ACMESharp.PKI.Providers.OpenSslCliProvider>();
            //CertificateProvider.RegisterProvider<ACMESharp.PKI.Providers.CertEnrollProvider>();
            //CertificateProvider.RegisterProvider<ACMESharp.PKI.Providers.BouncyCastleProvider>();

            //return CertificateProvider.GetProvider(ACMESharp.PKI.Providers.BouncyCastleProvider.PROVIDER_NAME);
            return PkiToolExtManager.GetPkiTool(ACMESharp.PKI.Providers.BouncyCastleProvider.PROVIDER_NAME);
        }

        [TestMethod]
        public void TestGenerateRsaPrivateKey()
        {
            using (var cp = GetCP())
            {
                var pkp = new RsaPrivateKeyParams();
                var pk = cp.GeneratePrivateKey(pkp);

                Assert.IsInstanceOfType(pk, typeof(RsaPrivateKey));

                var rsaPk = (RsaPrivateKey)pk;

                // TODO:  verify the components of RSA PK?
                //rsaPk.BigNumber
                //rsaPk.Bits
                //rsaPk.E
            }
        }

        [TestMethod]
        public void TestExportRsaPrivateKeyAsPem()
        {
            TestExportRsaPrivateKey(EncodingFormat.PEM);
        }

        [TestMethod]
        public void TestExportRsaPrivateKeyAsDer()
        {
            TestExportRsaPrivateKey(EncodingFormat.DER);
        }

        private void TestExportRsaPrivateKey(EncodingFormat fmt)
        {
            using (var cp = GetCP())
            {
                var pkp = new RsaPrivateKeyParams();
                var pk = cp.GeneratePrivateKey(pkp);

                using (var target = new MemoryStream())
                {
                    cp.ExportPrivateKey(pk, fmt, target);
                }
            }
        }

        [TestMethod]
        public void TestImportRsaPrivatekeyFromPem()
        {
            TestImportRsaPrivatekey(EncodingFormat.PEM,
                "CertificateProviderTests-PKey.pem"); // "ce-key.pem"); // 
        }

        [TestMethod]
        [Ignore] // Not sure if this is even a necessary scenario
        public void TestImportRsaPrivatekeyFromDer()
        {
            TestImportRsaPrivatekey(EncodingFormat.DER,
                "CertificateProviderTests-PKey.pem"); // "ce-key.der"); // 
        }

        public void TestImportRsaPrivatekey(EncodingFormat fmt, string filePath)
        {
            using (var cp = GetCP())
            {
                using (var source = new FileStream(filePath, FileMode.Open))
                {
                    var pk = cp.ImportPrivateKey<RsaPrivateKey>(fmt, source);
                }
            }
        }

        [TestMethod]
        public void TestGenerateRsaCsr()
        {
            using (var cp = GetCP())
            {
                var pkp = new RsaPrivateKeyParams();
                var pk = cp.GeneratePrivateKey(pkp);

                var crp = new CsrParams
                {
                    Details = new CsrDetails
                    {
                        CommonName = "TEST CERT",
                    }
                };

                var csr = cp.GenerateCsr(crp, pk, Crt.MessageDigest.SHA256);
            }
        }

        [TestMethod]
        public void TestExportRsaCsrAsDer()
        {
            TestExportRsaCsr(EncodingFormat.DER);
        }

        [TestMethod]
        public void TestExportRsaCsrAsPem()
        {
            TestExportRsaCsr(EncodingFormat.PEM);
        }

        private void TestExportRsaCsr(EncodingFormat fmt)
        {
            using (var cp = GetCP())
            {
                var pkp = new RsaPrivateKeyParams();
                var pk = cp.GeneratePrivateKey(pkp);

                var crp = new CsrParams
                {
                    Details = new CsrDetails
                    {
                        CommonName = "TEST CERT",
                    }
                };

                var csr = cp.GenerateCsr(crp, pk, Crt.MessageDigest.SHA256);

                using (var target = new MemoryStream())
                {
                    cp.ExportCsr(csr, fmt, target);
                }
            }
        }

        // TODO: DER Import not implemented in default CP
        //[TestMethod]
        public void TestImportRsaCsrAsDer()
        {
            TestImportRsaCsr(EncodingFormat.DER);
        }

        [TestMethod]
        public void TestImportRsaCsrAsPem()
        {
            TestImportRsaCsr(EncodingFormat.PEM);
        }

        private void TestImportRsaCsr(EncodingFormat fmt)
        {
            using (var cp = GetCP())
            {
                var pkp = new RsaPrivateKeyParams();
                var pk = cp.GeneratePrivateKey(pkp);

                var crp = new CsrParams
                {
                    Details = new CsrDetails
                    {
                        CommonName = "TEST CERT",
                    }
                };

                var csr = cp.GenerateCsr(crp, pk, Crt.MessageDigest.SHA256);
                byte[] bytes;
                using (var target = new MemoryStream())
                {
                    cp.ExportCsr(csr, fmt, target);
                    bytes = target.ToArray();
                }

                var imp = csr;
                using (var source = new MemoryStream(bytes))
                {
                    imp = cp.ImportCsr(fmt, source);
                }

                using (MemoryStream save1 = new MemoryStream(), save2 = new MemoryStream())
                {
                    cp.SaveCsr(csr, save1);
                    cp.SaveCsr(imp, save2);

                    var bytes1 = save1.ToArray();
                    var bytes2 = save2.ToArray();

                    CollectionAssert.AreEqual(bytes1, bytes2);
                }
            }
        }

        [TestMethod]
        public void TestImportCertificateFromDer()
        {
            TestImportCertificate(EncodingFormat.DER,
                    "CertificateProviderTests-Certificate.der");
        }

        [TestMethod]
        public void TestImportCertificateFromPem()
        {
            TestImportCertificate(EncodingFormat.PEM,
                    "CertificateProviderTests-Certificate.pem");
        }

        private void TestImportCertificate(EncodingFormat fmt, string filePath)
        {
            using (var cp = GetCP())
            {

                using (var source = new FileStream(filePath, FileMode.Open))
                {
                    var crt = cp.ImportCertificate(fmt, source);
                }
            }
        }

        [TestMethod]
        public void TestExportCertificateFromToDer()
        {
            TestExportCertificate(EncodingFormat.DER,
                    "CertificateProviderTests-Certificate.der");
        }

        [TestMethod]
        public void TestExportCertificateFromToPem()
        {
            TestExportCertificate(EncodingFormat.PEM,
                    "CertificateProviderTests-Certificate.pem");
        }

        private void TestExportCertificate(EncodingFormat fmt, string filePath)
        {
            using (var cp = GetCP())
            {

                using (var source = new FileStream(filePath, FileMode.Open))
                {
                    var crt = cp.ImportCertificate(fmt, source);

                    using (var target = new MemoryStream())
                    {
                        cp.ExportCertificate(crt, fmt, target);
                    }
                }
            }
        }

        [TestMethod]
        public void TestLoadAndSavePrivateKey()
        {
            using (var cp = GetCP())
            {

                var testPk = "CertificateProviderTests.key.json";

                PrivateKey pk;
                using (var s = new FileStream(testPk, FileMode.Open))
                {
                    pk = cp.LoadPrivateKey(s);
                }

                using (var s = new MemoryStream())
                {
                    cp.SavePrivateKey(pk, s);

                    var bytes = File.ReadAllBytes(testPk);
                    CollectionAssert.AreEqual(bytes, s.ToArray());
                }
            }
        }

        [TestMethod]
        public void TestExportArchive()
        {
            using (var cp = GetCP())
            {

                var testPk = "CertificateProviderTests.key.json";
                var testCert = "CertificateProviderTests-Certificate.pem";
                var testIcaCert = "CertificateProviderTests-ICA-Certificate.pem";

                PrivateKey pk;
                using (var s = new FileStream(testPk, FileMode.Open))
                {
                    pk = cp.LoadPrivateKey(s);
                }

                Crt cert;
                using (var s = new FileStream(testCert, FileMode.Open))
                {
                    cert = cp.ImportCertificate(EncodingFormat.PEM, s);
                }

                Crt icaCert;
                using (var s = new FileStream(testIcaCert, FileMode.Open))
                {
                    icaCert = cp.ImportCertificate(EncodingFormat.PEM, s);
                }

                using (var s = new MemoryStream())
                {
                    var certs = new[] { cert, icaCert };
                    cp.ExportArchive(pk, certs, ArchiveFormat.PKCS12, s);
                }
            }
        }
    }
}

