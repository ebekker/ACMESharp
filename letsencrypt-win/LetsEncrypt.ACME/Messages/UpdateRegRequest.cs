using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class UpdateRegRequest : NewRegRequest
    {
        public UpdateRegRequest()
        {
            base.Resource = "reg";
        }
    }
}
