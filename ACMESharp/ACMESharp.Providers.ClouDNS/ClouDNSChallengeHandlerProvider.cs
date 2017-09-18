using ACMESharp.ACME;
using ACMESharp.Ext;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.Providers.ClouDNS
{
    [ChallengeHandlerProvider(
        "ClouDNS",
        ChallengeTypeKind.DNS,
        Label = "ClouDNS",
        Description = "Provider for handling challenges that manages DNS entries hosted in a ClouDNS zone")]
    public class ClouDNSChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail DomainName =
            new ParameterDetail(nameof(ClouDNSChallengeHandler.DomainName), ParameterType.TEXT, isRequired: true,
                label: "Domain name", desc: "The domain name to operate against.");

        public static readonly ParameterDetail AuthId =
            new ParameterDetail(nameof(ClouDNSChallengeHandler.AuthId), ParameterType.TEXT, isRequired: true,
                label: "Authenticaton ID", desc: "The authentication ID of the ClouDNS account to operate against");

        public static readonly ParameterDetail AuthPassword =
            new ParameterDetail(nameof(ClouDNSChallengeHandler.AuthPassword), ParameterType.TEXT, isRequired: true,
                label: "Authentication Password", desc: "The authentication password of the ClouDNS account to operate against");

        private static readonly ParameterDetail[] Params =
        {
            DomainName,
            AuthId,
            AuthPassword
        };
        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return Params;
        }

        public bool IsSupported(Challenge c)
        {
            return c is DnsChallenge;
        }

        public IChallengeHandler GetHandler(Challenge c, IReadOnlyDictionary<string, object> initParams)
        {
            var handler = new ClouDNSChallengeHandler();
            if (initParams == null)
            {
                initParams = new Dictionary<string, object>();
            }
            ValidateParameters(initParams);
            handler.DomainName = (string)initParams[DomainName.Name];
            handler.AuthId = (string)initParams[AuthId.Name];
            handler.AuthPassword = (string)initParams[AuthPassword.Name];
            return handler;
        }

        private void ValidateParameters(IReadOnlyDictionary<string, object> parameters)
        {
            foreach (ParameterDetail detail in Params.Where(x => x.IsRequired))
            {
                if (!parameters.ContainsKey(detail.Name))
                {
                    throw new KeyNotFoundException($"Missing required parameter [{detail.Name}]");
                }
            }
        }
    }
}