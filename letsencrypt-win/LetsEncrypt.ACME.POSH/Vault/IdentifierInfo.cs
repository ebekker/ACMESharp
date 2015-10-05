using LetsEncrypt.ACME.POSH.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public class IdentifierInfo : IIdentifiable
    {
        public Guid Id
        { get; set; }

        public string Alias
        { get; set; }

        public string Label
        { get; set; }

        public string Memo
        { get; set; }

        public Guid RegistrationRef
        { get; set; }

        public string Dns
        { get; set; }

        public AuthorizationState Authorization
        { get; set; }

        public AuthorizationState AuthorizationUpdate
        { get; set; }

        public Dictionary<string, AuthorizeChallenge> Challenges
        { get; set; }

        public Dictionary<string, DateTime?> ChallengeCompleted
        { get; set; }
    }
}
