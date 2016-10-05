using ACMESharp.ACME;
using ACMESharp.Providers.OVH;
using Newtonsoft.Json;
using Ovh.Api;
using System;
using System.Collections.Generic;
using System.IO;

namespace ACMESharp.Providers.OVH_test
{
    class Program
    {
        private const string ConfigFilename = "OVH_test.json";

        private class Config
        {
            public string DomainName { get; set; }

            public string Endpoint { get; set; }

            public string ApplicationKey { get; set; }

            public string ApplicationSecret { get; set; }

            public string ConsumerKey { get; set; }

        }

        private static Config config;

        private static void LoadConfig()
        {
            if (File.Exists(ConfigFilename))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilename));
                }
                catch
                {
                    config = new Config();
                }
            }
            else
            {
                config = new Config();
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(ConfigFilename, JsonConvert.SerializeObject(config));
        }

        static void Main(string[] args)
        {
            LoadConfig();

            Console.WriteLine("Test OVH provider");

            if (string.IsNullOrWhiteSpace(config.Endpoint))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Enter the Application Secret: ");
                config.Endpoint = Console.ReadLine();
                Console.ResetColor();

                SaveConfig();
            }

            if (string.IsNullOrWhiteSpace(config.ApplicationKey) || string.IsNullOrWhiteSpace(config.ApplicationSecret))
            {
                string createUrl = OvhChallengeHandler.GetCreateApiUrl(config.Endpoint);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Go to url {0} to create application api keys", createUrl);
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Enter the Application Key: ");
                config.ApplicationKey = Console.ReadLine();

                Console.Write("Enter the Application Secret: ");
                config.ApplicationSecret = Console.ReadLine();
                Console.ResetColor();

                SaveConfig();
            }

            bool needRequestConsumerKey = string.IsNullOrWhiteSpace(config.ConsumerKey);
            if (!needRequestConsumerKey)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Force Request Consumer Key (y/n): ");
                var read = Console.ReadLine();
                Console.ResetColor();

                if (!string.IsNullOrWhiteSpace(read) && read.ToLower() == "y")
                {
                    needRequestConsumerKey = true;
                    config.ConsumerKey = null;
                    SaveConfig();
                }
            }
            if (needRequestConsumerKey)
            {
                CredentialRequestResult requestConsumer = OvhChallengeHandler.RequestConsumerKey(config.Endpoint, config.ApplicationKey, config.ApplicationSecret,
                    null, "https://eu.api.ovh.com/");
                config.ConsumerKey = requestConsumer.ConsumerKey;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Go to url {0} to validate your application credentials", requestConsumer.ValidationUrl);
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Press enter when ready");
                Console.ReadLine();
                Console.ResetColor();

                SaveConfig();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Enter the Domain Name (default {0}): ", config.DomainName);
            string domainName = Console.ReadLine();
            if (!string.IsNullOrEmpty(domainName))
            {
                config.DomainName = domainName;
            }
            Console.ResetColor();

            SaveConfig();

            var initParams = new Dictionary<string, object>()
            {
                {"DomainName", config.DomainName },
                {"Endpoint", config.Endpoint },
                {"ApplicationKey", config.ApplicationKey },
                {"ApplicationSecret", config.ApplicationSecret },
                {"ConsumerKey", config.ConsumerKey }
            };

            try
            {
                Console.WriteLine("Get provider");
                var provider = new OvhChallengeHandlerProvider();

                var r = new Random();
                var bn = new byte[4];
                var bv = new byte[10];
                r.NextBytes(bn);
                r.NextBytes(bv);
                var rn = BitConverter.ToString(bn);
                var rv = BitConverter.ToString(bv);

                var dnsChallenge = new DnsChallenge(AcmeProtocol.CHALLENGE_TYPE_DNS, new DnsChallengeAnswer())
                {
                    Token = "FOOBAR",
                    RecordName = $"{rn}.{config.DomainName}",
                    RecordValue = rv,
                };

                Console.WriteLine("Get handler");
                using (var handler = (OvhChallengeHandler) provider.GetHandler(dnsChallenge, initParams))
                {
                    Console.WriteLine("Handle");
                    handler.Handle(dnsChallenge);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Press enter when ready");
                    Console.ResetColor();
                    Console.ReadLine();

                    Console.WriteLine("CleanUp");
                    handler.CleanUp(dnsChallenge);
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace.ToString());
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Press enter to close");
            Console.ResetColor();
            Console.ReadLine();
        }
    }
}
