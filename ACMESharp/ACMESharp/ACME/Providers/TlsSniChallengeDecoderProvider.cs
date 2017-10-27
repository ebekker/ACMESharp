using ACMESharp.Messages;

namespace ACMESharp.ACME.Providers
{
    [ChallengeDecoderProvider(AcmeProtocol.CHALLENGE_TYPE_SNI, ChallengeTypeKind.TLS_SNI,
        Description = "Challenge type decoder for the TLS-SNI type" +
                      " as specified in" +
                      " https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.3")]
    public class TlsSniChallengeDecoderProvider : IChallengeDecoderProvider
    {
        public bool IsSupported(IdentifierPart ip, ChallengePart cp)
        {
            return AcmeProtocol.CHALLENGE_TYPE_SNI == cp.Type;
        }

        public IChallengeDecoder GetDecoder(IdentifierPart ip, ChallengePart cp)
        {
            return new TlsSniChallengeDecoder();
        }
    }
}
