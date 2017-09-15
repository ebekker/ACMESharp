using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.ACME
{
    [Flags]
    public enum ChallengeTypeKind
    {
        UNSPECIFIED = 0x00,

        PRIOR_KEY = 0x10,
        DNS = 0x20,
        HTTP = 0x40,
        TLS_SNI = 0x80,

        OTHER = 0x01,
    }

    public abstract class Challenge
    {
        protected Challenge(ChallengeTypeKind typeKind, string type, ChallengeAnswer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer), "challenge answer is required");

            TypeKind = typeKind;
            Type = type;
            Answer = answer;
        }

        public ChallengeTypeKind TypeKind
        { get; private set; }

        public string Type
        { get; private set; }

        public ChallengeAnswer Answer
        { get; private set; }
    }

    public class DnsChallenge : Challenge
    {
        public DnsChallenge(string type, ChallengeAnswer answer)
            : base(ChallengeTypeKind.DNS, type, answer)
        { }

        public string Token
        { get; set; }

        public string RecordName
        { get; set; }

        public string RecordValue
        { get; set; }
    }

    public class HttpChallenge : Challenge
    {
        public HttpChallenge(string type, ChallengeAnswer answer)
            : base(ChallengeTypeKind.HTTP, type, answer)
        { }

        public string Token
        { get; set; }

		/// <summary>
		/// The complete URL including host and path components at which the
		/// Challenge Response is expected to be served from.
		/// </summary>
		/// <remarks>
		/// The ACME specification specifies the requirements for this URL in
		/// <see cref="https://ietf-wg-acme.github.io/acme/draft-ietf-acme-acme.html#rfc.section.8.3">section 8.3</see>.
		/// </remarks>
		public string FileUrl
        { get; set; }

		/// <summary>
		/// The file path component of the Challenge Response URL, relative to the
		/// root of the response HTTP server.
		/// </summary>
        public string FilePath
        { get; set; }

		/// <summary>
		/// The complete content of the file that satisfies the Challenge Response.
		/// </summary>
        public string FileContent
        { get; set; }
    }

    public class TlsSniChallenge : Challenge
    {
        public TlsSniChallenge(string type, ChallengeAnswer answer)
            : base(ChallengeTypeKind.TLS_SNI, type, answer)
        { }

        public string Token
        { get; set; }

        public int IterationCount
        { get; set; }

        // TODO:  this is incomplete!!!
    }
}
