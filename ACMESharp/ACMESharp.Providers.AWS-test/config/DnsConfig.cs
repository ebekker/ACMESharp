using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.AWS.config
{
    public class DnsConfig : Dictionary<string, object>
    {
        public string DefaultDomain
        {
            get { return (string)this[nameof(DefaultDomain)]; }
        }

        public string HostedZoneId
        {
            get { return (string)this[nameof(HostedZoneId)]; }
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
