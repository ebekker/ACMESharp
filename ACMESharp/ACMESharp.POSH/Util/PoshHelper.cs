using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH.Util
{
    public static class PoshHelper
    {
        public static IDictionary<K,V> Convert<K, V>(this Hashtable h, IDictionary<K, V> d = null)
        {
            if (h == null)
                return d;

            if (d == null)
                d = new Dictionary<K, V>();

            foreach (var k in h.Keys)
                d.Add((K)k, (V)h[k]);

            return d;
        }
    }
}
