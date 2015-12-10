using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Util
{
    public static class ExceptionExtensions
    {
        public static Exception With(this Exception ex, string key, object val)
        {
            ex.Data[key] = val;
            return ex;
        }
    }
}
