using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LetsEncrypt.ACME
{
    public abstract class RequestMessage
    {
        public RequestMessage(string resource)
        {
            Resource = resource;
        }
        public string Resource
        { get; set; }
    }
}
