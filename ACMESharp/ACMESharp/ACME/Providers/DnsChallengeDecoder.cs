using System.IO;
using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.Util;
using Newtonsoft.Json;

namespace ACMESharp.ACME.Providers
{
    public class DnsChallengeDecoder : IChallengeDecoder
    {
        public bool IsDisposed
        { get; private set; }

        public Challenge Decode(IdentifierPart ip, ChallengePart cp, ISigner signer)
        {
            if (cp.Type != AcmeProtocol.CHALLENGE_TYPE_DNS)
                throw new InvalidDataException("unsupported Challenge type")
                    .With("challengeType", cp.Type)
                    .With("supportedChallengeTypes", AcmeProtocol.CHALLENGE_TYPE_DNS);

            //var token = (string)cp["token"];
            var token = cp.Token;

            // This response calculation is described in:
            //    https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.5

            var keyAuthz = JwsHelper.ComputeKeyAuthorization(signer, token);
            var keyAuthzDig = JwsHelper.ComputeKeyAuthorizationDigest(signer, token);

            var ca = new DnsChallengeAnswer
            {
                KeyAuthorization = keyAuthz,
            };

            var c = new DnsChallenge(cp.Type, ca)
            {
                Token = token,
                RecordName = $"{AcmeProtocol.DNS_CHALLENGE_NAMEPREFIX}{ip.Value}",
                RecordValue = keyAuthzDig,
            };

            return c;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
