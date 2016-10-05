using Ovh.Api;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.Providers.OVH
{
    internal class OvhHelper
    {
        private readonly Client _client;

        private string _zone;

        private string _subDomain;
        
        public OvhHelper(string endpoint, string applicationKey, string applicationSecret, string consumerKey)
        {
            _client = new Client(endpoint, applicationKey, applicationSecret, consumerKey);
        }

        private void SetZoneAndSubDomain(string recordName)
        {
            var parts = recordName.Split('.');
            _zone = string.Join(".", parts.Skip(parts.Length - 2));
            _subDomain = string.Join(".", parts.Take(parts.Length - 2));
        }

        internal void AddOrUpdateDnsRecord(string recordName, string value)
        {
            SetZoneAndSubDomain(recordName);

            var recordsId = GetRecordsId();
            if (recordsId.Any())
            {
                UpdateRecords(recordsId, value);
            }
            else
            {
                AddRecords(value);
            }
        }

        internal void DeleteDnsRecord(string recordName)
        {
            SetZoneAndSubDomain(recordName);

            var recordsId = GetRecordsId();
            if (recordsId.Any())
            {
                DeleteRecords(recordsId);
            }
        }

        private long[] GetRecordsId()
        {
            return _client.Get<long[]>(string.Format("/domain/zone/{0}/record?fieldType=TXT&subDomain={1}", _zone, _subDomain));
        }

        private void AddRecords(string value)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            payload.Add("fieldType", "TXT");
            payload.Add("subDomain", _subDomain);
            payload.Add("target", value);

            _client.Post(string.Format("/domain/zone/{0}/record", _zone), payload);
        }

        private void UpdateRecords(long[] recordsId, string value)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            payload.Add("target", value);

            foreach (var id in recordsId)
            {
                _client.Put(string.Format("/domain/zone/{0}/record/{1}", _zone, id), payload);
            }
        }

        private void DeleteRecords(long[] recordsId)
        {
            foreach (var id in recordsId)
            {
                var res = _client.Delete(string.Format("/domain/zone/{0}/record/{1}", _zone, id));
            }
        }

    }
}
