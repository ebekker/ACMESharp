namespace ACMESharp.JOSE
{
    public static class JwsHeaders
    {
        /// <summary>
        /// RFC7515 4.1.1.  "alg" (Algorithm) Header Parameter
        /// </summary>
        public const string ALG = "alg";
        /// <summary>
        /// RFC7515 4.1.2.  "jku" (JWK Set URL) Header Parameter
        /// </summary>
        public const string JKU = "jku";
        /// <summary>
        /// RFC7515 4.1.3.  "jwk" (JSON Web Key) Header Parameter
        /// </summary>
        public const string JWK = "jwk";
        /// <summary>
        /// RFC7515 4.1.4.  "kid" (Key ID) Header Parameter
        /// </summary>
        public const string KID = "kid";
        /// <summary>
        /// RFC7515 4.1.5.  "x5u" (X.509 URL) Header Parameter
        /// </summary>
        public const string X5U = "x5u";
        /// <summary>
        /// RFC7515 4.1.6.  "x5c" (X.509 Certificate Chain) Header Parameter
        /// </summary>
        public const string X5C = "x5c";
        /// <summary>
        /// RFC7515 4.1.7.  "x5t" (X.509 Certificate SHA-1 Thumbprint) Header Parameter
        /// </summary>
        public const string X5T = "x5t";
        /// <summary>
        /// RFC7515 4.1.8.  "x5t#S256" (X.509 Certificate SHA-256 Thumbprint) Header Parameter
        /// </summary>
        public const string X5TS56 = "x5t#256";
        /// <summary>
        /// RFC7515 4.1.9.  "typ" (Type) Header Parameter
        /// </summary>
        public const string TYP = "typ";
        /// <summary>
        /// RFC7515 4.1.10.  "cty" (Content Type) Header Parameter
        /// </summary>
        public const string CTY = "cty";
        /// <summary>
        /// RFC7515 4.1.11.  "crit" (Critical) Header Parameter
        /// </summary>
        public const string CRIT = "crit";
    }
}
