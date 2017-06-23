using ACMESharp.Providers.ClouDNS.Results;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;

namespace ACMESharp.Providers.ClouDNS
{
    public class ClouDNSHelper
    {
        private string _authId;
        private string _authPassword;
        private string _domainName;

        private const string _ttl = "3600";
        private const string _type = "TXT";

        /* private const string ListZonesUrl = baseUrl + "list-zones.json?auth-id={0}&auth-password={1}&domain={2}&page={3}&rows-per-page={4}"; */
        private const string baseUrl = "https://api.cloudns.net/dns/";
        private const string GetZoneInfoUrl = baseUrl + "get-zone-info.json?auth-id={0}&auth-password={1}&domain-name={2}";
        private const string CreateRecordUrl = baseUrl + "add-record.json?auth-id={0}&auth-password={1}&domain-name={2}&record-type={3}&host={4}&record={5}&ttl={6}";
        private const string ListRecordsUrl = baseUrl + "records.json?auth-id={0}&auth-password={1}&domain-name={2}&host={3}&type={4}";
        private const string DeleteRecordUrl = baseUrl + "delete-record.json?auth-id={0}&auth-password={1}&domain-name={2}&record-id={3}";
        private const string UpdateRecordUrl = baseUrl + "mod-record.json?auth-id={0}&auth-password={1}&domain-name={2}&record-id={3}&host={4}&record={5}&ttl={6}";

        public ClouDNSHelper(string AuthId, string AuthPassword, string DomainName)
        {
            _authId = AuthId;
            _authPassword = AuthPassword;
            _domainName = DomainName;
        }

        private void GetZoneInfo()
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, string.Format(GetZoneInfoUrl, _authId, _authPassword, _domainName));
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                ZoneRecord resultObj = JsonConvert.DeserializeObject<ZoneRecord>(content);
                Debug.WriteLine(string.Format("Zone is {0}, type is {1}", resultObj.Name, resultObj.Type));
            }
            else
            {
                throw new Exception($"Could not get DNS records for zone {_domainName}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        private DnsRecord GetDnsRecord(string host)
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, string.Format(ListRecordsUrl, _authId, _authPassword, _domainName, host, _type));
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine(content);
                var res = JsonConvert.DeserializeObject<Dictionary<string, DnsRecord>>(content); ;
                foreach (var r in res)
                {
                    Debug.WriteLine("Id = {0}, Host = {1}", r.Key, r.Value.Host);
                    if (r.Value.Host == host)
                    {
                        return r.Value;
                    }
                }
                return null;
            }
            else
            {
                throw new Exception($"Could not get DNS records for zone {_domainName}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        public void AddDnsRecord(string host, string recordValue)
        {
            HttpClient client = new HttpClient();
            string fullUrl = string.Format(CreateRecordUrl, _authId, _authPassword, _domainName, _type, host, recordValue, _ttl);
            Debug.WriteLine(fullUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine(content);
            }
            else
            {
                throw new Exception($"Could not add DNS records for zone {_domainName}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        public void UpdateDnsRecord(string recordId, string host, string recordValue)
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, string.Format(UpdateRecordUrl, _authId, _authPassword, _domainName, recordId, host, recordValue, _ttl));
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine(content);
            }
            else
            {
                throw new Exception($"Could not add DNS records for zone {_domainName}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        public void DeleteDnsRecord(string host)
        {
            DnsRecord rec = GetDnsRecord(host);
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, string.Format(DeleteRecordUrl, _authId, _authPassword, _domainName, rec.Id));
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.WriteLine(content);
            }
            else
            {
                throw new Exception($"Could not delete DNS records for zone {_domainName}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        public void AddOrUpdateDnsRecord(string host, string recordValue)
        {
            DnsRecord rec = GetDnsRecord(host);
            if (rec != null)
            {
                UpdateDnsRecord(rec.Id, host, recordValue);
            }
            else
            {
                AddDnsRecord(host, recordValue);
            }
        }
    }
}
