namespace ACMESharp.Messages
{
    public class AnswerDnsChallengeRequest : RequestMessage
    {
        public AnswerDnsChallengeRequest() : base("challenge")
        { }

        public string Type
        { get; private set; } = AcmeProtocol.CHALLENGE_TYPE_DNS;

        // TODO: carried over from legacy DNS challenge
        public object ClientPublicKey
        { get; set; }

        // TODO: carried over from legacy DNS challenge
        public object Validation
        { get; set; }

        public string KeyAuthorization
        { get; set; }
    }
}
