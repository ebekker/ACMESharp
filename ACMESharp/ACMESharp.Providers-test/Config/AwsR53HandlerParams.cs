using ACMESharp.Providers.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.AWS.Config
{
    public class AwsR53HandlerParams : BaseParams
    {
        [Obsolete]
        public string DefaultDomain
        {
            get { return (string)this[nameof(DefaultDomain)]; }
        }

        public string HostedZoneId
        {
            get { return Get<string>(AwsRoute53ChallengeHandlerProvider.HOSTED_ZONE_ID.Name); }
            set { this[AwsRoute53ChallengeHandlerProvider.HOSTED_ZONE_ID.Name] = value; }
        }

        public string AccessKeyId
        {
            get { return Get<string>(AwsCommonParams.ACCESS_KEY_ID.Name); }
            set { this[AwsCommonParams.ACCESS_KEY_ID.Name] = value; }
        }

        public string SecretAccessKey
        {
            get { return Get<string>(AwsCommonParams.SECRET_ACCESS_KEY.Name); }
            set { this[AwsCommonParams.SECRET_ACCESS_KEY.Name] = value; }
        }

        public string Region
        {
            get { return Get<string>(AwsCommonParams.REGION.Name); }
            set { this[AwsCommonParams.REGION.Name] = value; }
        }
    }
}
