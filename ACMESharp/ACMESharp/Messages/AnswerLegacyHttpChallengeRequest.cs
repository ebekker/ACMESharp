namespace ACMESharp.Messages
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
