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
            get { return (string)this[nameof(BucketName)]; }
        }

        public string ContentType
        {
            get { return (string)this[nameof(ContentType)]; }
        }

        public string CannedAcl
        {
            get { return (string)this[nameof(CannedAcl)]; }
        }

        public string AccessKeyId
        {
            get { return (string)this[nameof(AccessKeyId)]; }
        }

        public string SecretAccessKey
        {
            get { return (string)this[nameof(SecretAccessKey)]; }
        }

        public string Region
        {
            get { return (string)this[nameof(Region)]; }
        }
    }
}
