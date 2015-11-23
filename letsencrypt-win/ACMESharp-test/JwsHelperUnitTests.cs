using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using ACMESharp.JOSE;

namespace ACMESharp
{
    [TestClass]
    public class JwsHelperUnitTests
    {
        [TestMethod]
        [TestCategory("skipCI")]
        public void TestSignFlagJson()
        {
            Func<byte[], byte[]> sigFunc = (x) =>
            {
                using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(JwsUnitTests.GetRsaParamsForRfc7515Example_A_2_1());
                    using (var sha256 = new System.Security.Cryptography.SHA256CryptoServiceProvider())
                    {
                        return rsa.SignData(x, sha256);
                    }
                }
            };

            object protectedSample = new // From the RFC example
            {
                alg = "RS256"
            };
            object headerSample = new // From the RFC example
            {
                kid = "2010-12-29"
            };
            string payloadSample = // From the RFC example
                    "{\"iss\":\"joe\",\r\n" +
                    " \"exp\":1300819380,\r\n" +
                    " \"http://example.com/is_root\":true}";

            var wsRegex = new Regex("\\s+");
            var sigExpected = // Derived from the RFC example in A.6.4
                    wsRegex.Replace(@"{
                        ""payload"":""eyJpc3MiOiJqb2UiLA0KICJleHAiOjEzMDA4MTkzODAsDQogImh0dHA6Ly9leGFtcGxlLmNvbS9pc19yb290Ijp0cnVlfQ"",
                        ""protected"":""eyJhbGciOiJSUzI1NiJ9"",
                        ""header"":{""kid"":""2010-12-29""},
                        ""signature"":
                            ""cC4hiUPoj9Eetdgtv3hF80EGrhuB__dzERat0XF9g2VtQgr9PJbu3XOiZj5RZ
                            mh7AAuHIm4Bh-0Qc_lF5YKt_O8W2Fp5jujGbds9uJdbF9CUAr7t1dnZcAcQjb
                            KBYNX4BAynRFdiuB--f_nZLgrnbyTyWzO75vRK5h6xBArLIARNPvkSjtQBMHl
                            b1L07Qe7K0GarZRmB_eSN9383LcOLn6_dO--xi12jzDwusC-eOkHWEsqtFZES
                            c6BfI7noOPqvhJ1phCnvWh6IeYI2w9QOYEUipUTI8np6LbgGY9Fs98rqVt5AX
                            LIhWkWywlVmtVrBp0igcN_IoypGlUPQGe77Rw""
                    }", "");
            var sigActual = wsRegex.Replace(JwsHelper.SignFlatJson(
                    sigFunc, payloadSample, protectedSample, headerSample), "");
            Assert.AreEqual(sigExpected, sigActual);
        }
    }
}
