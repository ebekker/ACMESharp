using ACMESharp.Providers.CloudFlare.Results;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ACMESharp.Providers.CloudFlare
{
    /// <summary>
    /// Helper class to interface with the CloudFlare API endpoint.
    /// </summary>
    /// <remarks>
    /// See <see cref="https://api.cloudflare.com/#getting-started-endpoints"/>
    /// for more details.
    /// </remarks>
    public class CloudFlareHelper
    {
        private readonly string _authKey;
        private readonly string _emailAddress;
        private readonly string _domainName;
        private const string BaseUrl = "https://api.cloudflare.com/client/v4/";
        private const string ListZonesUrl = BaseUrl + "zones";
        private const string CreateRecordUrl = BaseUrl + "zones/{0}/dns_records";
        private const string ListRecordsUrl = BaseUrl + "zones/{0}/dns_records";
        private const string DeleteRecordUrl = BaseUrl + "zones/{0}/dns_records/{1}";
        private const string UpdateRecordUrl = BaseUrl + "zones/{0}/dns_records/{1}";

        public CloudFlareHelper(string authKey, string emailAddress, string domainName)
        {
            _authKey = authKey;
            _emailAddress = emailAddress;
            _domainName = domainName;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-AUTH-KEY", _authKey);
            request.Headers.Add("X-AUTH-EMAIL", _emailAddress);
            return request;
        }

        public void DeleteDnsRecord(string name)
        {
            HttpClient client = new HttpClient();
            var zoneId = GetZoneId();
            var records = GetDnsRecords(zoneId);
            var record = records.FirstOrDefault(x => x.Name == name);
            if (record == null)
            {
                return;
            }
            var request = CreateRequest(HttpMethod.Delete, string.Format(DeleteRecordUrl, zoneId, record.Id));
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                throw new Exception($"Could not delete record {name}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        public void AddOrUpdateDnsRecord(string name, string value)
        {
            var zoneId = GetZoneId();
            var records = GetDnsRecords(zoneId);
            var record = records.FirstOrDefault(x => x.Name == name);
            if (record != null)
            {
                UpdateDnsRecord(zoneId, record, value);
            }
            else
            {
                AddDnsRecord(zoneId, name, value);
            }
        }

        private string GetZoneId()
        {
            List<Zone> zones = new List<Zone>();
            bool finishedPaginating = false;
            int page = 1;
            HttpClient client = new HttpClient();
            while (!finishedPaginating)
            {
                var request = CreateRequest(HttpMethod.Get, $"{ListZonesUrl}?page={page}");
                var result = client.SendAsync(request).GetAwaiter().GetResult();
                if (result.IsSuccessStatusCode)
                {
                    var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var zonesResult = JsonConvert.DeserializeObject<ZoneResult>(content);
                    zones.AddRange(zonesResult.Result);
                    if (zonesResult.ResultInfo.Page == zonesResult.ResultInfo.TotalPages)
                    {
                        finishedPaginating = true;
                    }
                    else
                    {
                        page = page + 1;
                    }
                }
                else
                {
                    throw new Exception(
                            $"Could not retrieve a zone id for domain name {_domainName}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
                }
            }
            var zoneResult = zones.FirstOrDefault(x => x.Name == _domainName);
            if (zoneResult == null)
            {
                throw new Exception($"Could not fine a zone with matching domain name. Provided domain name: {_domainName}");
            }
            return zoneResult.Id;
        }

        private List<DnsRecord> GetDnsRecords(string zoneId)
        {
            List<DnsRecord> records = new List<DnsRecord>();
            bool finishedPaginating = false;
            int page = 1;
            HttpClient client = new HttpClient();
            while (!finishedPaginating)
            {
                var request = CreateRequest(HttpMethod.Get, $"{string.Format(ListRecordsUrl, zoneId)}?page={page}");
                var result = client.SendAsync(request).GetAwaiter().GetResult();
                if (result.IsSuccessStatusCode)
                {
                    var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var dnsResult = JsonConvert.DeserializeObject<DnsResult>(content);
                    records.AddRange(dnsResult.Result);
                    if (dnsResult.ResultInfo.Page == dnsResult.ResultInfo.TotalPages)
                    {
                        finishedPaginating = true;
                    }
                    else
                    {
                        page = page + 1;
                    }
                }
                else
                {
                    throw new Exception($"Could not get DNS records for zone {zoneId}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
                }
            }
            return records;
        }

        private void AddDnsRecord(string zoneId, string name, string value)
        {
            HttpClient client = new HttpClient();
            var request = CreateRequest(HttpMethod.Post, string.Format(CreateRecordUrl, zoneId));
            request.Content = new StringContent(
                $"{{\"type\": \"TXT\", \"name\": \"{name}\", \"content\": \"{value}\"}}");
            request.Content.Headers.ContentType.MediaType = "application/json";
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Could not add dns record {name} to zone {zoneId}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }

        private void UpdateDnsRecord(string zoneId, DnsRecord record, string value)
        {
            HttpClient client = new HttpClient();
            var request = CreateRequest(HttpMethod.Put, string.Format(UpdateRecordUrl, zoneId, record.Id));
            request.Content = new StringContent($"{{\"type\": \"TXT\", \"name\": \"{record.Name}\", \"content\": \"{value}\"}}");
            request.Content.Headers.ContentType.MediaType = "application/json";
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Could not update dns record {record.Name} to zone {zoneId}. Result: {result.StatusCode} - {result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
            }
        }
    }
}

