using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using ACMESharp.Ext;

namespace ACMESharp.Providers.AWS
{
    [ChallengeHandlerProvider("aws-s3", ChallengeTypeKind.HTTP,
        Aliases = new[] { "awsS3", "s3" },
        Label = "AWS S3",
        Description = "Provider for handling Challenges that manages" +
                      " file entries hosted in an AWS S3 bucket.  The" +
                      " handler depends on an appropriate DNS mapping" +
                      " to be configured externally that resolves to" +
                      " the target bucket.")]
    public class AwsS3ChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail BUCKET_NAME = new ParameterDetail(
                nameof(AwsS3ChallengeHandler.BucketName),
                ParameterType.TEXT, isRequired: true, label: "Bucket Name",
                desc: "Name of the S3 Bucket where files will be managed");
        public static readonly ParameterDetail CONTENT_TYPE = new ParameterDetail(
                nameof(AwsS3ChallengeHandler.ContentType),
                ParameterType.TEXT, label: "MIME Content Type",
                desc: "Content Type to associate with the file object");
        public static readonly ParameterDetail CANNED_ACL = new ParameterDetail(
                nameof(AwsS3ChallengeHandler.CannedAcl),
                ParameterType.TEXT, label: "Canned ACL",
                desc: "Name of a pre-defined access policy that will be applied to the file object");

        static readonly ParameterDetail[] PARAMS =
        {
            BUCKET_NAME,

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
            return c is HttpChallenge;
        }


        public IChallengeHandler GetHandler(Challenge c, IReadOnlyDictionary<string, object> initParams)
        {
            var h = new AwsS3ChallengeHandler();

            if (initParams == null)
                initParams = new Dictionary<string, object>();

            // Required params
            if (!initParams.ContainsKey(BUCKET_NAME.Name))
                throw new KeyNotFoundException($"missing required parameter [{BUCKET_NAME.Name}]");
            h.BucketName = (string)initParams[BUCKET_NAME.Name];

            // Optional params
            if (initParams.ContainsKey(CONTENT_TYPE.Name))
                h.ContentType = (string)initParams[CONTENT_TYPE.Name];
            if (initParams.ContainsKey(CANNED_ACL.Name))
                h.CannedAcl = (string)initParams[CANNED_ACL.Name];

            // Process the common params
            h.CommonParams.InitParams(initParams);

            return h;
        }
    }
}
