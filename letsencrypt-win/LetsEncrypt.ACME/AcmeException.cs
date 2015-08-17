using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
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
