using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace ACMESharp.ACME.Providers
{
    public class DnsMadeEasyChallengeHandler : IChallengeHandler
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public bool Staging { get; set; }

        public bool IsDisposed
        {
            get; private set;
        }

        public void CleanUp(Challenge c)
        {
            var dnsChallenge = (DnsChallenge)c;
            var domainDetails = getDomainId(dnsChallenge);

            var records = managedPath + domainDetails.DomainId + "/records";
            CleanUp(dnsChallenge, domainDetails, records);
        }

        private void CleanUp(DnsChallenge dnsChallenge, DomainDetails domainDetails, string records)
        {
            string recordId = getRecordId(dnsChallenge, domainDetails, records);

            if (!string.IsNullOrEmpty(recordId))
            {
                var wr = createRequest(records + "/" + recordId);
                wr.Method = "DELETE";

                using (var response = wr.GetResponse())
                { }
            }
        }

        private string getRecordId(DnsChallenge dnsChallenge, DomainDetails domainDetails, string records)
        {
            var wr = createRequest(records);
            wr.Method = "GET";

            var recordNameToFind = dnsChallenge.RecordName.Replace("." + domainDetails.DomainName, string.Empty);

            using (var response = wr.GetResponse())
            using (var content = new StreamReader(response.GetResponseStream()))
            {
                var resp = content.ReadToEnd();
                var respObject = JsonConvert.DeserializeObject<DomainResponseCollection>(resp).data;

                var record = respObject.FirstOrDefault(a => a.name == recordNameToFind);
                if (record != null)
                    return record.id;
            }

            return null;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        private readonly string managedPath = "dns/managed/";
        private readonly string nameQuery = "name?domainname=";

        public void Handle(Challenge c)
        {
            var dnsChallenge = (DnsChallenge)c;
            var domainDetails = getDomainId(dnsChallenge);

            var records = managedPath + domainDetails.DomainId + "/records";

            CleanUp(dnsChallenge, domainDetails, records);

            var recordNameToAdd = dnsChallenge.RecordName.Replace("." + domainDetails.DomainName, string.Empty);

            var wr = createRequest(records);
            wr.Method = "POST";

            using (var request = new StreamWriter(wr.GetRequestStream()))
            {
                var requestObject = new DomainRequest()
                {
                    name = recordNameToAdd,
                    value = dnsChallenge.RecordValue,
                    ttl = 600,
                    type = "TXT"
                };

                var json = JsonConvert.SerializeObject(requestObject);

                request.Write(json);
            }

            using (var response = wr.GetResponse())
            using (var content = new StreamReader(response.GetResponseStream()))
            {
                var resp = content.ReadToEnd();
                var respObject = JsonConvert.DeserializeObject<DomainRequest>(resp);
                if (string.IsNullOrEmpty(respObject.id))
                {
                    //Failed
                }
            }
        }

        DomainDetails getDomainId(DnsChallenge dnsChallenge)
        {
            var startIndex = dnsChallenge.RecordName.IndexOf(".") + 1;

            return getDomainId(dnsChallenge, startIndex);
        }

        DomainDetails getDomainId(DnsChallenge dnsChallenge, int startIndex)
        {
            try
            {
                var domainName = dnsChallenge.RecordName.Substring(startIndex);

                var wr = createRequest(managedPath + nameQuery + domainName);
                using (var response = wr.GetResponse())
                {
                    using (var content = new StreamReader(response.GetResponseStream()))
                    {
                        var dr = JsonConvert.DeserializeObject<DomainResponse>(content.ReadToEnd());
                        return new DomainDetails() { DomainId = dr.id, DomainName = domainName };
                    }
                }
            }
            catch (WebException wex)
            {
                startIndex = dnsChallenge.RecordName.IndexOf(".", startIndex) + 1;
                return getDomainId(dnsChallenge, startIndex);
            }
        }

        class DomainDetails
        {
            public string DomainName { get; set; }
            public string DomainId { get; set; }
        }

        class DomainResponseCollection
        {
            public DomainRequest[] data { get; set; }
        }

        class DomainResponse
        {
            public string id { get; set; }
            public string name { get; set; }
            public int ttl { get; set; }
        }

        class DomainRequest : DomainResponse
        {
            public string type { get; set; }
            public string value { get; set; }
        }

        WebRequest createRequest(string url)
        {
            if (Staging)
                url = "https://api.sandbox.dnsmadeeasy.com/V2.0/" + url;
            else
                url = "https://api.dnsmadeeasy.com/V2.0/" + url;

            var currentDate = DateTime.UtcNow.ToString("r");
            var hash = SHA1Hash(currentDate, SecretKey);

            var wr = WebRequest.Create(url);

            wr.Headers.Add("x-dnsme-apiKey", ApiKey);
            wr.Headers.Add("x-dnsme-requestDate", currentDate);
            wr.Headers.Add("x-dnsme-hmac", hash);

            return wr;
        }

        static string SHA1Hash(string input, string key)
        {
            using (var shaProvider = new HMACSHA1(new UTF8Encoding().GetBytes(key)))
            {
                return Hash(shaProvider, input);
            }
        }

        static string Hash(HashAlgorithm a, string input)
        {
            var bytes = a.ComputeHash(new UTF8Encoding().GetBytes(input));
            return ToHex(bytes);
        }

        static string ToHex(byte[] value)
        {
            var stringBuilder = new StringBuilder();
            if (value != null)
            {
                foreach (var b in value)
                {
                    stringBuilder.Append(HexStringTable[b]);
                }
            }

            return stringBuilder.ToString();
        }

        private static readonly string[] HexStringTable =
        {
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0a", "0b", "0c", "0d", "0e", "0f",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1a", "1b", "1c", "1d", "1e", "1f",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2a", "2b", "2c", "2d", "2e", "2f",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3a", "3b", "3c", "3d", "3e", "3f",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4a", "4b", "4c", "4d", "4e", "4f",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5a", "5b", "5c", "5d", "5e", "5f",
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6a", "6b", "6c", "6d", "6e", "6f",
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7a", "7b", "7c", "7d", "7e", "7f",
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8a", "8b", "8c", "8d", "8e", "8f",
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9a", "9b", "9c", "9d", "9e", "9f",
            "a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "aa", "ab", "ac", "ad", "ae", "af",
            "b0", "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "ba", "bb", "bc", "bd", "be", "bf",
            "c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9", "ca", "cb", "cc", "cd", "ce", "cf",
            "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8", "d9", "da", "db", "dc", "dd", "de", "df",
            "e0", "e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", "ea", "eb", "ec", "ed", "ee", "ef",
            "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "fa", "fb", "fc", "fd", "fe", "ff"
        };
    }
}
