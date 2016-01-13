using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Text.RegularExpressions;

namespace ACMESharp
{

    /// <summary>
    /// Set of unit tests that test RFC 7515 JSON Web Signature (JWS)
    /// standard and interoperability with the Boulder CA.  We can use
    /// these tests to experiment with Boulder and channel these
    /// lessons into the actual ACME client and supporting components.
    /// </summary>
    [TestClass]
    public class JwsUnitTests
    {
        [TestMethod]
        public void TestRfc7515_3_3()
        {
            var sampleHeader =
                    "{\"typ\":\"JWT\",\r\n \"alg\":\"HS256\"}";

            var samplePayload =
                    "{\"iss\":\"joe\",\r\n" +
                    " \"exp\":1300819380,\r\n" +
                    " \"http://example.com/is_root\":true}";

            var utf8 = Encoding.UTF8.GetBytes(sampleHeader);
            var b64u = Convert.ToBase64String(utf8).Replace("=","");
            Assert.AreEqual("eyJ0eXAiOiJKV1QiLA0KICJhbGciOiJIUzI1NiJ9", b64u);

            utf8 = Encoding.UTF8.GetBytes(samplePayload);
            b64u = Convert.ToBase64String(utf8).Replace("=","");
            Assert.AreEqual("eyJpc3MiOiJqb2UiLA0KICJleHAiOjEzMDA4MTkzODAsDQogImh0dHA6Ly9leGFtcGxlLmNvbS9pc19yb290Ijp0cnVlfQ", b64u);


        }

        [TestMethod]
        public void TestRfc7515Example_A_1_1()
        {
            string protectedSample = // From the RFC example
                    "{\"typ\":\"JWT\",\r\n" +
                    " \"alg\":\"HS256\"}";
          
            var protectedBytesExpected = new byte[] // From the RFC example
            {
                123, 34, 116, 121, 112, 34, 58, 34, 74, 87, 84, 34, 44, 13, 10,
                32, 34, 97, 108, 103, 34, 58, 34, 72, 83, 50, 53, 54, 34, 125
            };
            var protectedBytesActual = Encoding.UTF8.GetBytes(protectedSample);
            CollectionAssert.AreEqual(protectedBytesExpected, protectedBytesActual);
           
            string protectedB64uExpected = "eyJ0eXAiOiJKV1QiLA0KICJhbGciOiJIUzI1NiJ9"; // From the RFC example
            string protectedB64uActual = JOSE.JwsHelper.Base64UrlEncode(protectedBytesActual);
            Assert.AreEqual(protectedB64uExpected, protectedB64uActual);

            string payloadSample = // From the RFC example
                    "{\"iss\":\"joe\",\r\n" +
                    " \"exp\":1300819380,\r\n" +
                    " \"http://example.com/is_root\":true}";

            byte[] payloadBytesExpected = // From the RFC example
            {
                123, 34, 105, 115, 115, 34, 58, 34, 106, 111, 101, 34, 44, 13, 10,
                32, 34, 101, 120, 112, 34, 58, 49, 51, 48, 48, 56, 49, 57, 51, 56,
                48, 44, 13, 10, 32, 34, 104, 116, 116, 112, 58, 47, 47, 101, 120, 97,
                109, 112, 108, 101, 46, 99, 111, 109, 47, 105, 115, 95, 114, 111,
                111, 116, 34, 58, 116, 114, 117, 101, 125
            };
            byte[] payloadBytesActual = Encoding.UTF8.GetBytes(payloadSample);
            CollectionAssert.AreEqual(payloadBytesExpected, payloadBytesActual);

            string payloadB64uExpected = // From the RFC example
                    "eyJpc3MiOiJqb2UiLA0KICJleHAiOjEzMDA4MTkzODAsDQogImh0dHA6Ly9leGFt" +
                    "cGxlLmNvbS9pc19yb290Ijp0cnVlfQ";
            string payloadB64uActual = JOSE.JwsHelper.Base64UrlEncode(payloadBytesActual);
            Assert.AreEqual(payloadB64uExpected, payloadB64uActual);

            string signingInput = $"{protectedB64uActual}.{payloadB64uActual}";

            byte[] signingBytesExpected = // From the RFC example
            {
                101, 121, 74, 48, 101, 88, 65, 105, 79, 105, 74, 75, 86, 49, 81,
                105, 76, 65, 48, 75, 73, 67, 74, 104, 98, 71, 99, 105, 79, 105, 74,
                73, 85, 122, 73, 49, 78, 105, 74, 57, 46, 101, 121, 74, 112, 99, 51,
                77, 105, 79, 105, 74, 113, 98, 50, 85, 105, 76, 65, 48, 75, 73, 67,
                74, 108, 101, 72, 65, 105, 79, 106, 69, 122, 77, 68, 65, 52, 77, 84,
                107, 122, 79, 68, 65, 115, 68, 81, 111, 103, 73, 109, 104, 48, 100,
                72, 65, 54, 76, 121, 57, 108, 101, 71, 70, 116, 99, 71, 120, 108, 76,
                109, 78, 118, 98, 83, 57, 112, 99, 49, 57, 121, 98, 50, 57, 48, 73,
                106, 112, 48, 99, 110, 86, 108, 102, 81
            };
            byte[] signingBytesActual = Encoding.ASCII.GetBytes(signingInput);
            CollectionAssert.AreEqual(signingBytesExpected, signingBytesActual);

            // JSON Web Key (JWK) symmetric key from the RFC example:
            //   {"kty":"oct",
            //    "k":"AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75
            //         aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"
            //   }
            byte[] symKey = JOSE.JwsHelper.Base64UrlDecode(
                    "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75" +
                    "aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow");
            byte[] hmacExpected = // From the RFC example:
            {
                116, 24, 223, 180, 151, 153, 224, 37, 79, 250, 96, 125, 216, 173,
                187, 186, 22, 212, 37, 77, 105, 214, 191, 240, 91, 88, 5, 88, 83,
                132, 141, 121
            };
            byte[] hmacActual;
            using (var hmacAlgor = new System.Security.Cryptography.HMACSHA256(symKey))
            {
                hmacActual = hmacAlgor.ComputeHash(signingBytesActual);
            }
            CollectionAssert.AreEqual(hmacExpected, hmacActual);

            string hmacB64uExpected = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"; // From RFC example
            string hmacB64uActual = JOSE.JwsHelper.Base64UrlEncode(hmacActual);
            Assert.AreEqual(hmacB64uExpected, hmacB64uActual);

            string jwsSigExpected = // From RFC example
                    "eyJ0eXAiOiJKV1QiLA0KICJhbGciOiJIUzI1NiJ9" +
                    ".eyJpc3MiOiJqb2UiLA0KICJleHAiOjEzMDA4MTkzODAsDQogImh0dHA6Ly9leGFt" +
                    "cGxlLmNvbS9pc19yb290Ijp0cnVlfQ" +
                    ".dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
            string jwsSigActual = $"{protectedB64uActual}.{payloadB64uActual}.{hmacB64uActual}";
            Assert.AreEqual(jwsSigExpected, jwsSigActual);
        }

        /// <summary>
        /// Returns a set of <see cref="System.Security.Cryptography.RSAParameters">RSA
        /// Parameters</see> precomputed with values from the RFC 7515 Appendix A.2.1
        /// sample.
        /// </summary>
        public static System.Security.Cryptography.RSAParameters GetRsaParamsForRfc7515Example_A_2_1()
        {
            // JSON Web Key (JWK) symmetric key from the RFC example Appendix A.2
            // (with line breaks within values for display purposes only):
            //   {"kty":"RSA",
            //    "n":"ofgWCuLjybRlzo0tZWJjNiuSfb4p4fAkd_wWJcyQoTbji9k0l8W26mPddx
            //         HmfHQp-Vaw-4qPCJrcS2mJPMEzP1Pt0Bm4d4QlL-yRT-SFd2lZS-pCgNMs
            //         D1W_YpRPEwOWvG6b32690r2jZ47soMZo9wGzjb_7OMg0LOL-bSf63kpaSH
            //         SXndS5z5rexMdbBYUsLA9e-KXBdQOS-UTo7WTBEMa2R2CapHg665xsmtdV
            //         MTBQY4uDZlxvb3qCo5ZwKh9kG4LT6_I5IhlJH7aGhyxXFvUK-DWNmoudF8
            //         NAco9_h9iaGNj8q2ethFkMLs91kzk2PAcDTW9gb54h4FRWyuXpoQ",
            //    "e":"AQAB",
            //    "d":"Eq5xpGnNCivDflJsRQBXHx1hdR1k6Ulwe2JZD50LpXyWPEAeP88vLNO97I
            //         jlA7_GQ5sLKMgvfTeXZx9SE-7YwVol2NXOoAJe46sui395IW_GO-pWJ1O0
            //         BkTGoVEn2bKVRUCgu-GjBVaYLU6f3l9kJfFNS3E0QbVdxzubSu3Mkqzjkn
            //         439X0M_V51gfpRLI9JYanrC4D4qAdGcopV_0ZHHzQlBjudU2QvXt4ehNYT
            //         CBr6XCLQUShb1juUO1ZdiYoFaFQT5Tw8bGUl_x_jTj3ccPDVZFD9pIuhLh
            //         BOneufuBiB4cS98l2SR_RQyGWSeWjnczT0QU91p1DhOVRuOopznQ",
            //    "p":"4BzEEOtIpmVdVEZNCqS7baC4crd0pqnRH_5IB3jw3bcxGn6QLvnEtfdUdi
            //         YrqBdss1l58BQ3KhooKeQTa9AB0Hw_Py5PJdTJNPY8cQn7ouZ2KKDcmnPG
            //         BY5t7yLc1QlQ5xHdwW1VhvKn-nXqhJTBgIPgtldC-KDV5z-y2XDwGUc",
            //    "q":"uQPEfgmVtjL0Uyyx88GZFF1fOunH3-7cepKmtH4pxhtCoHqpWmT8YAmZxa
            //         ewHgHAjLYsp1ZSe7zFYHj7C6ul7TjeLQeZD_YwD66t62wDmpe_HlB-TnBA
            //         -njbglfIsRLtXlnDzQkv5dTltRJ11BKBBypeeF6689rjcJIDEz9RWdc",
            //    "dp":"BwKfV3Akq5_MFZDFZCnW-wzl-CCo83WoZvnLQwCTeDv8uzluRSnm71I3Q
            //         CLdhrqE2e9YkxvuxdBfpT_PI7Yz-FOKnu1R6HsJeDCjn12Sk3vmAktV2zb
            //         34MCdy7cpdTh_YVr7tss2u6vneTwrA86rZtu5Mbr1C1XsmvkxHQAdYo0",
            //    "dq":"h_96-mK1R_7glhsum81dZxjTnYynPbZpHziZjeeHcXYsXaaMwkOlODsWa
            //         7I9xXDoRwbKgB719rrmI2oKr6N3Do9U0ajaHF-NKJnwgjMd2w9cjz3_-ky
            //         NlxAr2v4IKhGNpmM5iIgOS1VZnOZ68m6_pbLBSp3nssTdlqvd0tIiTHU",
            //    "qi":"IYd7DHOhrWvxkwPQsRM2tOgrjbcrfvtQJipd-DlcxyVuuM9sQLdgjVk2o
            //         y26F0EmpScGLq2MowX7fhd_QJQ3ydy5cY7YIBi87w93IKLEdfnbJtoOPLU
            //         W0ITrJReOgo1cq9SbsxYawBgfp_gh6A5603k2-ZQwVK0JKSHuLFkuQ3U"
            //   }

            // With help from:
            //   https://msdn.microsoft.com/en-us/library/system.security.cryptography.rsaparameters(v=vs.110).aspx

            var wsRegex = new Regex("\\s+");
            var rsaKeyPartN = wsRegex.Replace(@"
                    ofgWCuLjybRlzo0tZWJjNiuSfb4p4fAkd_wWJcyQoTbji9k0l8W26mPddx
                    HmfHQp-Vaw-4qPCJrcS2mJPMEzP1Pt0Bm4d4QlL-yRT-SFd2lZS-pCgNMs
                    D1W_YpRPEwOWvG6b32690r2jZ47soMZo9wGzjb_7OMg0LOL-bSf63kpaSH
                    SXndS5z5rexMdbBYUsLA9e-KXBdQOS-UTo7WTBEMa2R2CapHg665xsmtdV
                    MTBQY4uDZlxvb3qCo5ZwKh9kG4LT6_I5IhlJH7aGhyxXFvUK-DWNmoudF8
                    NAco9_h9iaGNj8q2ethFkMLs91kzk2PAcDTW9gb54h4FRWyuXpoQ", "");
            var rsaKeyPartE = wsRegex.Replace(@"AQAB", "");
            var rsaKeyPartD = wsRegex.Replace(@"
                    Eq5xpGnNCivDflJsRQBXHx1hdR1k6Ulwe2JZD50LpXyWPEAeP88vLNO97I
                    jlA7_GQ5sLKMgvfTeXZx9SE-7YwVol2NXOoAJe46sui395IW_GO-pWJ1O0
                    BkTGoVEn2bKVRUCgu-GjBVaYLU6f3l9kJfFNS3E0QbVdxzubSu3Mkqzjkn
                    439X0M_V51gfpRLI9JYanrC4D4qAdGcopV_0ZHHzQlBjudU2QvXt4ehNYT
                    CBr6XCLQUShb1juUO1ZdiYoFaFQT5Tw8bGUl_x_jTj3ccPDVZFD9pIuhLh
                    BOneufuBiB4cS98l2SR_RQyGWSeWjnczT0QU91p1DhOVRuOopznQ", "");
            var rsaKeyPartP = wsRegex.Replace(@"
                    4BzEEOtIpmVdVEZNCqS7baC4crd0pqnRH_5IB3jw3bcxGn6QLvnEtfdUdi
                    YrqBdss1l58BQ3KhooKeQTa9AB0Hw_Py5PJdTJNPY8cQn7ouZ2KKDcmnPG
                    BY5t7yLc1QlQ5xHdwW1VhvKn-nXqhJTBgIPgtldC-KDV5z-y2XDwGUc", "");
            var rsaKeyPartQ = wsRegex.Replace(@"
                    uQPEfgmVtjL0Uyyx88GZFF1fOunH3-7cepKmtH4pxhtCoHqpWmT8YAmZxa
                    ewHgHAjLYsp1ZSe7zFYHj7C6ul7TjeLQeZD_YwD66t62wDmpe_HlB-TnBA
                    -njbglfIsRLtXlnDzQkv5dTltRJ11BKBBypeeF6689rjcJIDEz9RWdc", "");
            var rsaKeyPartDP = wsRegex.Replace(@"
                    BwKfV3Akq5_MFZDFZCnW-wzl-CCo83WoZvnLQwCTeDv8uzluRSnm71I3Q
                    CLdhrqE2e9YkxvuxdBfpT_PI7Yz-FOKnu1R6HsJeDCjn12Sk3vmAktV2zb
                    34MCdy7cpdTh_YVr7tss2u6vneTwrA86rZtu5Mbr1C1XsmvkxHQAdYo0", "");
            var rsaKeyPartDQ = wsRegex.Replace(@"
                    h_96-mK1R_7glhsum81dZxjTnYynPbZpHziZjeeHcXYsXaaMwkOlODsWa
                    7I9xXDoRwbKgB719rrmI2oKr6N3Do9U0ajaHF-NKJnwgjMd2w9cjz3_-ky
                    NlxAr2v4IKhGNpmM5iIgOS1VZnOZ68m6_pbLBSp3nssTdlqvd0tIiTHU", "");
            var rsaKeyPartQI = wsRegex.Replace(@"
                    IYd7DHOhrWvxkwPQsRM2tOgrjbcrfvtQJipd-DlcxyVuuM9sQLdgjVk2o
                    y26F0EmpScGLq2MowX7fhd_QJQ3ydy5cY7YIBi87w93IKLEdfnbJtoOPLU
                    W0ITrJReOgo1cq9SbsxYawBgfp_gh6A5603k2-ZQwVK0JKSHuLFkuQ3U", "");

            var rsaKeyParams = new System.Security.Cryptography.RSAParameters
            {
                Exponent = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartE),
                Modulus = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartN),
                D = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartD),
                P = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartP),
                Q = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartQ),
                DP = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartDP),
                DQ = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartDQ),
                InverseQ = JOSE.JwsHelper.Base64UrlDecode(rsaKeyPartQI)
            };

            return rsaKeyParams;
        }

        [TestMethod]
        public void TestRfc7515Example_A_2_1()
        {
            string protectedSample = // From the RFC example
                    "{\"alg\":\"RS256\"}";
            byte[] protectedBytesExpected = // From the RFC example
            {
                123, 34, 97, 108, 103, 34, 58, 34, 82, 83, 50, 53, 54, 34, 125
            };
            byte[] protectedBytesActual = Encoding.UTF8.GetBytes(protectedSample);
            CollectionAssert.AreEqual(protectedBytesExpected, protectedBytesActual);

            string protectedB64uExpected = "eyJhbGciOiJSUzI1NiJ9"; // From the RFC example
            string protectedB64uActual = JOSE.JwsHelper.Base64UrlEncode(protectedBytesActual);
            Assert.AreEqual(protectedB64uExpected, protectedB64uActual);

            string payloadSample = // From the RFC example
                    "{\"iss\":\"joe\",\r\n" +
                    " \"exp\":1300819380,\r\n" +
                    " \"http://example.com/is_root\":true}";
            byte[] payloadBytesActual = Encoding.UTF8.GetBytes(payloadSample);
            string payloadB64uActual = JOSE.JwsHelper.Base64UrlEncode(payloadBytesActual);
            string signingInput = $"{protectedB64uActual}.{payloadB64uActual}";

            byte[] signingBytesExpected = // From the RFC example
            {
                101, 121, 74, 104, 98, 71, 99, 105, 79, 105, 74, 83, 85, 122, 73,
                49, 78, 105, 74, 57, 46, 101, 121, 74, 112, 99, 51, 77, 105, 79, 105,
                74, 113, 98, 50, 85, 105, 76, 65, 48, 75, 73, 67, 74, 108, 101, 72,
                65, 105, 79, 106, 69, 122, 77, 68, 65, 52, 77, 84, 107, 122, 79, 68,
                65, 115, 68, 81, 111, 103, 73, 109, 104, 48, 100, 72, 65, 54, 76,
                121, 57, 108, 101, 71, 70, 116, 99, 71, 120, 108, 76, 109, 78, 118,
                98, 83, 57, 112, 99, 49, 57, 121, 98, 50, 57, 48, 73, 106, 112, 48,
                99, 110, 86, 108, 102, 81
            };
            byte[] signingBytesActual = Encoding.ASCII.GetBytes(signingInput);
            CollectionAssert.AreEqual(signingBytesExpected, signingBytesActual);


            byte[] sigExpected = // From the RFC example
            {
                112, 46, 33, 137, 67, 232, 143, 209, 30, 181, 216, 45, 191, 120, 69,
                243, 65, 6, 174, 27, 129, 255, 247, 115, 17, 22, 173, 209, 113, 125,
                131, 101, 109, 66, 10, 253, 60, 150, 238, 221, 115, 162, 102, 62, 81,
                102, 104, 123, 0, 11, 135, 34, 110, 1, 135, 237, 16, 115, 249, 69,
                229, 130, 173, 252, 239, 22, 216, 90, 121, 142, 232, 198, 109, 219,
                61, 184, 151, 91, 23, 208, 148, 2, 190, 237, 213, 217, 217, 112, 7,
                16, 141, 178, 129, 96, 213, 248, 4, 12, 167, 68, 87, 98, 184, 31,
                190, 127, 249, 217, 46, 10, 231, 111, 36, 242, 91, 51, 187, 230, 244,
                74, 230, 30, 177, 4, 10, 203, 32, 4, 77, 62, 249, 18, 142, 212, 1,
                48, 121, 91, 212, 189, 59, 65, 238, 202, 208, 102, 171, 101, 25, 129,
                253, 228, 141, 247, 127, 55, 45, 195, 139, 159, 175, 221, 59, 239,
                177, 139, 93, 163, 204, 60, 46, 176, 47, 158, 58, 65, 214, 18, 202,
                173, 21, 145, 18, 115, 160, 95, 35, 185, 232, 56, 250, 175, 132, 157,
                105, 132, 41, 239, 90, 30, 136, 121, 130, 54, 195, 212, 14, 96, 69,
                34, 165, 68, 200, 242, 122, 122, 45, 184, 6, 99, 209, 108, 247, 202,
                234, 86, 222, 64, 92, 178, 33, 90, 69, 178, 194, 85, 102, 181, 90,
                193, 167, 72, 160, 112, 223, 200, 163, 42, 70, 149, 67, 208, 25, 238,
                251, 71
            };
            byte[] sigActual = null;
            using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
            {
                rsa.ImportParameters(GetRsaParamsForRfc7515Example_A_2_1());
                using (var sha256 = new System.Security.Cryptography.SHA256CryptoServiceProvider())
                {
                    sigActual = rsa.SignData(signingBytesExpected, sha256);
                }
            }
            CollectionAssert.AreEqual(sigExpected, sigActual);

            string sigB64uExpected = // From the RFC example
                    "cC4hiUPoj9Eetdgtv3hF80EGrhuB__dzERat0XF9g2VtQgr9PJbu3XOiZj5RZmh7" +
                    "AAuHIm4Bh-0Qc_lF5YKt_O8W2Fp5jujGbds9uJdbF9CUAr7t1dnZcAcQjbKBYNX4" +
                    "BAynRFdiuB--f_nZLgrnbyTyWzO75vRK5h6xBArLIARNPvkSjtQBMHlb1L07Qe7K" +
                    "0GarZRmB_eSN9383LcOLn6_dO--xi12jzDwusC-eOkHWEsqtFZESc6BfI7noOPqv" +
                    "hJ1phCnvWh6IeYI2w9QOYEUipUTI8np6LbgGY9Fs98rqVt5AXLIhWkWywlVmtVrB" +
                    "p0igcN_IoypGlUPQGe77Rw";
            string sigB64uActual = JOSE.JwsHelper.Base64UrlEncode(sigActual);
            Assert.AreEqual(sigB64uExpected, sigB64uActual);
        }
    }
}
