using ACMESharp.Util;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.IIS
{
    public static class IisHelper
    {
        #region -- Constants --

        public const string IIS_REG_KEY = @"Software\Microsoft\InetStp";
        public const string IIS_REG_MAJOR_VERS_VALNAME = "MajorVersion";
        public const string IIS_REG_MINOR_VERS_VALNAME = "MinorVersion";

        #endregion -- Constants --

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
        public static IEnumerable<IisWebSiteBinding> ListDistinctHttpWebSites()
        {
            return ListWebSitesBindings().Where(_ => "http" == _.BindingProtocol)
                .OrderBy(_ => _.SiteId).Distinct(IisWebSiteComparer.INSTANCE);
        }

        /// <summary>
        /// Returns just the distinct sites that have sorted by ID.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IisWebSiteBinding> ListDistinctWebSites()
        {
            return ListWebSitesBindings()
                .OrderBy(_ => _.SiteId).Distinct(IisWebSiteComparer.INSTANCE);
        }

        /// <summary>
        /// Returns an enumeration of minimal IIS site details.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IisWebSiteBinding> ListWebSitesBindings()
        {
            if (GetIisVersion()?.Major == 0)
                yield break;

            using (var iis = new ServerManager())
            {
                foreach (var site in iis.Sites)
                    foreach (var binding in site.Bindings)
                        yield return new IisWebSiteBinding
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

        public static void UpdateSiteBinding(IisWebSiteBinding binding, string certStore, byte[] certHash)
        {
            using (var iis = new ServerManager())
            {
                var sites = iis.Sites.Where(_ => _.Id == binding.SiteId).ToArray();
                if (sites?.Length == 0)
                    throw new ArgumentException("no matching sites found")
                            .With(nameof(binding.SiteId), binding.SiteId);

                var bindingCount = 0;
                foreach (var site in sites)
                {
                    foreach (var b in site.Bindings)
                    {
                        if (!string.Equals(binding.BindingProtocol, b.Protocol))
                            continue;
                        if (!string.Equals(binding.BindingAddress, b.EndPoint?.Address?.ToString()))
                            continue;
                        if (!string.Equals(binding.BindingHost, b.Host))
                            continue;
                        if (!string.Equals(binding.BindingPort, b.EndPoint?.Port.ToString()))
                            continue;

                        ++bindingCount;
                        b.CertificateStoreName = certStore;
                        b.CertificateHash = certHash;
                        if (binding.BindingHostRequired.GetValueOrDefault() && GetIisVersion().Major >= 8)
                            b.SetAttributeValue("sslFlags", 1);
                        else
                            b.SetAttributeValue("sslFlags", 3);
                    }
                }

                if (bindingCount == 0)
                    throw new ArgumentException("no matching bindings found")
                            .With(nameof(binding.SiteId), binding.SiteId)
                            .With(nameof(binding.BindingProtocol), binding.BindingProtocol)
                            .With(nameof(binding.BindingAddress), binding.BindingAddress)
                            .With(nameof(binding.BindingPort), binding.BindingPort)
                            .With(nameof(binding.BindingHost), binding.BindingHost);

                iis.CommitChanges();
            }
        }

        public static void CreateSiteBinding(IisWebSiteBinding binding, string certStore, byte[] certHash)
        {
            using (var iis = new ServerManager())
            {
                var sites = iis.Sites.Where(_ => _.Id == binding.SiteId).ToArray();
                if (sites?.Length == 0)
                    throw new ArgumentException("no matching sites found")
                            .With(nameof(binding.SiteId), binding.SiteId);

                foreach (var site in sites)
                {
                    // Binding Information spec
                    //      (https://msdn.microsoft.com/en-us/library/bb339271(v=vs.90).aspx):
                    //
                    //    IpAddr:Port:HostHeader
                    //
                    // where IpAddr can be * for all interfaces
                    // where HostHeader is only valid for SNI-capable IIS (8+)

                    var bindingAddr = string.IsNullOrEmpty(binding.BindingAddress)
                            ? "*"
                            : binding.BindingAddress;
                    var bindingPort = string.IsNullOrEmpty(binding.BindingPort)
                            ? "443"
                            : int.Parse(binding.BindingPort).ToString();
                    var bindingHost = string.IsNullOrEmpty(binding.BindingHost)
                            ? ""
                            : binding.BindingHost;

                    var bindingInfo = $"{bindingAddr}:{bindingPort}:{bindingHost}";
                    var b = site.Bindings.Add(bindingInfo, certHash, certStore);
                }

                iis.CommitChanges();
            }
        }

        /// <summary>
        /// Find and return all site bindings under a single IIS site
        /// matching the site reference of either a site ID or a site
        /// Name from a collection of sites.
        /// </summary>
        /// <param name="webSiteRef">a site ID or site name</param>
        /// <returns></returns>
        public static IEnumerable<IisWebSiteBinding> ResolveSiteBindings(string webSiteRef,
                IEnumerable<IisWebSiteBinding> bindings = null)
        {
            if (bindings == null)
                bindings = ListWebSitesBindings();
            var sitesById = bindings.Where(_ => webSiteRef == _.SiteId.ToString()).ToArray();
            var sitesByName = bindings.Where(_ => webSiteRef == _.SiteName).ToArray();

            IEnumerable<IisWebSiteBinding> resolvedSites = null;

            if (sitesById.Length > 0)
                resolvedSites = sitesById;
            else if (sitesByName.Length > 0)
                resolvedSites = sitesByName;

            if (resolvedSites == null)
                throw new InvalidOperationException("unresolved site for given site reference")
                        .With(nameof(webSiteRef), webSiteRef);

            // Make sure all the bindings are for the same site
            var siteId = resolvedSites.First().SiteId;
                if (resolvedSites.Any(_ => _.SiteId != siteId))
                    throw new InvalidOperationException("duplicate sites resolved for referenced site ID")
                            .With(nameof(webSiteRef), webSiteRef)
                            .With("count", resolvedSites.Count());

            return resolvedSites;
        }

        /// <summary>
        /// Find and return all site bindings under matching the
        /// target combination of binding parameters.
        /// </summary>
        public static IEnumerable<IisWebSiteBinding> ResolveSiteBindings(
                string bindingAddress, int bindingPort, string bindingHost,
                IEnumerable<IisWebSiteBinding> bindings = null)
        {
            if (bindings == null)
                bindings = ListWebSitesBindings();

            return bindings.Where(_ =>
            {
                var matchAddress = string.IsNullOrEmpty(bindingAddress)
                        || string.Equals(_.BindingAddress, bindingAddress,
                                StringComparison.InvariantCultureIgnoreCase);
                var matchPort = int.Parse(_.BindingPort) == bindingPort;
                var matchHost = string.IsNullOrEmpty(bindingHost)
                        || string.Equals(_.BindingHost, bindingHost,
                                StringComparison.InvariantCultureIgnoreCase);

                return matchAddress && matchPort && matchHost;
            });
        }

        /// <summary>
        /// Find and return a site matching the site reference of either
        /// a site ID or a site Name from a collection of sites.
        /// </summary>
        /// <param name="webSiteRef">a site ID or site name</param>
        /// <returns></returns>
        public static IisWebSiteBinding ResolveSingleSite(string webSiteRef,
                IEnumerable<IisWebSiteBinding> bindings = null)
        {
            if (bindings == null)
                bindings = ListWebSitesBindings();
            var sitesById = bindings.Where(_ => webSiteRef == _.SiteId.ToString()).ToArray();
            var sitesByName = bindings.Where(_ => webSiteRef == _.SiteName).ToArray();

            IisWebSiteBinding site;

            if (sitesById.Length > 1)
                throw new InvalidOperationException("duplicate sites resolved for referenced site ID")
                        .With(nameof(webSiteRef), webSiteRef)
                        .With("count", sitesById.Length);
            if (sitesById.Length == 1)
                site = sitesById[0];
            else if (sitesByName.Length > 1)
                throw new InvalidOperationException("ambiguous reference, duplicate sites resolved for referenced site name")
                        .With(nameof(webSiteRef), webSiteRef)
                        .With("count", sitesByName.Length);
            else if (sitesByName.Length < 1)
                throw new InvalidOperationException("unresolved site for given site reference")
                        .With(nameof(webSiteRef), webSiteRef);
            else
                site = sitesByName[0];

            return site;
        }

        #endregion -- Methods --

        #region -- Types --

        private class IisWebSiteComparer : EqualityComparer<IisWebSiteBinding>
        {
            public static readonly IisWebSiteComparer INSTANCE = new IisWebSiteComparer();

            public override bool Equals(IisWebSiteBinding x, IisWebSiteBinding y)
            {
                return x?.SiteId == y?.SiteId;
            }

            public override int GetHashCode(IisWebSiteBinding obj)
            {
                return $"{obj?.SiteId}".GetHashCode();
            }
        }

        #endregion -- Types --
    }
}
