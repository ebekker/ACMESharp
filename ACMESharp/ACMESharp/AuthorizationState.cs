using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using ACMESharp.Messages;
using ACMESharp.Util;

namespace ACMESharp
{
    public class AuthorizationState
    {
        public const string STATUS_PENDING = "pending";
        public const string STATUS_PROCESSING = "processing";
        public const string STATUS_VALID = "valid";
        public const string STATUS_INVALID = "invalid";
        public const string STATUS_REVOKED = "revoked";

        public IdentifierPart IdentifierPart
        { get; set; }

        public string IdentifierType
        { get; set; }

        public string Identifier
        { get; set; }

        public string Uri
        { get; set; }

        public string Status
        { get; set; }

        public DateTime? Expires
        { get; set; }

        public IEnumerable<AuthorizeChallenge> Challenges
        { get; set; }

        public IEnumerable<IEnumerable<int>> Combinations
        { get; set; }

        public bool IsPending()
        {
            return string.IsNullOrEmpty(Status) || string.Equals(Status, STATUS_PENDING,
                    StringComparison.InvariantCultureIgnoreCase);
        }

        public void Save(Stream s)
        {
            JsonHelper.Save(s, this);
        }

        public static AuthorizationState Load(Stream s)
        {
            return JsonHelper.Load<AuthorizationState>(s);
        }
    }
}
