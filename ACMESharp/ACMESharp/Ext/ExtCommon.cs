using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Ext
{
    public static class ExtCommon
    {
        private const string EXT_DIR = "ext";

        public static string GetExtPath()
        {
            var thisAsm = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(thisAsm))
                return null;

            var thisDir = Path.GetDirectoryName(thisAsm);
            if (string.IsNullOrEmpty(thisDir))
                return null;

            return Path.Combine(thisDir, EXT_DIR);
        }

        public static TExtConfig InitExtConfig<TExtConfig>() where TExtConfig : new()
        {
            var aggCat = new AggregateCatalog();

            // Add the assembly that contains the current Component/Provider Scaffold
            var thisAsm = Assembly.GetExecutingAssembly();
            aggCat.Catalogs.Add(new AssemblyCatalog(thisAsm));

            // Add the local extension folder
            var thisExt = ExtCommon.GetExtPath();
            aggCat.Catalogs.Add(new DirectoryCatalog(thisExt));

            // Other possible folders to include:
            //    * Application CWD
            //    * PATH
            //    * User-specific ext folder
            //    * System-wide ext folder

            var config = new TExtConfig();

            // Guard this with a try-catch if we want to do something
            // in an error situation other than let it throw up
            var cc = new CompositionContainer(aggCat);
            cc.ComposeParts(config);

            if (config is IExtDetail)
            {
                ((IExtDetail) config).CompositionContainer = cc;
            }
            else
            {
                cc.Dispose();
            }

            return config;
        }
    }
}
