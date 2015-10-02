using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Util
{
    public static class StringHelper
    {
        public static string IfNullOrEmpty(string s, string v1 = null)
        {
            if (string.IsNullOrEmpty(s))
                return v1;
            return s;
        }
    }
}
