using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using ACMESharp.ACME;
using ACMESharp.JOSE;
using ACMESharp.Messages;

namespace ACMESharp
{
    public class AuthorizeChallenge
    {
        public const string STATUS_PENDING = "pending";
        public const string STATUS_VALID = "valid";
        public const string STATUS_INVALID = "invalid";

        public ChallengePart ChallengePart
        { get; set; }

        public Challenge Challenge
        { get; set; }

        public string Type
        { get; set; }

        public string Uri
        { get; set; }

        public string Token
        { get; set; }

        public string Status
        { get; set; }

        public KeyValuePair<string, string> OldChallengeAnswer
        { get; set; }

        public object ChallengeAnswerMessage
        { get; set; }

        public string HandlerName
        { get; set; }

        public DateTime? HandlerHandleDate
        { get; set; }

        public DateTime? HandlerCleanUpDate
        { get; set; }

        public DateTime? SubmitDate
        { get; set; }

        public object SubmitResponse
        { get; set; }

        //public object ValidationRecord
        //{ get; set; }

        public bool IsPending()
        {
            return string.IsNullOrEmpty(Status) || string.Equals(Status, STATUS_PENDING,
                    StringComparison.InvariantCultureIgnoreCase);
        }

        public bool IsInvalid()
        {
            return string.Equals(Status, STATUS_INVALID,
                    StringComparison.InvariantCultureIgnoreCase);
        }

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
                type = AcmeProtocol.CHALLENGE_TYPE_DNS,
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
                    $"{AcmeProtocol.DNS_CHALLENGE_NAMEPREFIX}{dnsId}",
                    signed.signature); /*sigFormatted);*/
        }

        /// <summary>
        /// Returns a key-value pair that represents the HTTP resource path that
        /// needs to be configured (the key) and the resource content that should be returned
        /// for an HTTP request for this path on a server that the target DNS resolve to.
        /// </summary>
        /// <param name="dnsId"></param>
        /// <param name="signer"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GenerateHttpChallengeAnswer(string dnsId, ISigner signer)
        {
            var keyAuthz = JwsHelper.ComputeKeyAuthorization(signer, Token);
            
            return new KeyValuePair<string, string>(
                    $"{AcmeProtocol.HTTP_CHALLENGE_PATHPREFIX}{Token}", keyAuthz);
        }
    }
}
