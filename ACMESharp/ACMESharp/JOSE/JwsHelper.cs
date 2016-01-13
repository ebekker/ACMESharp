using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ACMESharp.JOSE
{
    /// <summary>
    /// A helper class to support JSON Web Signature (JWS) operations as needed for ACME.
    /// </summary>
    /// <remarks>
    /// ACME only requires a subset of JWS functionality, such as only requiring support
    /// for the <see cref="https://tools.ietf.org/html/rfc7515#section-7.2.2">Flattened
    /// JWS JSON Serialization</see> format, and so this helper class' scope and
    /// implelmentation of JWS is limited to those features required for ACME.
    /// </remarks>
    public static class JwsHelper
    {
        /*
         *  In the JWS JSON Serialization, a JWS is represented as a JSON object
         *  containing some or all of these four members:
         *
         *  o  "protected", with the value BASE64URL(UTF8(JWS Protected Header))
         *  o  "header", with the value JWS Unprotected Header
         *  o  "payload", with the value BASE64URL(JWS Payload)
         *  o  "signature", with the value BASE64URL(JWS Signature)
         */
        /*
         *   {
         *     "header": {
         *       "alg": "RS256",
         *       "jwk": {
         *         "kty": "RSA",
         *         "n": "xGZV9QR__pWrlw7cPbFaI-84Yn8-qAC3CHUaXmDIqK0kUoXEeZG5P8NmWGf8dCQCAywyc-k5FjP34lEhYwpqn81r_1u1WNVwsAaBfcVEGRy3HwWozWhkXlFXN-HUku_7vrtgR4DM4JzCnHipART-s3Xy6jzmcJSdy-278EsCql7wpNYT9CabxdbtNc7pDIDxt2t69QtVyrjm2NFz6y9AGABR1DksM7YGz-zc-3SdHnotXnKt1m2TXeGIECn7r4LuRbjlnVBTFO77jqbNN5u7kVRQGaqtn4i7AzAHgUtIaZW1iwmlfTE-ek4N6GsK2nO89nHRzmS0YQuqfuNFqGbM0Q",
         *         "e": "AQAB"
         *       }
         *     },
         *     "protected": "eyJub25jZSI6ImJidTdIWmxucGZ0VW95eFZydkdZYkl3ZjVRajdTZkxYVE1pNy1pUGFiUDgifQ",
         *     "payload": "ewogICJyZXNvdXJjZSI6ICJuZXctcmVnIiwKICAiY29udGFjdCI6IFsKICAgICJtYWlsdG86bGV0c2VuY3J5cHRAbWFpbGluYXRvci5jb20iCiAgXQp9",
         *     "signature": "dCK1T9T5Tg1-ZLpJKimHBvvjDNPloJPELvAVyLeRpjxx3sN8GNhqybRONDUz7umXDUaCKSkOX2osZ9GkVJNlda4FLLwn2a_TXHRWXyDyM-LI6ZTOHKW-dSVUR-HUo7MOAA-rdjbEmEOMq00jeLvmepEkElYdRTFEvo42XZHShjY1ybS96iwJbKDetJQCHHYOXrOtKhPC9zKv8FeMgl0ppwzV2YYISEeMZpM70ER0SiI7ECQ3ISn1dpPJBzU-3AEx2lLurkU3PaXbTQ6XoHqr9EmhmjnzsaWAGeL5m_e0JdAbBNcNkNeowGAhSztC5tKDnqn4SFvfgH-e9rDdmDslng"
         *   }
         */

        /* References:
         *   https://tools.ietf.org/html/rfc7515
         *   http://kjur.github.io/jsrsasign/
         *   http://dotnetcodr.com/2014/01/20/introduction-to-oauth2-json-web-tokens/
         */

        /// <summary>
        /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static string Base64UrlEncode(string raw, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return Base64UrlEncode(encoding.GetBytes(raw));
        }

        /// <summary>
        /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static string Base64UrlEncode(byte[] raw)
        {
            string enc = Convert.ToBase64String(raw);  // Regular base64 encoder
            enc = enc.Split('=')[0];                   // Remove any trailing '='s
            enc = enc.Replace('+', '-');               // 62nd char of encoding
            enc = enc.Replace('/', '_');               // 63rd char of encoding
            return enc;
        }

        /// <summary>
        /// URL-safe Base64 decoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static byte[] Base64UrlDecode(string enc)
        {
            string raw = enc;
            raw = raw.Replace('-', '+');  // 62nd char of encoding
            raw = raw.Replace('_', '/');  // 63rd char of encoding
            switch (raw.Length % 4)       // Pad with trailing '='s
            {
                case 0: break;               // No pad chars in this case
                case 2: raw += "=="; break;  // Two pad chars
                case 3: raw += "="; break;   // One pad char
                default:
                    throw new System.Exception("Illegal base64url string!");
            }
            return Convert.FromBase64String(raw); // Standard base64 decoder
        }

        public static string Base64UrlDecodeToString(string enc, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return encoding.GetString(Base64UrlDecode(enc));
        }

        /// <summary>
        /// Computes a JSON Web Signature (JWS) according to the rules of RFC 7515 Section 5.
        /// </summary>
        /// <param name="sigFunc"></param>
        /// <param name="payload"></param>
        /// <param name="protectedHeaders"></param>
        /// <param name="unprotectedHeaders"></param>
        /// <returns></returns>
        public static JwsSigned SignFlatJsonAsObject(Func<byte[], byte[]> sigFunc, string payload,
                object protectedHeaders = null, object unprotectedHeaders = null)
        {
            if (protectedHeaders == null && unprotectedHeaders == null)
                throw new ArgumentException("at least one of protected or unprotected headers must be specified");

            string protectedHeadersSer = "";
            if (protectedHeaders != null)
            {
                protectedHeadersSer = JsonConvert.SerializeObject(
                        protectedHeaders, Formatting.None);
            }

            string payloadB64u = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            string protectedB64u = Base64UrlEncode(Encoding.UTF8.GetBytes(protectedHeadersSer));

            string signingInput = $"{protectedB64u}.{payloadB64u}";
            byte[] signingBytes = Encoding.ASCII.GetBytes(signingInput);

            byte[] sigBytes = sigFunc(signingBytes);
            string sigB64u = Base64UrlEncode(sigBytes);

            var jwsFlatJS = new JwsSigned
            {
                header = unprotectedHeaders,
                @protected = protectedB64u,
                payload = payloadB64u,
                signature = sigB64u
            };

            return jwsFlatJS;
        }
        public static string SignFlatJson(Func<byte[], byte[]> sigFunc, string payload,
                object protectedHeaders = null, object unprotectedHeaders = null)
        {
            var jwsFlatJS = SignFlatJsonAsObject(sigFunc, payload, protectedHeaders, unprotectedHeaders);
            return JsonConvert.SerializeObject(jwsFlatJS, Formatting.Indented);
        }

        /// <summary>
        /// Computes a thumbprint of the JWK using the argument Hash Algorithm
        /// as per <see cref="https://tools.ietf.org/html/rfc7638">RFC 7638</see>,
        /// JSON Web Key (JWK) Thumbprint.
        /// </summary>
        /// <param name="algor"></param>
        /// <returns></returns>
        public static byte[] ComputeThumbprint(ISigner signer, HashAlgorithm algor)
        {
            // As per RFC 7638 Section 3, we export the JWK in a canonical form
            // and then produce a JSON object with no whitespace or line breaks

            var jwkCanon = signer.ExportJwk(true);
            var jwkJson = JsonConvert.SerializeObject(jwkCanon, Formatting.None);
            var jwkBytes = Encoding.UTF8.GetBytes(jwkJson);
            var jwkHash = algor.ComputeHash(jwkBytes);

            return jwkHash;
        }

        /// <summary>
        /// Computes the ACME Key Authorization of the JSON Web Key (JWK) of an argument
        /// Signer as prescribed in the
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.1"
        /// >ACME specification, section 7.1</see>.
        /// </summary>
        public static string ComputeKeyAuthorization(ISigner signer, string token)
        {
            using (var sha = SHA256.Create())
            {
                var jwkThumb = Base64UrlEncode(ComputeThumbprint(signer, sha));
                return $"{token}.{jwkThumb}";
            }
        }

        /// <summary>
        /// Computes a SHA256 digest over the <see cref="ComputeKeyAuthorization"
        /// >ACME Key Authorization</see> as required by some of the ACME Challenge
        /// responses.
        /// </summary>
        public static string ComputeKeyAuthorizationDigest(ISigner signer, string token)
        {
            using (var sha = SHA256.Create())
            {
                var jwkThumb = Base64UrlEncode(ComputeThumbprint(signer, sha));
                var keyAuthz = $"{token}.{jwkThumb}";
                var keyAuthzDig = sha.ComputeHash(Encoding.UTF8.GetBytes(keyAuthz));
                return Base64UrlEncode(keyAuthzDig);
            }
        }

        public class JwsSigned
        {
            public object header
            { get; set; }

            public string @protected
            { get; set; }

            public string payload
            { get; set; }

            public string signature
            { get; set; }
        }
    }
}
