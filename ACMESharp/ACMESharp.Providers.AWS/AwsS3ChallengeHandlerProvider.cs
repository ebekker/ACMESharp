using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using ACMESharp.Ext;
using Amazon.S3;
using Amazon.S3.Model;

namespace ACMESharp.Providers.AWS
{
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


        public IChallengeHandler GetHandler(Challenge c, IDictionary<string, object> initParams)
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

    public class AwsS3ChallengeHandler : IChallengeHandler
    {
        #region -- Properties --

        public string BucketName
        { get; set; }

        public string ContentType
        { get; set; }

        public string CannedAcl
        {
            get { return S3CannedAcl?.Value; }
            set
            {
                S3CannedAcl = S3CannedACL.FindValue(value);
            }
        }

        public S3CannedACL S3CannedAcl
        { get; set; }

        public AwsCommonParams CommonParams
        { get; set; } = new AwsCommonParams();

        public bool IsDisposed
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public void Handle(Challenge c)
        {
            AssertNotDisposed();

            var httpChallenge = (HttpChallenge)c;
            EditFile(httpChallenge, false);
        }

        public void CleanUp(Challenge c)
        {
            AssertNotDisposed();

            var httpChallenge = (HttpChallenge)c;
            EditFile(httpChallenge, true);
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

        public static GetObjectResponse GetFile(AwsCommonParams commonParams,
                string bucketName, string filePath)
        {
            // We need to strip off any leading '/' in the path or
            // else it creates a path with an empty leading segment
            // This also implements behavior consistent with the
            // edit counterpart routine for verification purposes
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);

            using (var s3 = new Amazon.S3.AmazonS3Client(
                commonParams.AccessKeyId, commonParams.SecretAccessKey,
                commonParams.RegionEndpoint))
            {
                var s3Requ = new Amazon.S3.Model.GetObjectRequest
                {
                    BucketName = bucketName,
                    //Prefix = filePath,
                    Key = filePath,
                };

                //var s3Resp = s3.ListObjects(s3Requ);
                try
                {
                    var s3Resp = s3.GetObject(s3Requ);

                    return s3Resp;
                }
                catch (AmazonS3Exception ex)
                        when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
        }

        private void EditFile(HttpChallenge httpChallenge, bool delete)
        {
            var filePath = httpChallenge.FilePath;

            // We need to strip off any leading '/' in the path or
            // else it creates a path with an empty leading segment
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);

            using (var s3 = new Amazon.S3.AmazonS3Client(
                CommonParams.AccessKeyId, CommonParams.SecretAccessKey,
                CommonParams.RegionEndpoint))
            {
                if (delete)
                {
                    var s3Requ = new Amazon.S3.Model.DeleteObjectRequest
                    {
                        BucketName = BucketName,
                        Key = filePath,
                    };
                    var s3Resp = s3.DeleteObject(s3Requ);
                }
                else
                {
                    var s3Requ = new Amazon.S3.Model.PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = filePath,
                        ContentBody = httpChallenge.FileContent,
                        ContentType = ContentType,
                        CannedACL = S3CannedAcl,
                    };
                    var s3Resp = s3.PutObject(s3Requ);
                }
            }
        }

        #endregion -- Methods --
    }
}
