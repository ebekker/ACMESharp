using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Util
{
    public static class EntityHelper
    {
        public static Guid NewId()
        {
            return Guid.NewGuid();
        }


    }
}
