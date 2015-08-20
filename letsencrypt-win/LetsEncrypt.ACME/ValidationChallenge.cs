using LetsEncrypt.ACME.JOSE;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    public class ValidationChallenge
    {
        public const string DNS_CHALLENGE_NAMEPREFIX = "_acme-challenge";
        public const string DNS_CHALLENGE_RECORDTYPE = "TXT";

        public string Type
        { get; set; }

        public string Uri
        { get; set; }

        public string Token
        { get; set; }

        public string Status
        { get; set; }

        public bool? Tls
        { get; set; }

        /// <summary>
        /// Returns a key-value pair that represents the DNS domain name that needs
        /// to be configured (the key) and the value that should be returned (the value)
        /// for a query against that domain name for a record of type TXT.
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="signer"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GenerateDnsChallengeResponse(string dns, ISigner signer)
        {
            var resp = new
            {
                type = "dns",
                token = Token
            };
            var json = JsonConvert.SerializeObject(resp);
            var hdrs = new { alg = signer.JwsAlg, jwk = signer.ExportJwk() };
            var signed = JwsHelper.SignFlatJsonAsObject(
                    signer.Sign, json, unprotectedHeaders: hdrs);

            return new KeyValuePair<string, string>(
                    $"{DNS_CHALLENGE_NAMEPREFIX}.{dns}",
                    signed.signature);
        }
    }
}
