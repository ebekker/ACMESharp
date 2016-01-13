using System.IO;
using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.Util;

namespace ACMESharp.ACME.Providers
{
    public class HttpChallengeDecoder : IChallengeDecoder
    {
        public bool IsDisposed
        { get; private set; }

        public Challenge Decode(IdentifierPart ip, ChallengePart cp, ISigner signer)
        {
            if (cp.Type != AcmeProtocol.CHALLENGE_TYPE_HTTP)
                throw new InvalidDataException("unsupported Challenge type")
                    .With("challengeType", cp.Type)
                    .With("supportedChallengeTypes", AcmeProtocol.CHALLENGE_TYPE_HTTP);

            //var token = (string)cp["token"];
            var token = cp.Token;

            // This response calculation is described in:
            //    https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.2

            var keyAuthz = JwsHelper.ComputeKeyAuthorization(signer, token);
            var path = $"{AcmeProtocol.HTTP_CHALLENGE_PATHPREFIX}{token}";
            var url = $"http://{ip.Value}/{path}";


            var ca = new HttpChallengeAnswer
            {
                KeyAuthorization = keyAuthz,
            };

            var c = new HttpChallenge(cp.Type, ca)
            {
                Token = token,
                FileUrl = url,
                FilePath = path,
                FileContent = keyAuthz,
            };

            return c;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
