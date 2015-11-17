﻿using LetsEncrypt.ACME.PKI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    [TestClass]
    public class CertificateProviderTests
    {
        [TestMethod]
        public void TestGenerateRsaPrivateKey()
        {
            var cp = CertificateProvider.GetProvider();
            var pkp = new RsaPrivateKeyParams();
            var pk = cp.GeneratePrivateKey(pkp);

            Assert.IsInstanceOfType(pk, typeof(RsaPrivateKey));

            var rsaPk = (RsaPrivateKey)pk;

            // TODO:  verify the components of RSA PK?
            //rsaPk.BigNumber
            //rsaPk.Bits
            //rsaPk.E
        }

        [TestMethod]
        public void TestExportRsaPrivateKeyAsPem()
        {
            TestExportRsaPrivateKey(EncodingFormat.PEM);
        }

        private void TestExportRsaPrivateKey(EncodingFormat fmt)
        {
            var cp = CertificateProvider.GetProvider();
            var pkp = new RsaPrivateKeyParams();
            var pk = cp.GeneratePrivateKey(pkp);

            using (var target = new MemoryStream())
            {
                cp.ExportPrivateKey(pk, fmt, target);
            }
        }

        [TestMethod]
        public void TestGenerateRsaCsr()
        {
            var cp = CertificateProvider.GetProvider();
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
            var cp = CertificateProvider.GetProvider();
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
            var cp = CertificateProvider.GetProvider();
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
            var cp = CertificateProvider.GetProvider();

            using (var source = new FileStream(filePath, FileMode.Open))
            {
                var crt = cp.ImportCertificate(fmt, source);
            }
        }

        [TestMethod]
        public void TestExportCertificateFromDer()
        {
            TestExportCertificate(EncodingFormat.DER,
                    "CertificateProviderTests-Certificate.der");
        }

        [TestMethod]
        public void TestExportCertificateFromPem()
        {
            TestExportCertificate(EncodingFormat.PEM,
                    "CertificateProviderTests-Certificate.pem");
        }

        private void TestExportCertificate(EncodingFormat fmt, string filePath)
        {
            var cp = CertificateProvider.GetProvider();

            using (var source = new FileStream(filePath, FileMode.Open))
            {
                var crt = cp.ImportCertificate(fmt, source);

                using (var target = new MemoryStream())
                {
                    cp.ExportCertificate(crt, fmt, target);
                }
            }
        }

        [TestMethod]
        public void TestLoadAndSavePrivateKey()
        {
            var cp = CertificateProvider.GetProvider();

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

        [TestMethod]
        public void TestExportArchive()
        {
            var cp = CertificateProvider.GetProvider();

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
