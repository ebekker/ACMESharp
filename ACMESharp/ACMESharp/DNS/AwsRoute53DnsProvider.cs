using System.Collections.Generic;

namespace ACMESharp.DNS
{
    public class AwsRoute53DnsProvider : XXXIDnsProvider
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
            var dnsValuesJoined = string.Join("\" \"", dnsValues);
            var rrset = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = 30,
                Name = dnsName,
                Type = Amazon.Route53.RRType.TXT,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(
                            $"\"{dnsValuesJoined}\"")
                }
            };

            EditR53Record(rrset);
        }

        public void EditARecord(string dnsName, string dnsValue)
        {
            var rrset = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = 30,
                Name = dnsName,
                Type = Amazon.Route53.RRType.A,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(dnsValue)
                }
            };

            EditR53Record(rrset);
        }

        public void EditCnameRecord(string dnsName, string dnsValue)
        {
            var rrset = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = 30,
                Name = dnsName,
                Type = Amazon.Route53.RRType.CNAME,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(dnsValue)
                }
            };

            EditR53Record(rrset);
        }

        private void EditR53Record(Amazon.Route53.Model.ResourceRecordSet rrset)
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
                            ResourceRecordSet = rrset
                        }
                    }
                }
            };
            var rrResp = r53.ChangeResourceRecordSets(rrRequ);
        }
    }
}
