using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class AnswerDnsChallengeRequest : RequestMessage
    {
        public AnswerDnsChallengeRequest() : base("challenge")
        { }

        public string KeyAuthorization
        { get; set; }
    }
}
