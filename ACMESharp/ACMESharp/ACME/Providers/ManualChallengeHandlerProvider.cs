using System.Collections.Generic;
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
    [ChallengeHandlerProvider("manual",
        ChallengeTypeKind.DNS | ChallengeTypeKind.HTTP,
        Label = "Manual Provider",
        Description = "A manual provider for handling Challenges." +
                      " This provider supports the DNS and HTTP" +
                      " Challenge types and computes all the necessary" +
                      " response values. It will provide instructions" +
                      " to the user on what to do with the values but" +
                      " actual steps must be implemented manually.")]
    public class ManualChallengeHandlerProvider : IChallengeHandlerProvider
    {
        public static readonly ParameterDetail WRITE_OUT_PATH = new ParameterDetail(
                nameof(ManualChallengeHandler.WriteOutPath),
                ParameterType.TEXT, label: "Write Out Path",
                desc: "Path to a file in which to write out manual instructions," +
                      "or one of the special standard streams (OUT, ERR)");

        public static readonly ParameterDetail APPEND = new ParameterDetail(
                nameof(ManualChallengeHandler.Append),
                ParameterType.BOOLEAN, label: "Append",
                desc: "When true, output to a file will be appended");

        public static readonly ParameterDetail OVERWRITE = new ParameterDetail(
                nameof(ManualChallengeHandler.Overwrite),
                ParameterType.BOOLEAN, label: "Overwrite",
                desc: "When true, output to a file will overwrite the file");

        private static readonly ParameterDetail[] PARAMS =
        {
            WRITE_OUT_PATH,
            APPEND,
            OVERWRITE,
        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public bool IsSupported(Challenge c)
        {
            return c is DnsChallenge || c is HttpChallenge;
        }

        public IChallengeHandler GetHandler(Challenge c, IReadOnlyDictionary<string, object> initParams)
        {
            var h = new ManualChallengeHandler();

            // Start off with the current (default) settings
            var p = h.WriteOutPath;
            var a = h.Append;
            var o = h.Overwrite;

            if (initParams?.Count > 0)
            {
                // See which ones are overridden
                if (initParams.ContainsKey(WRITE_OUT_PATH.Name))
                    p = (string) initParams[WRITE_OUT_PATH.Name];
                if (initParams.ContainsKey(APPEND.Name))
                    a = (bool) initParams[APPEND.Name];
                if (initParams.ContainsKey(OVERWRITE.Name))
                    o = (bool) initParams[OVERWRITE.Name];

                // Apply any changes
                h.SetOut(p, a, o);
            }

            return h;
        }
    }
}
