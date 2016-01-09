using ACMESharp.Providers.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.IIS.Config
{
    public class IisHandlerParams : BaseParams
    {
        public string WebSiteRef
        {
            get { return Get<string>(IisChallengeHandlerProvider.WEB_SITE_REF.Name); }
            set { this[IisChallengeHandlerProvider.WEB_SITE_REF.Name] = value; }
        }

        public bool SkipLocalWebConfig
        {
            get { return Get<bool>(IIS.IisChallengeHandlerProvider.SKIP_LOCAL_WEB_CONFIG.Name); }
            set { this[IisChallengeHandlerProvider.SKIP_LOCAL_WEB_CONFIG.Name] = value; }
        }

        public string OverrideSiteRoot
        {
            get { return Get<string>(IIS.IisChallengeHandlerProvider.OVERRIDE_SITE_ROOT.Name); }
            set { this[IisChallengeHandlerProvider.OVERRIDE_SITE_ROOT.Name] = value; }
        }
    }
}
