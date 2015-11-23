namespace ACMESharp.Messages
{
    public class AnswerDnsChallengeRequest : RequestMessage
    {
        public AnswerDnsChallengeRequest() : base("challenge")
        { }

        public string KeyAuthorization
        { get; set; }
    }
}
