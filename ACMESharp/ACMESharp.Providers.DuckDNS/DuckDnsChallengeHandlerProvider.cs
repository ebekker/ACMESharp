using ACMESharp.ACME;
using ACMESharp.Ext;
using System.Collections.Generic;

namespace ACMESharp.Providers.DuckDNS
{
    /// <summary>
    /// Provider for a Challenge Handler that updates the TXT records on a
    /// Duck DNS account.
    /// </summary>
    [ChallengeHandlerProvider("duckdns",
        ChallengeTypeKind.DNS,
        Label = "Duck DNS Provider",
        Description = "A Duck DNS provider for handling Challenges." +
                      " This provider supports the DNS" +
                      " Challenge type and computes all the necessary" +
                      " response values.")]
    public class DuckDnsChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail TOKEN = new ParameterDetail(
                nameof(DuckDnsChallengeHandler.Token),
                ParameterType.TEXT, isRequired: true, label: "Token",
                desc: "The Token for your Duck DNS account");

        private static readonly ParameterDetail[] PARAMS =
        {
            TOKEN
        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public bool IsSupported(Challenge c)
        {
            return c is DnsChallenge;
        }

        public IChallengeHandler GetHandler(Challenge c, IReadOnlyDictionary<string, object> initParams)
        {
            if (initParams == null)
                initParams = new Dictionary<string, object>();

            if (!initParams.ContainsKey(TOKEN.Name))
                throw new KeyNotFoundException($"missing required parameter [{TOKEN.Name}]");

            var h = new DuckDnsChallengeHandler();

            h.Token = (string)initParams[TOKEN.Name];

            return h;
        }
    }
}
