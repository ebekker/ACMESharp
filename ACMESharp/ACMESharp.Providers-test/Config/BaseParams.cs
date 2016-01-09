using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.Config
{
    public class BaseParams : Dictionary<string, object>
    {
        protected T Get<T>(string key)
        {
            if (ContainsKey(key))
                return (T)this[key];
            else
                return default(T);
        }
    }
}
