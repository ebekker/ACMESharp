using ACMESharp.Messages;

namespace ACMESharp.ACME.Providers
{
    [ChallengeParserProvider("dns-01", ChallengeTypeKind.DNS,
        Description = "Challenge type parser for the DNS type" +
                      " as specified in" +
                      " https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.5")]
    public class DnsChallengeParserProvider : IChallengeParserProvider
    {
        public bool IsSupported(IdentifierPart ip, ChallengePart cp)
        {
            return AcmeProtocol.CHALLENGE_TYPE_DNS == cp.Type;
        }

        public IChallengeParser GetParser(IdentifierPart ip, ChallengePart cp)
        {
            return new DnsChallengeParser();
        }
    }
}
