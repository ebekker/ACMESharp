using System;
using System.Runtime.Serialization;

namespace ACMESharp
{
    public class AcmeException : Exception
    {
        public AcmeException()
        {
        }

        public AcmeException(string message) : base(message)
        {
        }

        public AcmeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AcmeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
