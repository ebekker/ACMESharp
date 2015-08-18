using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.JOSE
{
    public class RS256Signer : ISigner
    {
        RSACryptoServiceProvider _rsa;
        SHA256CryptoServiceProvider _sha;
        object _jwk;

        public string JwsAlg { get { return "RS256"; } }

        public int KeySize
        { get; set; } = 2048;

        public void Init()
        {
            _rsa = new RSACryptoServiceProvider(KeySize);
            _sha = new SHA256CryptoServiceProvider();
        }

        public void Dispose()
        {
            if (_rsa != null)
                _rsa.Dispose();
            _rsa = null;
        }

        public void Save(Stream stream)
        {
            using (var w = new StreamWriter(stream))
            {
                w.Write(_rsa.ToXmlString(true));
            }
        }

        public void Load(Stream stream)
        {
            using (var r = new StreamReader(stream))
            {
                _rsa.FromXmlString(r.ReadToEnd());
            }
        }

        public object ExportJwk()
        {
            if (_jwk == null)
            {
                var keyParams = _rsa.ExportParameters(false);
                _jwk = new
                {
                    kty = "RSA",
                    n = JwsHelper.Base64UrlEncode(keyParams.Modulus),
                    e = JwsHelper.Base64UrlEncode(keyParams.Exponent),
                };
            }

            return _jwk;
        }

        public byte[] Sign(byte[] raw)
        {
            return _rsa.SignData(raw, _sha);
        }
    }
}
