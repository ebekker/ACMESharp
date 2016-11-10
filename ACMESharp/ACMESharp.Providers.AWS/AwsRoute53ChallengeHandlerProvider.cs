using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using ACMESharp.Ext;

namespace ACMESharp.Providers.AWS
{
    [ChallengeHandlerProvider("aws-route53", ChallengeTypeKind.DNS,
        Aliases = new[] { "awsRoute53", "route53", "awsR53" },
        Label = "AWS Route 53",
        Description = "Provider for handling Challenges that manages" +
                      " DNS entries hosted in an AWS Route 53 zone.")]
    public class AwsRoute53ChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail HOSTED_ZONE_ID = new ParameterDetail(
                nameof(AwsRoute53ChallengeHandler.HostedZoneId),
                ParameterType.TEXT, isRequired: true, label: "Hosted Zone ID",
                desc: "The ID of Route 53 Zone to operate against");

        public static readonly ParameterDetail RR_TYPE = new ParameterDetail(
                nameof(AwsRoute53ChallengeHandler.ResourceRecordType),
                ParameterType.TEXT, label: "Resource Record Type",
                desc: "Qualifies the type of RR that should be configured (TXT, A, CNAME)");

        public static readonly ParameterDetail RR_TTL = new ParameterDetail(
                nameof(AwsRoute53ChallengeHandler.ResourceRecordTtl),
                ParameterType.TEXT, label: "Resource Record TTL",
                desc: "Specifies the time-to-live (TTL) in seconds of the RR (defaults to 300)");

        static readonly ParameterDetail[] PARAMS =
        {
            HOSTED_ZONE_ID,
            RR_TYPE,
            RR_TTL,

            AwsCommonParams.ACCESS_KEY_ID,
            AwsCommonParams.SECRET_ACCESS_KEY,
            AwsCommonParams.SESSION_TOKEN,

            AwsCommonParams.PROFILE_NAME,
            AwsCommonParams.PROFILE_LOCATION,

            AwsCommonParams.IAM_ROLE,

            AwsCommonParams.REGION,
        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public bool IsSupported(Challenge c)
        {
            return c is DnsChallenge;
        }

        public IChallengeHandler GetHandler(Challenge c, IReadOnlyDictionary<string, object> initParams)
        {
            var h = new AwsRoute53ChallengeHandler();

            if (initParams == null)
                initParams = new Dictionary<string, object>();

            // Required params
            if (!initParams.ContainsKey(HOSTED_ZONE_ID.Name))
                throw new KeyNotFoundException($"missing required parameter [{HOSTED_ZONE_ID.Name}]");
            h.HostedZoneId = (string) initParams[HOSTED_ZONE_ID.Name];

            // Optional params
            if (initParams.ContainsKey(RR_TYPE.Name))
                h.ResourceRecordType = (string)initParams[RR_TYPE.Name];
            if (initParams.ContainsKey(RR_TTL.Name))
                h.ResourceRecordTtl = (int)initParams[RR_TTL.Name];

            // Process the common params
            h.CommonParams.InitParams(initParams);

            return h;
        }
    }
}
