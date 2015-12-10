using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.Util;

namespace ACMESharp.ACME.Providers
{
    public class HttpChallengeParser : IChallengeParser
    {
        public bool IsDisposed
        { get; private set; }

        public Challenge Parse(IdentifierPart ip, ChallengePart cp, ISigner signer)
        {
            if (cp.Type != AcmeProtocol.CHALLENGE_TYPE_HTTP)
                throw new InvalidDataException("unsupported Challenge type")
                    .With("challengeType", cp.Type)
                    .With("supportedChallengeTypes", AcmeProtocol.CHALLENGE_TYPE_HTTP);

            //var token = (string)cp["token"];
            var token = cp.Token;

            var keyAuthz = JwsHelper.ComputeKeyAuthorization(signer, token);
            var path = $"{AcmeProtocol.HTTP_CHALLENGE_PATHPREFIX}{token}";
            var url = $"http://{ip.Value}/{path}";

            var c = new HttpChallenge
            {
                TypeKind = ChallengeTypeKind.HTTP,
                Type = cp.Type,
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
