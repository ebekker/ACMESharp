using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class AnswerLegacyHttpChallengeRequest : RequestMessage
    {
        public AnswerLegacyHttpChallengeRequest() : base("challenge")
        { }

        public string Type
        { get; private set; } = AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP;

        public bool Tls
        { get; set; }
    }
}
