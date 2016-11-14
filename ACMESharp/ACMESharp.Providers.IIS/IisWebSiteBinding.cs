using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.IIS
{
    public class IisWebSiteBinding
    {
        public long SiteId
        { get; set; }

        public string SiteName
        { get; set; }

        public string BindingProtocol
        { get; set; }

        public string BindingAddress
        { get; set; }

        public string BindingPort
        { get; set; }

        public string BindingHost
        { get; set; }

        public bool? BindingHostRequired
        { get; set; }

        public string SiteRoot
        { get; set; }
    }
}
