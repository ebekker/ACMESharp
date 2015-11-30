using System;
using System.Collections.Generic;

namespace ACMESharp.Messages
{
    public class NewAuthzResponse
    {
        public string Status
        { get; set; }

        public DateTime? Expires
        { get; set; }

        public IdentifierPart Identifier
        { get; set; }

        public IEnumerable<ChallengePart> Challenges
        { get; set; }

        public IEnumerable<IEnumerable<int>> Combinations
        { get; set; }
    }
}
