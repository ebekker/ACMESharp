using ACMESharp.ACME;
using ACMESharp.Ext;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.Providers.CloudFlare
{
    [ChallengeHandlerProvider(
        "CloudFlare",
        ChallengeTypeKind.DNS,
        Label = "CloudFlare DNS",
        Description = "Provider for handling challenges that manages DNS entries hosted in a CloudFlare zone")]
    public class CloudFlareChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail DomainName =
            new ParameterDetail(nameof(CloudFlareChallengeHandler.DomainName), ParameterType.TEXT, isRequired: true,
                label: "Domain name", desc: "The domain name to operate against.");

        public static readonly ParameterDetail EmailAddress =
            new ParameterDetail(nameof(CloudFlareChallengeHandler.EmailAddress), ParameterType.TEXT, isRequired: true,
                label: "Email address", desc: "The email address of the CloudFlare account to operate against");

        public static readonly ParameterDetail AuthKey =
            new ParameterDetail(nameof(CloudFlareChallengeHandler.AuthKey), ParameterType.TEXT, isRequired: true,
                label: "Authentication Key", desc: "The authentication key of the CloudFlare account to operate against");

        private static readonly ParameterDetail[] Params =
        {
            DomainName,
            EmailAddress,
            AuthKey
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
            var handler = new CloudFlareChallengeHandler();
            if (initParams == null)
            {
                initParams = new Dictionary<string, object>();
            }
            ValidateParameters(initParams);
            handler.DomainName = (string)initParams[DomainName.Name];
            handler.EmailAddress = (string)initParams[EmailAddress.Name];
            handler.AuthKey = (string)initParams[AuthKey.Name];
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