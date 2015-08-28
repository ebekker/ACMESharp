using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.DNS
{
    public class AwsRoute53DnsProvider : IDnsProvider
    {
        public string HostedZoneId
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

        public void EditTxtRecord(string dnsName, IEnumerable<string> dnsValues)
        {
            var r53 = new Amazon.Route53.AmazonRoute53Client(
                    AccessKeyId, SecretAccessKey, RegionEndpoint);

            var rrRequ = new Amazon.Route53.Model.ChangeResourceRecordSetsRequest
            {
                HostedZoneId = HostedZoneId,
                ChangeBatch = new Amazon.Route53.Model.ChangeBatch
                {
                    Changes = new List<Amazon.Route53.Model.Change>
                    {
                        new Amazon.Route53.Model.Change
                        {
                            Action = Amazon.Route53.ChangeAction.UPSERT,
                            ResourceRecordSet = new Amazon.Route53.Model.ResourceRecordSet
                            {
                                TTL = 60,
                                Name = dnsName,
                                Type = Amazon.Route53.RRType.TXT,
                                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>(
                                        dnsValues.Select(x =>
                                                new Amazon.Route53.Model.ResourceRecord($"\"{x}\"")))
                            }
                        }
                    }
                }
            };
            var rrResp = r53.ChangeResourceRecordSets(rrRequ);
        }
    }
}
