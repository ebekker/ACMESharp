using ACMESharp.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
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

        public static string BaseDirectoryOverride
        { get; set; }

        public static string RelativeSearchPathOverride
        { get; set; }

        /// <summary>
        /// When true, includes the immediate children of the extension folder
        /// as extension locations to be included in the aggregate catalog.
        /// </summary>
        public static bool IncludeExtPathFolders
        { get; set; } = true;

        public static bool IncludeExtPathLinks
        { get; set; } = true;

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

        public static TExtConfig ReloadExtConfig<TExtConfig>(TExtConfig existing) where TExtConfig : new()
        {
            if (existing != null)
            {
                if (existing is IExtDetail)
                {
                    var extDetail = (IExtDetail)existing;
                    if (extDetail?.CompositionContainer != null)
                    {
                        extDetail.CompositionContainer.Catalog?.Dispose();
                        extDetail.CompositionContainer.Dispose();
                    }
                }

                if (existing is IDisposable)
                {
                    ((IDisposable)existing).Dispose();
                }
            }
            return InitExtConfig<TExtConfig>();
        }

        public static TExtConfig InitExtConfig<TExtConfig>() where TExtConfig : new()
        {
            var aggCat = new AggregateCatalog();

            // Add the assembly that contains the current Component/Provider Scaffold
            var thisAsm = Assembly.GetExecutingAssembly();
            aggCat.Catalogs.Add(new AssemblyCatalog(thisAsm));

            // Add assemblies in the current apps path and runtime
            aggCat.Catalogs.Add(new AppCatalog(BaseDirectoryOverride, RelativeSearchPathOverride));

            // Add the local extension folder if it exists
            var thisExt = ExtCommon.GetExtPath();
            if (Directory.Exists(thisExt))
            {
                aggCat.Catalogs.Add(new DirectoryCatalog(thisExt));

                if (IncludeExtPathFolders)
                {
                    // Add each immediate child directory as well
                    foreach (var d in Directory.GetDirectories(thisExt))
                    {
                        aggCat.Catalogs.Add(new DirectoryCatalog(d));
                    }
                }

                if (IncludeExtPathLinks)
                {
                    // Add each folder that's defined in ExtPathLink definition file
                    foreach (var f in Directory.GetFiles(thisExt, "*.extlnk"))
                    {
                        try
                        {
                            var epl = JsonHelper.Load<ExtPathLink>(File.ReadAllText(f));
                            aggCat.Catalogs.Add(new DirectoryCatalog(epl.Path));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("failed to resolve extension link", ex)
                                    .With(nameof(ExtPathLink), f);
                        }
                    }
                }
            }

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

        /// <summary>
        /// An abreviated form of the <see cref="ApplicationCatalog"/> that allows us to
        /// override the base directory and relative search paths.
        /// </summary>
        public class AppCatalog : AggregateCatalog
        {
            public AppCatalog(string baseDir = null, string relSearchPath = null)
            {
                // This logic is 'borrowed' from the decompiled guts of the class
                //    <<System.ComponentModel.Composition.Hosting.ApplicationCatalog>>
                // The behavior is mostly the same, but it is simplified for our
                // particular use-case, and enhanced to support setting the BaseDir 

                if (baseDir == null)
                    baseDir = AppDomain.CurrentDomain.BaseDirectory;

                if (relSearchPath == null)
                    relSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;

                BaseDirectory = baseDir;
                RelativeSearchPath = relSearchPath;

                Catalogs.Add(new DirectoryCatalog(baseDir, "*.dll"));
                Catalogs.Add(new DirectoryCatalog(baseDir, "*.exe"));
                if (!string.IsNullOrEmpty(relSearchPath))
                {
                    foreach (var path in relSearchPath.Split(new char[] { ';' },
                            StringSplitOptions.RemoveEmptyEntries))
                    {
                        var catDir = Path.Combine(baseDir, path);
                        if (Directory.Exists(catDir))
                            Catalogs.Add(new DirectoryCatalog(catDir, "*.dll"));
                    }
                }
            }

            public string BaseDirectory
            { get; }

            public string RelativeSearchPath
            { get; }
        }

        public class ExtPathLink
        {
            public string Path
            { get; set; }
        }
    }
}
