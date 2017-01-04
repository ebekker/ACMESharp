using System;
using System.IO;
using ACMESharp.DNS;

namespace ACMESharp.WebServer
{
    public class AwsS3WebServerProvider : XXXIWebServerProvider
    {
        public string BucketName
        { get; set; }

        public string AccessKeyId
        { get; set; }

        public string SecretAccessKey
        { get; set; }

        public string Region
        {
            get { return RegionEndpoint == null ? null : RegionEndpoint.SystemName; }
            set { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value); }
        }

        public Amazon.RegionEndpoint RegionEndpoint
        { get; set; } = Amazon.RegionEndpoint.USEast1;

        public XXXIDnsProvider DnsProvider
        { get; set; }

        public string DnsCnameTarget
        { get; set; }

        public void UploadFile(Uri fileUrl, Stream s)
        {
            var filePath = fileUrl.AbsolutePath;
            // We need to strip off any leading '/' in the path or
            // else it creates a path with an empty leading segment
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);

            using (var s3 = new Amazon.S3.AmazonS3Client(
                    AccessKeyId, SecretAccessKey, RegionEndpoint))
            {
                var s3Requ = new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = filePath,
                    InputStream = s,
                    AutoCloseStream = false,
                };
                var s3Resp = s3.PutObject(s3Requ);
            }

            if (DnsProvider != null)
            {
                var hostname = fileUrl.Host;
                DnsProvider.EditCnameRecord(hostname, DnsCnameTarget);
            }
            else
            {
                // TODO:  do nothing for now
                // Throw Exception???
            }
        }
    }
}
