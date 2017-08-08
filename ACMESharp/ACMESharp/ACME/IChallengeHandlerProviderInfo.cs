using ACMESharp.Ext;

namespace ACMESharp.ACME
{
	public interface IChallengeHandlerProviderInfo : IAliasesSupported
    {
        string Name
        { get; }

        string Label
        { get; }

        string Description
        { get; }

        ChallengeTypeKind SupportedTypes
        { get; }

        bool IsCleanUpSupported
        { get; }
    }
}
