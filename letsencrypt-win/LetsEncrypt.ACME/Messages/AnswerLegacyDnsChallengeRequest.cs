using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class AnswerLegacyDnsChallengeRequest : RequestMessage
    {
        public AnswerLegacyDnsChallengeRequest() : base("challenge")
        { }

        public string Type
        { get; private set; } = AcmeProtocol.CHALLENGE_TYPE_DNS;

        public object ClientPublicKey
        { get; set; }

        public object Validation
        { get; set; }
    }
}
