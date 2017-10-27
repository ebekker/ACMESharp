using ACMESharp.ACME;
using ACMESharp.Ext;
using System.Collections.Generic;

namespace ACMESharp.Providers.DNSMadeEasy
{
	/// <summary>
	/// Provider for a Challenge Handler that updates the TXT records on a
	/// DNS Made Easy account.
	/// </summary>
	/// <remarks>
	/// The staging environment is available as an option, but Let's Encrypt
	/// won't be able to read these values, so this is for debugging only
	/// </remarks>
	[ChallengeHandlerProvider("dnsme",
		ChallengeTypeKind.DNS,
		Label = "DNSMadeEasy Provider",
		Description = "A DNS Made Easy provider for handling Challenges." +
					  " This provider supports the DNS" +
					  " Challenge type and computes all the necessary" +
					  " response values. It will create DNS entries in your account.")]
	public class DnsMadeEasyChallengeHandlerProvider : IChallengeHandlerProvider
	{
        public static readonly ParameterDetail API_KEY = new ParameterDetail(
                nameof(DnsMadeEasyChallengeHandler.ApiKey),
                ParameterType.TEXT, isRequired: true, label: "API Key",
                desc: "The API Key for your DNS Made Easy account");

        public static readonly ParameterDetail SECRET_KEY = new ParameterDetail(
                nameof(DnsMadeEasyChallengeHandler.SecretKey),
                ParameterType.TEXT, isRequired: true, label: "Secret Key",
                desc: "The Secret Key for your DNS Made Easy account");

        public static readonly ParameterDetail STAGING = new ParameterDetail(
                nameof(DnsMadeEasyChallengeHandler.Staging),
                ParameterType.BOOLEAN, isRequired: false, label: "Staging",
                desc: "True if we should use the staging API server");

        private static readonly ParameterDetail[] PARAMS =
		{
            API_KEY, SECRET_KEY, STAGING
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

            if (!initParams.ContainsKey(API_KEY.Name))
                throw new KeyNotFoundException($"missing required parameter [{API_KEY.Name}]");

            if (!initParams.ContainsKey(SECRET_KEY.Name))
                throw new KeyNotFoundException($"missing required parameter [{SECRET_KEY.Name}]");

            var h = new DnsMadeEasyChallengeHandler();

            h.ApiKey = (string)initParams[API_KEY.Name];
            h.SecretKey = (string)initParams[SECRET_KEY.Name];

            if (initParams.ContainsKey(STAGING.Name))
                h.Staging = (bool)initParams[STAGING.Name];

            return h;
		}
	}
}
