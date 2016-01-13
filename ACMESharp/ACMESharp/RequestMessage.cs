namespace ACMESharp
{
    public abstract class RequestMessage
    {
        protected RequestMessage(string resource)
        {
            Resource = resource;
        }
        public string Resource
        { get; }
    }
}
