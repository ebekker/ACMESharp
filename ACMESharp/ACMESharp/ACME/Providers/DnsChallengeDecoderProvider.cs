using ACMESharp.Messages;

namespace ACMESharp.ACME.Providers
{
    [ChallengeDecoderProvider("dns-01", ChallengeTypeKind.DNS,
        Description = "Challenge type decoder for the DNS type" +
                      " as specified in" +
                      " https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.5")]
    public class DnsChallengeDecoderProvider : IChallengeDecoderProvider
    {
        public bool IsSupported(IdentifierPart ip, ChallengePart cp)
        {
            return AcmeProtocol.CHALLENGE_TYPE_DNS == cp.Type;
        }

        public IChallengeDecoder GetDecoder(IdentifierPart ip, ChallengePart cp)
        {
            return new DnsChallengeDecoder();
        }
    }
}
