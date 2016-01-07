using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.AWS.config
{
    public class HttpConfig : Dictionary<string, object>
    {
        public string BucketName
        {
            get { return (string)this[AwsS3ChallengeHandlerProvider.BUCKET_NAME.Name]; }
        }

        public string ContentType
        {
            get { return (string)this[AwsS3ChallengeHandlerProvider.CONTENT_TYPE.Name]; }
        }

        public string CannedAcl
        {
            get { return (string)this[AwsS3ChallengeHandlerProvider.CANNED_ACL.Name]; }
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
