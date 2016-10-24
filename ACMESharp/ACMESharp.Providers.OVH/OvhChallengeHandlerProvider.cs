using ACMESharp.ACME;
using ACMESharp.Ext;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.Providers.OVH
{
    [ChallengeHandlerProvider("OVH", ChallengeTypeKind.DNS,
        Label = "OVH",
        Description = "Provider for handling Challenges that manages DNS entries hosted in an OVH zone.")]
    public class OvhChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail DomainName =
            new ParameterDetail(nameof(OvhChallengeHandler.DomainName), ParameterType.TEXT, isRequired: true,
                label: "Domain name", desc: "The domain name to operate against.");

        public static readonly ParameterDetail Endpoint =
            new ParameterDetail(nameof(OvhChallengeHandler.Endpoint), ParameterType.TEXT, isRequired: true,
                label: "API endpoint", desc: "API endpoint to use. Valid values in \"Endpoints\"");

        public static readonly ParameterDetail ApplicationKey =
            new ParameterDetail(nameof(OvhChallengeHandler.ApplicationKey), ParameterType.TEXT, isRequired: true,
                label: "Application key", desc: "Application key as provided by OVH");

        public static readonly ParameterDetail ApplicationSecret =
            new ParameterDetail(nameof(OvhChallengeHandler.ApplicationSecret), ParameterType.TEXT, isRequired: true,
                label: "Application secret key", desc: "Application secret key as provided by OVH");

        public static readonly ParameterDetail ConsumerKey =
            new ParameterDetail(nameof(OvhChallengeHandler.ConsumerKey), ParameterType.TEXT, isRequired: true,
                label: "Consumer Key", desc: "User token as provided by OVH");

        private static readonly ParameterDetail[] Params =
        {
            DomainName,
            Endpoint  ,
            ApplicationKey ,
            ApplicationSecret,
            ConsumerKey
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
            var handler = new OvhChallengeHandler();
            if (initParams == null)
            {
                initParams = new Dictionary<string, object>();
            }
            ValidateParameters(initParams);
            handler.DomainName = (string) initParams[DomainName.Name];
            handler.Endpoint = (string) initParams[Endpoint.Name];
            handler.ApplicationKey = (string) initParams[ApplicationKey.Name];
            handler.ApplicationSecret = (string) initParams[ApplicationSecret.Name];
            handler.ConsumerKey = (string) initParams[ConsumerKey.Name];
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
