using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.ACME
{
	public class ChallengeHandlingContext
	{
		/// <summary>
		/// Constructs a context instance.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="outWriter">if null, defaults to <c>Console.Out</c></param>
		public ChallengeHandlingContext(Challenge c, TextWriter outWriter = null)
		{
			Challenge = c;
			if (outWriter == null)
				outWriter = System.Console.Out;
			Out = outWriter;
		}

		public Challenge Challenge
		{ get; }

		public TextWriter Out
		{ get; }
	}
}
