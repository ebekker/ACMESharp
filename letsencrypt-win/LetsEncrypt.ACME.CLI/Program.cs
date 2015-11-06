using LetsEncrypt.ACME.JOSE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using System.Threading;
using LetsEncrypt.ACME.PKI;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.CLI
{
    class Program
    {
        public static string BaseURI { get; set; } = "https://acme-staging.api.letsencrypt.org/";
        public static string ProductionBaseURI { get; set; } = "https://acme-v01.api.letsencrypt.org/";

        static string UserAgent = "Let's Encrypt Windows Command Line Client";


        static void Main(string[] args)
        {
            Console.WriteLine(UserAgent);

            Console.Write("\nUse production Let's Encrypt server? (Y/N) ");
            if (PromptYesNo())
                BaseURI = ProductionBaseURI;

            //var vaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LetsEncrypt");
            //Console.WriteLine("Vault Path: " + vaultPath);

            Console.WriteLine($"\nACME Server: {BaseURI}");

            using (var signer = new RS256Signer())
            {
                signer.Init();

                using (var client = new AcmeClient(new Uri(BaseURI), new AcmeServerDirectory(), signer))
                {
                    client.Init();
                    Console.WriteLine("\nGetting AcmeServerDirectory");
                    client.GetDirectory(true);

                    //client.UserAgent = UserAgent;

                    Console.WriteLine("Calling Register");
                    var registration = client.Register(new string[] { });

                    Console.Write($"Do you agree to {registration.TosLinkUri}? (Y/N) ");
                    if (!PromptYesNo())
                        return;

                    Console.WriteLine("Updating Registration");
                    client.UpdateRegistration(true, true);

                    Console.WriteLine("Scanning IIS 7 Site Bindings for Hosts (Elevated Permissions Required)");
                    var bindings = GetHostNames();

                    Console.WriteLine("\nIIS Bindings");
                    var count = 1;
                    foreach (var binding in bindings)
                    {
                        Console.WriteLine($" {count}: {binding}");
                        count++;
                    }
                    Console.WriteLine();
                    Console.WriteLine(" A: Cert all bindings (ENCRYPT ALL THE THINGS!)");
                    Console.WriteLine(" Q: Quit");
                    Console.Write("Which binding do you want to get a cert for: ");
                    var response = Console.ReadLine();
                    switch (response.ToLowerInvariant())
                    {
                        case "a":
                            foreach (var hostName in bindings)
                            {
                                Auto(client, hostName.Host, hostName.PhysicalPath);
                            }
                            break;
                        case "q":
                            return;
                        default:
                            var bindingId = 0;
                            if (Int32.TryParse(response, out bindingId))
                            {
                                bindingId--;
                                if (bindingId >= 0 && bindingId < bindings.Count)
                                {
                                    var binding = bindings[bindingId];
                                    Auto(client, binding.Host, binding.PhysicalPath);
                                }
                            }
                            break;
                    }
                }
            }

#if DEBUG
            Console.WriteLine("Press enter to continue.");
            Console.ReadLine();
#endif
        }

        static bool PromptYesNo()
        {
            while (true)
            {
                var response = Console.ReadKey(true);
                if (response.Key == ConsoleKey.Y)
                    return true;
                if (response.Key == ConsoleKey.N)
                    return false;
                Console.WriteLine("Please press Y or N.");
            }
        }

        static List<SiteHost> GetHostNames()
        {
            var result = new List<SiteHost>();
            using (var iisManager = new ServerManager())
            {
                foreach (var site in iisManager.Sites)
                {
                    foreach (var binding in site.Bindings)
                    {
                        if (!String.IsNullOrEmpty(binding.Host))
                            result.Add(new SiteHost() { Host = binding.Host, PhysicalPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath });
                    }
                }
            }
            return result;
        }

        static void Auto(AcmeClient client, string dnsIdentifier, string webRootPath)
        {
            var auth = Authorize(client, dnsIdentifier, webRootPath);
            if (auth.Status == "valid")
            {
                var rsaKeys = CsrHelper.GenerateRsaPrivateKey();
                var csrDetails = new CsrHelper.CsrDetails
                {
                    CommonName = dnsIdentifier
                };
                var csr = CsrHelper.GenerateCsr(csrDetails, rsaKeys);
                byte[] derRaw;
                using (var bs = new MemoryStream())
                {
                    csr.ExportAsDer(bs);
                    derRaw = bs.ToArray();
                }
                var derB64u = JwsHelper.Base64UrlEncode(derRaw);

                Console.WriteLine($"\nRequesting Cert");
                var certRequ = client.RequestCertificate(derB64u);

                Console.WriteLine($" Request Status: {certRequ.StatusCode}");

                //Console.WriteLine($"Refreshing Cert Request");
                //client.RefreshCertificateRequest(certRequ);

                if (certRequ.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    var keyGenFile = $"{dnsIdentifier}-gen-key.json";
                    var keyPemFile = $"{dnsIdentifier}-key.pem";
                    var csrGenFile = $"{dnsIdentifier}-gen-csr.json";
                    var csrPemFile = $"{dnsIdentifier}-csr.pem";
                    var crtDerFile = $"{dnsIdentifier}-crt.der";
                    var crtPemFile = $"{dnsIdentifier}-crt.pem";
                    var crtPfxFile = $"{dnsIdentifier}-all.pfx";

                    using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    {
                        rsaKeys.Save(fs);
                        File.WriteAllText(keyPemFile, rsaKeys.Pem);
                    }
                    using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    {
                        csr.Save(fs);
                        File.WriteAllText(csrPemFile, csr.Pem);
                    }

                    Console.WriteLine($" Saving Cert to {crtDerFile}");
                    using (var file = File.Create(crtDerFile))
                        certRequ.SaveCertificate(file);


                    using (FileStream source = new FileStream(crtDerFile, FileMode.Open), target = new FileStream(crtPemFile, FileMode.Create))
                    {
                        CsrHelper.Crt.ConvertDerToPem(source, target);
                    }

                    //Console.WriteLine($" Saving Cert to {crtPfxFile}");
                    // can't create a pfx until we get an irsPemFile, which seems to be some issuer cert thing.
                    //CsrHelper.Crt.ConvertToPfx(keyPemFile, crtPemFile, irsPemFile, crtPfxFile, FileMode.Create);
                }
            }
        }

        static AuthorizationState Authorize(AcmeClient client, string dnsIdentifier, string webRootPath)
        {
            Console.WriteLine($"\nAuthorizing Identifier {dnsIdentifier}");
            var authzState = client.AuthorizeIdentifier(dnsIdentifier);
            var challenge = client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
            var answerPath = Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, challenge.ChallengeAnswer.Key));

            Console.WriteLine($" Writing challenge answer to {answerPath}");
            Directory.CreateDirectory(Path.GetDirectoryName(answerPath));
            File.WriteAllText(answerPath, challenge.ChallengeAnswer.Value);

            var answerUri = new Uri(new Uri("http://" + dnsIdentifier), challenge.ChallengeAnswer.Key);
            Console.WriteLine($" Answer should now be browsable at {answerUri}");

            try
            {
                Console.WriteLine(" Submitting answer");
                // This always throws throw new InvalidOperationException("challenge answer has not been generated"); because the authoState.Challenge list isn't changing for some reason
                //client.SubmitAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);
                // so I pulled the core of SubmitAuthorizeChallengeAnswer into it's own method that I can call directly
                client.SubmitAuthorizeChallengeAnswer(challenge, true);

                // this loop is commented out because RefreshIdentifierAuthorization can't be called more than once currently.
                // have to loop to wait for server to stop being pending.
                // TODO: put timeout/retry limit in this loop
                //while (authzState.Status == "pending")
                //{
                    Console.WriteLine(" Refreshing authorization");
                    Thread.Sleep(1000); // this has to be here to give ACME server a chance to think
                    authzState = client.RefreshIdentifierAuthorization(authzState);
                //}

                Console.WriteLine($" Authorization RESULT: {authzState.Status}");
                if (authzState.Status == "invalid")
                {
                    Console.WriteLine("\n******************************************************************************");
                    Console.WriteLine($"The ACME server was probably unable to reach {answerUri}.");

                    Console.WriteLine(@"
Most likely this was caused by IIS not being setup to handle extensionless
static files. Here's how to fix that:
1. Goto Site/Server->Mime Types
2. Add a mime type of .* (application/octet-stream)
3. Goto Site/Server->Handler Mappings->View Ordered List
4. Move the StaticFile mapping above the ExtensionlessUrlHandler mappings.
(like this http://i.stack.imgur.com/nkvrL.png)
******************************************************************************");
                }

                return authzState;
            }
            finally
            {
                //Console.WriteLine(" Deleting answer");
                //File.Delete(answerPath);
            }
        }
    }
}
