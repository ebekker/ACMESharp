using ACMESharp.Ext;

namespace ACMESharp.ACME
{
	public interface IChallengeDecoderProviderInfo

	{
        string Type
        { get; }

        ChallengeTypeKind SupportedType
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }
}
