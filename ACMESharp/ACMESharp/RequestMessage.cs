namespace ACMESharp
{
    public abstract class RequestMessage
    {
        public RequestMessage(string resource)
        {
            Resource = resource;
        }
        public string Resource
        { get; }
    }
}
