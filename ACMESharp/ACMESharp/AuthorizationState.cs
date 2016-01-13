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
