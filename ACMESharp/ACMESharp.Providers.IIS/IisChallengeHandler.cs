using ACMESharp.ACME;
using ACMESharp.Util;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.IIS
{
    /// <summary>
    /// Implements a <see cref="IChallengeHandler">Challenge Handler</see> that handles
    /// HTTP challenges by managing responses served up by the local IIS server.
    /// </summary>
    /// <remarks>
    /// Much of the implementation of this handler was initially adapted from the
    /// similarly-intentioned <see
    /// cref="https://github.com/Lone-Coder/letsencrypt-win-simple/blob/master/letsencrypt-win-simple/Plugin/IISPlugin.cs"
    /// ><code>IISPlugin</code></see> class from the <see
    /// cref="https://github.com/Lone-Coder/letsencrypt-win-simple"
    /// >letsencrypt-win-simple</see> project -- thank you, <see
    /// cref="https://github.com/Lone-Coder">Bryan</see>!
    /// </remarks>
    public class IisChallengeHandler : IChallengeHandler
    {
        #region -- Constants --

        public const string IIS_REG_KEY = @"Software\Microsoft\InetStp";
        public const string IIS_REG_MAJOR_VERS_VALNAME = "MajorVersion";
        public const string IIS_REG_MINOR_VERS_VALNAME = "MinorVersion";

        #endregion -- Constants --

        #region -- Properties --

        public string WebSiteRef
        { get; set; }

        public string OverrideSiteRoot
        { get; set; }

        public bool SkipLocalWebConfig
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public static bool IsAdministrator()
        {
            var winId = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(winId);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static Version GetIisVersion()
        {
            using (RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(IIS_REG_KEY, false))
            {
                if (componentsKey != null)
                {
                    int majorVersion = (int)componentsKey.GetValue(IIS_REG_MAJOR_VERS_VALNAME, -1);
                    int minorVersion = (int)componentsKey.GetValue(IIS_REG_MINOR_VERS_VALNAME, -1);

                    if (majorVersion != -1 && minorVersion != -1)
                        return new Version(majorVersion, minorVersion);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns just the distinct sites that have
        /// at least one HTTP binding, sorted by ID.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IisWebSite> ListHttpWebSites()
        {
            return ListWebSites().Where(x => "http" == x.BindingProtocol)
                .OrderBy(x => x.SiteId).Distinct(IisWebSiteComparer.INSTANCE);
        }

        /// <summary>
        /// Returns an enumeration of minimal IIS site details.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IisWebSite> ListWebSites()
        {
            if (GetIisVersion()?.Major == 0)
                yield break;

            using (var iis = new ServerManager())
            {
                foreach (var site in iis.Sites)
                    foreach (var binding in site.Bindings)
                        yield return new IisWebSite
                        {
                            SiteId = site.Id,
                            SiteName = site.Name,
                            SiteRoot = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                            BindingProtocol = binding.Protocol,
                            BindingAddress = binding?.EndPoint?.Address?.ToString(),
                            BindingPort = binding?.EndPoint?.Port.ToString(),
                            BindingHost = binding.Host,
                        };
            }
        }

        public void Handle(Challenge c)
        {
            AssertNotDisposed();
            var httpChallenge = (HttpChallenge)c;
            EditFile(httpChallenge, false);
        }

        public void CleanUp(Challenge c)
        {
            AssertNotDisposed();
            var httpChallenge = (HttpChallenge)c;
            EditFile(httpChallenge, true);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("IIS Challenge Handler is disposed");
        }

        private void EditFile(HttpChallenge httpChallenge, bool delete)
        {
            var httpSites = ListHttpWebSites().ToArray();
            var sitesById = httpSites.Where(x => WebSiteRef == x.SiteId.ToString()).ToArray();
            var sitesByName = httpSites.Where(x => WebSiteRef == x.SiteName).ToArray();

            IisWebSite site;

            if (sitesById.Length > 1)
                throw new InvalidOperationException("duplicate sites resolved for referenced site ID")
                        .With(nameof(WebSiteRef), WebSiteRef)
                        .With("count", sitesById.Length);
            if (sitesById.Length == 1)
                site = sitesById[0];
            else if (sitesByName.Length > 1)
                throw new InvalidOperationException("ambiguous reference, duplicate sites resolved for referenced site name")
                        .With(nameof(WebSiteRef), WebSiteRef)
                        .With("count", sitesByName.Length);
            else if (sitesByName.Length < 1)
                throw new InvalidOperationException("unresolved site for given site reference")
                        .With(nameof(WebSiteRef), WebSiteRef);
            else
                site = sitesByName[0];

            var siteRoot = site.SiteRoot;
            if (!string.IsNullOrEmpty(OverrideSiteRoot))
                siteRoot = OverrideSiteRoot;
            if (string.IsNullOrEmpty(siteRoot))
                throw new InvalidOperationException("missing root path for resolve site")
                        .With(nameof(IisWebSite.SiteId), site.SiteId)
                        .With(nameof(IisWebSite.SiteName), site.SiteName);

            // IIS-configured Site Root can use env vars
            siteRoot = Environment.ExpandEnvironmentVariables(siteRoot);

            // Make sure we're using the canonical full path
            siteRoot = Path.GetFullPath(siteRoot);

            // We need to strip off any leading '/' in the path
            var filePath = httpChallenge.FilePath;
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);

            var fullFilePath = Path.Combine(siteRoot, filePath);
            var fullDirPath = Path.GetDirectoryName(fullFilePath);
            var fullConfigPath = Path.Combine(fullDirPath, "web.config");

            // This meta-data file will be placed next to the actual
            // Challenge answer content file and it captures some details
            // that we need in order to properly clean up the handling of
            // this Challenge after it has been submitted
            var fullMetaPath = $"{fullFilePath}-acmesharp_meta";

            // Check if user is running with elevated privs and warn if not
            if (!IsAdministrator())
            {
                Console.Error.WriteLine("WARNING:  You are not running with elelvated privileges.");
                Console.Error.WriteLine("          Write access may be denied to the destination.");
            }

            if (delete)
            {
                bool skipLocalWebConfig = SkipLocalWebConfig;
                List<string> dirsCreated = null;

                // First see if there's a meta file there to help us out
                if (File.Exists(fullMetaPath))
                {
                    var meta = JsonHelper.Load<IisChallengeHandlerMeta>(
                            File.ReadAllText(fullMetaPath));

                    skipLocalWebConfig = meta.SkippedLocalWebConfig;
                    dirsCreated = meta.DirsCreated;
                }

                // Get rid of the Challenge answer content file
                if (File.Exists(fullFilePath))
                    File.Delete(fullFilePath);

                // Get rid of web.config if necessary
                if (!skipLocalWebConfig && File.Exists(fullConfigPath))
                    File.Delete(fullConfigPath);

                // Get rid of the meta file so that we can clean up the dirs
                if (File.Exists(fullMetaPath))
                    File.Delete(fullMetaPath);

                // Walk up the tree if needed
                if (dirsCreated?.Count > 0)
                {
                    dirsCreated.Reverse();
                    foreach (var dir in dirsCreated)
                    {
                        if (Directory.Exists(dir))
                        {
                            if (Directory.GetFileSystemEntries(dir).Length == 0)
                            {
                                Directory.Delete(dir);
                            }
                        }
                    }
                }
            }
            else
            {
                // Figure out which dirs we have to create so
                // we can capture and clean it up later on
                var meta = new IisChallengeHandlerMeta
                {
                    WasAdmin = IsAdministrator(),
                    SkippedLocalWebConfig = SkipLocalWebConfig,
                    DirsCreated = new List<string>(),
                };

                // In theory this ascending of the dir path should work
                // just fine, but just in case, this path segment counter
                // should gaurd against the possibility of an infinite loop
                var dirLimit = 100;

                var testDir = fullDirPath;
                while (!Directory.Exists(testDir))
                {
                    // Sanity check against an infinite loop
                    if (--dirLimit <= 0)
                        throw new Exception("Unexpected directory path segment count reached")
                                .With(nameof(dirLimit), "100")
                                .With(nameof(fullDirPath), fullDirPath)
                                .With(nameof(testDir), testDir)
                                .With($"first-{nameof(meta.DirsCreated)}",
                                        meta.DirsCreated[0])
                                .With($"last-{nameof(meta.DirsCreated)}",
                                        meta.DirsCreated[meta.DirsCreated.Count - 1]);

                    if (Path.GetFullPath(testDir) == siteRoot)
                        break;

                    // Add to the top of the list
                    meta.DirsCreated.Insert(0, testDir);
                    // Move to the parent
                    testDir = Path.GetDirectoryName(testDir);
                }

                foreach (var dir in meta.DirsCreated)
                    Directory.CreateDirectory(dir);

                File.WriteAllText(fullFilePath, httpChallenge.FileContent);
                File.WriteAllText(fullMetaPath, JsonHelper.Save(meta));

                if (!SkipLocalWebConfig)
                {
                    var t = typeof(IisChallengeHandler);
                    var r = $"{t.Namespace}.{t.Name}-WebConfig";
                    using (Stream rs = t.Assembly.GetManifestResourceStream(r))
                    {
                        using (var fs = new FileStream(fullConfigPath, FileMode.Create))
                        {
                            rs.CopyTo(fs);
                        }
                    }
                }
            }
        }

        #endregion -- Methods --

        #region -- Types --

        private class IisWebSiteComparer : EqualityComparer<IisWebSite>
        {
            public static readonly IisWebSiteComparer INSTANCE = new IisWebSiteComparer();

            public override bool Equals(IisWebSite x, IisWebSite y)
            {
                return x?.SiteId == y?.SiteId;
            }

            public override int GetHashCode(IisWebSite obj)
            {
                return $"{obj?.SiteId}".GetHashCode();
            }
        }

        public class IisChallengeHandlerMeta
        {
            public bool WasAdmin
            { get; set; }

            public bool SkippedLocalWebConfig
            { get; set; }

            public List<string> DirsCreated
            { get; set; }
        }

        #endregion -- Types --
    }
}
