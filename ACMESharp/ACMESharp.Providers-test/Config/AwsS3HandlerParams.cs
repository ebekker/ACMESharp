using ACMESharp.Providers.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.AWS.Config
{
    public class AwsS3HandlerParams : BaseParams
    {
        public string BucketName
        {
            get { return Get<string>(AwsS3ChallengeHandlerProvider.BUCKET_NAME.Name); }
        }

        public string ContentType
        {
            get { return Get<string>(AwsS3ChallengeHandlerProvider.CONTENT_TYPE.Name); }
        }

        public string CannedAcl
        {
            get { return Get<string>(AwsS3ChallengeHandlerProvider.CANNED_ACL.Name); }
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
