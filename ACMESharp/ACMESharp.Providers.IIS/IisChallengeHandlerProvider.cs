using ACMESharp.ACME;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.Providers.IIS
{
    [ChallengeHandlerProvider("iis", ChallengeTypeKind.HTTP,
		isCleanUpSupported: true,
        Label = "Internet Information Server (IIS)",
        Description = "Provider for handling Challenges that manages" +
                      " the local IIS site configuration.")]
    public class IisChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail WEB_SITE_REF = new ParameterDetail(
                nameof(IisChallengeHandler.WebSiteRef),
                ParameterType.TEXT, isRequired: true, label: "Web Site Ref",
                desc: "Either the Name or the ID of a local IIS Web Site");

        public static readonly ParameterDetail OVERRIDE_SITE_ROOT = new ParameterDetail(
                nameof(IisChallengeHandler.OverrideSiteRoot),
                ParameterType.TEXT, label: "Override Site Root",
                desc: "The full path to the site root to use, overriding the value defined in the"
                + " IIS Web Site settings.");

        public static readonly ParameterDetail SKIP_LOCAL_WEB_CONFIG = new ParameterDetail(
                nameof(IisChallengeHandler.SkipLocalWebConfig),
                ParameterType.BOOLEAN, label: "Skip Local Web Config",
                desc: "When enabled, inhibits the generation of a local web.config file that"
                + " controls MIME type mapping and IIS handler mapping.");

        static readonly ParameterDetail[] PARAMS =
        {
            WEB_SITE_REF,
            OVERRIDE_SITE_ROOT,
            SKIP_LOCAL_WEB_CONFIG,
        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public bool IsSupported(Challenge c)
        {
            return c is HttpChallenge;
        }

        public IChallengeHandler GetHandler(Challenge c, IReadOnlyDictionary<string, object> initParams)
        {
            var h = new IisChallengeHandler();

            if (initParams == null)
                initParams = new Dictionary<string, object>();

            // Required params
            if (!initParams.ContainsKey(WEB_SITE_REF.Name))
                throw new KeyNotFoundException($"missing required parameter [{WEB_SITE_REF.Name}]");
            h.WebSiteRef = (string)initParams[WEB_SITE_REF.Name];

            // Optional params
            if (initParams.ContainsKey(OVERRIDE_SITE_ROOT.Name))
                h.OverrideSiteRoot = (string)initParams[OVERRIDE_SITE_ROOT.Name];
            if (initParams.ContainsKey(SKIP_LOCAL_WEB_CONFIG.Name))
                h.SkipLocalWebConfig = (bool)initParams[SKIP_LOCAL_WEB_CONFIG.Name];

            return h;
        }
    }
}
