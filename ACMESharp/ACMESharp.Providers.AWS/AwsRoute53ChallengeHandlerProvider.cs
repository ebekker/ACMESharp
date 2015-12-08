using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ACMESharp.ACME;
using ACMESharp.Ext;

namespace ACMESharp.Providers.AWS
{
    public class AwsRoute53ChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail HOSTED_ZONE_ID = new ParameterDetail(
                nameof(AwsRoute53ChallengeHandler.HostedZoneId),
                ParameterType.TEXT, isRequired: true, label: "Hosted Zone ID",
                desc:"The ID of Route 53 Zone to operate against");

        public static readonly ParameterDetail RR_TYPE = new ParameterDetail(
                nameof(AwsRoute53ChallengeHandler.ResourceRecordType),
                ParameterType.TEXT, isRequired: true, label: "Resource Record Type",
                desc: "Qualifies the type of RR that should be configured (TXT, A, CNAME)");

        public static readonly ParameterDetail RR_TTL = new ParameterDetail(
                nameof(AwsRoute53ChallengeHandler.ResourceRecordTtl),
                ParameterType.TEXT, isRequired: true, label: "Resource Record TTL",
                desc: "Specifies the time-to-live (TTL) in seconds of the RR (defaults to 300)");

        static readonly ParameterDetail[] PARAMS =
        {
            HOSTED_ZONE_ID,
            RR_TYPE,
            RR_TTL,
            AwsCommonParams.ACCESS_KEY_ID,
            AwsCommonParams.SECRET_ACCESS_KEY,
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

        public IChallengeHandler GetHandler(Challenge c, IDictionary<string, object> initParams)
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

    public class AwsRoute53ChallengeHandler : IChallengeHandler
    {
        #region -- Properties --

        public string HostedZoneId
        { get; set; }

        public string ResourceRecordType
        { get; set; } = "TXT";

        public int ResourceRecordTtl
        { get; set; } = 300;

        public AwsCommonParams CommonParams
        { get; set; } = new AwsCommonParams();

        public bool IsDisposed
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public void Handle(Challenge c)
        {
            AssertNotDisposed();
            EditDns((DnsChallenge) c, false);
        }

        public void CleanUp(Challenge c)
        {
            AssertNotDisposed();
            EditDns((DnsChallenge)c, true);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("AWS Challenge Handler is disposed");
        }

        private void EditDns(DnsChallenge dnsChallenge, bool delete)
        {
            var dnsName = dnsChallenge.RecordName;
            var dnsValue = Regex.Replace(dnsChallenge.RecordValue, "\\s", "");
            var dnsValues = Regex.Replace(dnsValue, "(.{100,100})", "$1\n").Split('\n');

            var r53 = new Route53Helper
            {
                HostedZoneId = HostedZoneId,
                ResourceRecordTtl = ResourceRecordTtl,
                CommonParams = CommonParams,
            };

            if (ResourceRecordType == "TXT")
                r53.EditTxtRecord(dnsName, dnsValues, delete);
            else
                throw new NotImplementedException($"RR type of [{ResourceRecordType}] not implemented");
        }

        #endregion -- Methods --
    }
}
