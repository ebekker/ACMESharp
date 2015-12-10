using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Messages;

namespace ACMESharp.ACME.Providers
{
    [ChallengeParserProvider("http-01", ChallengeTypeKind.DNS,
        Description = "Challenge type parser for the HTTP type" +
                      " as specified in" +
                      " https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.2")]
    public class HttpChallengeParserProvider : IChallengeParserProvider
    {
        public bool IsSupported(IdentifierPart ip, ChallengePart cp)
        {
            return AcmeProtocol.CHALLENGE_TYPE_HTTP == cp.Type;
        }

        public IChallengeParser GetParser(IdentifierPart ip, ChallengePart cp)
        {
            return new HttpChallengeParser();
        }
    }
}
