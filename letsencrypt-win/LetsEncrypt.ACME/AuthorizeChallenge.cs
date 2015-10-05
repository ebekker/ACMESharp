using LetsEncrypt.ACME.JOSE;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    public class AuthorizeChallenge
    {
        public const string DNS_CHALLENGE_NAMEPREFIX = "_acme-challenge.";
        public const string DNS_CHALLENGE_RECORDTYPE = "TXT";

        public const string HTTP_CHALLENGE_PATHPREFIX = ".well-known/acme-challenge/";

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

        public KeyValuePair<string, string> ChallengeAnswer
        { get; set; }

        public object ChallengeAnswerMessage
        { get; set; }

        public object ValidationRecord
        { get; set; }

        /// <summary>
        /// Returns a key-value pair that represents the DNS domain name that needs
        /// to be configured (the key) and the value that should be returned (the value)
        /// for a query against that domain name for a record of type TXT.
        /// </summary>
        /// <param name="dnsId"></param>
        /// <param name="signer"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GenerateDnsChallengeAnswer(string dnsId, ISigner signer)
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

            /*
            // We format it as a set of lines broken on 100-character boundaries to make it
            // easier to copy and put into a DNS TXT RR which normally have a 255-char limit
            // so this result may need to be broken up into multiple smaller TXT RR entries
            var sigFormatted = Regex.Replace(signed.signature,
                    "(.{100,100})", "$1\r\n");
            */

            return new KeyValuePair<string, string>(
                    $"{DNS_CHALLENGE_NAMEPREFIX}{dnsId}",
                    signed.signature); /*sigFormatted);*/
        }

        /// <summary>
        /// Returns a key-value pair that represents the Simple HTTP resource path that
        /// needs to be configured (the key) and the resource content that should be returned
        /// for an HTTP request for this path on a server that the target DNS resolve to.
        /// </summary>
        /// <param name="dnsId"></param>
        /// <param name="signer"></param>
        /// <param name="tls"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GenerateHttpChallengeAnswer(string dnsId, ISigner signer, bool tls)
        {
            var resp = new
            {
                type = "simpleHttp",
                token = Token,
                tls = tls
            };
            var json = JsonConvert.SerializeObject(resp);
            var hdrs = new { alg = signer.JwsAlg, jwk = signer.ExportJwk() };
            var signed = JwsHelper.SignFlatJsonAsObject(
                    signer.Sign, json, unprotectedHeaders: hdrs);

            return new KeyValuePair<string, string>(
                    $"{HTTP_CHALLENGE_PATHPREFIX}{Token}",
                    JsonConvert.SerializeObject(signed, Formatting.Indented));
        }
    }
}
