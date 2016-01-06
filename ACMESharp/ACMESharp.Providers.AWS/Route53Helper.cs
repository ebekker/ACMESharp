using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Route53.Model;

namespace ACMESharp.Providers.AWS
{
    public class Route53Helper
    {
        public string HostedZoneId
        { get; set; }

        public int ResourceRecordTtl
        { get; set; } = 300;

        public AwsCommonParams CommonParams
        { get; set; } = new AwsCommonParams();

        /// <summary>
        /// Returns all records up to 100 at a time, starting with the
        /// one with the optional name and/or type, sorted in lexographical
        /// order by name (with labels reversed) then by type.
        /// </summary>
        public ListResourceRecordSetsResponse GetRecords(
                string startingDnsName, string startingDnsType = null)
        {
            using (var r53 = new Amazon.Route53.AmazonRoute53Client(
                    CommonParams.ResolveCredentials(),
                    CommonParams.RegionEndpoint))
            {
                var rrRequ = new Amazon.Route53.Model.ListResourceRecordSetsRequest
                {
                    HostedZoneId = HostedZoneId,
                    StartRecordName = startingDnsName,
                    StartRecordType = startingDnsType,
                };

                var rrResp = r53.ListResourceRecordSets(rrRequ);

                return rrResp;
            }
        }

        public void EditTxtRecord(string dnsName, IEnumerable<string> dnsValues, bool delete = false)
        {
            var dnsValuesJoined = string.Join("\" \"", dnsValues);
            var rrset = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = ResourceRecordTtl,
                Name = dnsName,
                Type = Amazon.Route53.RRType.TXT,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(
                            $"\"{dnsValuesJoined}\"")
                }
            };

            EditR53Record(rrset, delete);
        }

        public void EditARecord(string dnsName, string dnsValue, bool delete = false)
        {
            var rrset = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = ResourceRecordTtl,
                Name = dnsName,
                Type = Amazon.Route53.RRType.A,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(dnsValue)
                }
            };

            EditR53Record(rrset);
        }

        public void EditCnameRecord(string dnsName, string dnsValue, bool delete = false)
        {
            var rrset = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = ResourceRecordTtl,
                Name = dnsName,
                Type = Amazon.Route53.RRType.CNAME,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(dnsValue)
                }
            };

            EditR53Record(rrset);
        }

        public void EditR53Record(Amazon.Route53.Model.ResourceRecordSet rrset, bool delete = false)
        {
            using (var r53 = new Amazon.Route53.AmazonRoute53Client(
                    CommonParams.ResolveCredentials(),
                    CommonParams.RegionEndpoint))
            {
                var rrRequ = new Amazon.Route53.Model.ChangeResourceRecordSetsRequest
                {
                    HostedZoneId = HostedZoneId,
                    ChangeBatch = new Amazon.Route53.Model.ChangeBatch
                    {
                        Changes = new List<Amazon.Route53.Model.Change>
                        {
                            new Amazon.Route53.Model.Change
                            {
                                Action = delete
                                    ? Amazon.Route53.ChangeAction.DELETE
                                    : Amazon.Route53.ChangeAction.UPSERT,
                                ResourceRecordSet = rrset
                            }
                        }
                    }
                };
                var rrResp = r53.ChangeResourceRecordSets(rrRequ);
            }
        }
    }
}
