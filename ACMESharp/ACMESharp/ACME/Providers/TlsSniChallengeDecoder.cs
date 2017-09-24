using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.Util;
using NLog;
using System.IO;

namespace ACMESharp.ACME.Providers
{
    public class TlsSniChallengeDecoder : IChallengeDecoder
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public bool IsDisposed
        { get; private set; }

        public Challenge Decode(IdentifierPart ip, ChallengePart cp, ISigner signer)
        {
            if (cp.Type != AcmeProtocol.CHALLENGE_TYPE_SNI)
                throw new InvalidDataException("unsupported Challenge type")
                    .With("challengeType", cp.Type)
                    .With("supportedChallengeTypes", AcmeProtocol.CHALLENGE_TYPE_SNI);

            //var token = (string)cp["token"];
            var token = cp.Token;

            // This response calculation is described in:
            //    https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.5

            var keyAuthz = JwsHelper.ComputeKeyAuthorization(signer, token);
            var keyAuthzDig = JwsHelper.ComputeKeyAuthorizationDigest(signer, token);

            LOG.Debug("Computed key authorization {0} and digest {1}", keyAuthz, keyAuthzDig);

            var ca = new TlsSniChallengeAnswer
            {
                KeyAuthorization = keyAuthz,
            };

            var c = new TlsSniChallenge(cp.Type, ca)
            {
                Token = token
            };

            return c;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}