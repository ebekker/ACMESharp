using ACMESharp.Messages;

namespace ACMESharp.ACME.Providers
{
    [ChallengeDecoderProvider("http-01", ChallengeTypeKind.HTTP,
        Description = "Challenge type decoder for the HTTP type" +
                      " as specified in" +
                      " https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.2")]
    public class HttpChallengeDecoderProvider : IChallengeDecoderProvider
    {
        public bool IsSupported(IdentifierPart ip, ChallengePart cp)
        {
            return AcmeProtocol.CHALLENGE_TYPE_HTTP == cp.Type;
        }

        public IChallengeDecoder GetDecoder(IdentifierPart ip, ChallengePart cp)
        {
            return new HttpChallengeDecoder();
        }
    }
}
