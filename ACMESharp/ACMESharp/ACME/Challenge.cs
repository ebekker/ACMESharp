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
        UNSPECIFIED = 0,
        DNS = 1,
        HTTP = 2,
        TLS_SNI = 4,
    }

    public abstract class Challenge
    {
        public ChallengeTypeKind TypeKind
        { get; set; }

        public string Type
        { get; set; }
    }

    public class DnsChallenge : Challenge
    {
        public string Token
        { get; set; }

        public string RecordName
        { get; set; }

        public string RecordValue
        { get; set; }
    }

    public class HttpChallenge : Challenge
    {
        public string Token
        { get; set; }

        public string FileUrl
        { get; set; }

        public string FilePath
        { get; set; }

        public string FileContent
        { get; set; }
    }

    public class TlsSniChallenge : Challenge
    {
        public string Token
        { get; set; }

        public int IterationCount
        { get; set; }

        // TODO:  this is incomplete!!!
    }
}
