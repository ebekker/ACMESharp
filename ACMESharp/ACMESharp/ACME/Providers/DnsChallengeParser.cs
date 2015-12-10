using System.IO;
using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.Util;
using Newtonsoft.Json;

namespace ACMESharp.ACME.Providers
{
    public class DnsChallengeParser : IChallengeParser
    {
        public bool IsDisposed
        { get; private set; }

        public Challenge Parse(IdentifierPart ip, ChallengePart cp, ISigner signer)
        {
            if (cp.Type != AcmeProtocol.CHALLENGE_TYPE_DNS)
                throw new InvalidDataException("unsupported Challenge type")
                    .With("challengeType", cp.Type)
                    .With("supportedChallengeTypes", AcmeProtocol.CHALLENGE_TYPE_DNS);

            //var token = (string)cp["token"];
            var token = cp.Token;

            var resp = new
            {
                type = AcmeProtocol.CHALLENGE_TYPE_DNS,
                token = token,
            };
            var json = JsonConvert.SerializeObject(resp);
            var hdrs = new { alg = signer.JwsAlg, jwk = signer.ExportJwk() };
            var signed = JwsHelper.SignFlatJsonAsObject(
                signer.Sign, json, unprotectedHeaders: hdrs);

            /*
            // NO LONGER DO THIS BY DEFAULT!
            // We format it as a set of lines broken on 100-character boundaries to make it
            // easier to copy and put into a DNS TXT RR which normally have a 255-char limit
            // so this result may need to be broken up into multiple smaller TXT RR entries
            var sigFormatted = Regex.Replace(signed.signature,
                    "(.{100,100})", "$1\r\n");
            */

            var c = new DnsChallenge
            {
                TypeKind = ChallengeTypeKind.DNS,
                Type = cp.Type,
                Token = token,
                RecordName = $"{AcmeProtocol.DNS_CHALLENGE_NAMEPREFIX}{ip.Value}",
                RecordValue = signed.signature,
            };

            return c;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
