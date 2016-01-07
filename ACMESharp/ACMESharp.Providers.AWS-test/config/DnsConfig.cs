using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.AWS.config
{
    public class DnsConfig : Dictionary<string, object>
    {
        [Obsolete]
        public string DefaultDomain
        {
            get { return (string)this[nameof(DefaultDomain)]; }
        }

        public string HostedZoneId
        {
            get { return (string)this[AwsRoute53ChallengeHandlerProvider.HOSTED_ZONE_ID.Name]; }
        }

        public string AccessKeyId
        {
            get { return (string)this[AwsCommonParams.ACCESS_KEY_ID.Name]; }
        }

        public string SecretAccessKey
        {
            get { return (string)this[AwsCommonParams.SECRET_ACCESS_KEY.Name]; }
        }

        public string Region
        {
            get { return (string)this[AwsCommonParams.REGION.Name]; }
        }
    }
}
