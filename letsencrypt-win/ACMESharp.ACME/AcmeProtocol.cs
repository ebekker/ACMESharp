using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    public static class AcmeProtocol
    {
        #region -- Constants --

        public const string HTTP_METHOD_HEAD = "HEAD";
        public const string HTTP_METHOD_GET = "GET";
        public const string HTTP_METHOD_POST = "POST";
        public const string HTTP_CONTENT_TYPE_JSON = "application/json";
        public const string HTTP_USER_AGENT_FMT = "ACMEdotNET v{0} (ACME 1.0)";

        /// <summary>
        /// Identifier type indicator indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-5.3"
        /// >fully-qualified domain name (DNS)</see>.
        /// </summary>
        public const string IDENTIFIER_TYPE_DNS = "dns";

        /// <summary>
        /// Legacy Identifier validation challenge type indicator for
        /// <see cref="https://letsencrypt.github.io/acme-spec/#rfc.section.7.4"
        /// ></see>.
        /// Included for backward compatibility.
        /// </summary>
        public const string CHALLENGE_TYPE_LEGACY_DNS = "dns";
        /// <summary>
        /// Legacy Identifier validation challenge type indicator for
        /// <see cref="https://letsencrypt.github.io/acme-spec/#rfc.section.7.1"
        /// >Simple HTTP</see>.
        /// Included for backward compatibility.  TLS option is unsupported
        /// as this feature was later disallowed by the ACME spec.
        /// </summary>
        public const string CHALLENGE_TYPE_LEGACY_HTTP = "simpleHttp";
        /// <summary>
        /// Legacy Identifier validation challenge type indicator for
        /// <see cref="https://letsencrypt.github.io/acme-spec/#rfc.section.7.2"
        /// >DVSNI</see>.
        /// Included for backward compatibility.  Currently UNSUPPORTED.
        /// </summary>
        public const string CHALLENGE_TYPE_LEGACY_DVSNI = "dvsni";
        /// <summary>
        /// Legacy Identifier validation challenge type indicator for
        /// <see cref="https://letsencrypt.github.io/acme-spec/#rfc.section.7.3"
        /// >Proof of Possession of a Prior Key</see>.
        /// Included for backward compatibility.  Currently UNSUPPORTED.
        /// </summary>
        public const string CHALLENGE_TYPE_LEGACY_PRIORKEY = "proofOfPossession";

        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.5"
        /// >DNS</see>.
        /// </summary>
        public const string CHALLENGE_TYPE_DNS = "dns-01";
        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.2"
        /// >HTTP (non-SSL/TLS)</see>.
        /// </summary>
        public const string CHALLENGE_TYPE_HTTP = "http-01";
        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.3"
        /// >TLS SNI</see>.  Currently UNSUPPORTED.
        /// </summary>
        public const string CHALLENGE_TYPE_SNI = "tls-sni-01";
        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.4"
        /// >Proof of Possession of a Prior Key</see>.  Currently UNSUPPORTED.
        /// </summary>
        public const string CHALLENGE_TYPE_PRIORKEY = "proofOfPossession-01";


        public const string HEADER_REPLAY_NONCE = "Replay-nonce";
        public const string HEADER_LOCATION = "Location";
        public const string HEADER_LINK = "Link";
        public const string HEADER_RETRY_AFTER = "Retry-After";


        /// <summary>
        /// The relation name for the "Terms of Service" related link header.
        /// </summary>
        /// <remarks>
        /// Link headers can be returned as part of a registration:
        ///   HTTP/1.1 201 Created
        ///   Content-Type: application/json
        ///   Location: https://example.com/acme/reg/asdf
        ///   Link: <https://example.com/acme/new-authz>;rel="next"
        ///   Link: <https://example.com/acme/recover-reg>;rel="recover"
        ///   Link: <https://example.com/acme/terms>;rel="terms-of-service"
        ///
        /// The "terms-of-service" URI should be included in the "agreement" field
        /// in a subsequent registration update
        /// </remarks>
        public const string LINK_HEADER_REL_TOS = "terms-of-service";

        #endregion -- Constants --
    }
}
