using ACMESharp.Ext;
using ACMESharp.Vault;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH.Util
{
    public static class PoshHelper
    {
        static PoshHelper()
        {
            // We have to override the base directory used to search for
            // Extension assemblies because under PowerShell, the base
            // directory happens to be where the PowerShell binary is
            // running from, not where the ACMESharp PS Module lives

            var baseUri = new Uri(typeof(VaultExtManager).Assembly.CodeBase);
            var baseDir = Path.GetDirectoryName(baseUri.AbsolutePath);

            ExtCommon.BaseDirectoryOverride = baseDir;
            ExtCommon.RelativeSearchPathOverride = string.Empty;
        }

        /// <summary>
        /// This routine must be invoked from any CMDLET that relies on the Ext
        /// mechanism when running under POSH, but does not make use of Vault.
        /// </summary>
        public static void BeforeExtAccess()
        {
            // This is a no-op routine but by accessing this from a POSH cmdlet
            // it will force the class constructor to be called which will make
            // sure the Ext mechanism is properly initilized for use under POSH
        }

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
