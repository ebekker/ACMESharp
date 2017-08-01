using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.ACME.Providers
{
	/// <summary>
	/// Provider for a Challenge Handler that outputs the manual steps
	/// needed to be completed by the operator.
	/// </summary>
	/// <remarks>
	/// When the output resolves to a file and that file already exists,
	/// unless either the Append or Overwrite parameters are specified
	/// as true, an exception will be raised.
	/// </remarks>
	[ChallengeHandlerProvider("msdns",
		ChallengeTypeKind.DNS,
		Label = "Microsoft DNS Provider",
		Description = "A microsoft dns provider for handling Challenges." +
					  " This provider supports the DNS" +
					  " Challenge type and computes all the necessary" +
					  " response values. It will create DNS entries.")]
	public class MSDNSChallengeHandlerProvider : IChallengeHandlerProvider
	{
		private static readonly ParameterDetail[] PARAMS =
		{
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
			var h = new MSDNSChallengeHandler();

			return h;
		}
	}
}
