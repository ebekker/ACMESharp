using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using ACMESharp.JOSE;
using ACMESharp.Messages;

namespace ACMESharp.ACME
{
    /// <summary>
    /// Defines the Provider interface needed to support discovery
    /// and instance-creation of a <see cref="IChallengeParser"
    /// >Challenge Parser</see>.
    /// </summary>
    public interface IChallengeParserProvider // : IDisposable
    {
        bool IsSupported(IdentifierPart ip, ChallengePart cp);
        
        IChallengeParser GetParser(IdentifierPart ip, ChallengePart cp);
    }

    /// <summary>
    /// Defines the interface needed to support implementations of
    /// Challenge Parsers.
    /// </summary>
    /// <remarks>
    /// Challenge Parsers are those components that are able to decode
    /// the Challenge part of a new Authorization response message and
    /// compute the needed elements of a Challenge Response which will
    /// be handled by a <see cref="IChallengeHandler">Challenge Handler</see>.
    /// </remarks>
    public interface IChallengeParser : IDisposable
    {
        #region -- Properties --

        bool IsDisposed { get; }

        #endregion -- Properties --

        #region -- Methods --

        Challenge Parse(IdentifierPart ip, ChallengePart cp, ISigner signer);

        #endregion -- Methods --
    }
}
