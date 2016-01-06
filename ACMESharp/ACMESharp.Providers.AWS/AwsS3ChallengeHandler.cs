using System;
using System.Net;
using ACMESharp.ACME;
using Amazon.S3;
using Amazon.S3.Model;

namespace ACMESharp.Providers.AWS
{
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
                commonParams.ResolveCredentials(),
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
                    CommonParams.ResolveCredentials(),
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