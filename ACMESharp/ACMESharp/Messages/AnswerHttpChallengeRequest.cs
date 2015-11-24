namespace ACMESharp.Messages
{
    public class AnswerHttpChallengeRequest : RequestMessage
    {
        public AnswerHttpChallengeRequest() : base("challenge")
        { }

        public string KeyAuthorization
        { get; set; }
    }
}
