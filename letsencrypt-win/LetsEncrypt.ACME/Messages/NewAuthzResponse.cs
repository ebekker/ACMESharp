using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class NewAuthzResponse
    {
        public string Status
        { get; set; }

        public IdentifierPart Identifier
        { get; set; }

        public IEnumerable<ChallengePart> Challenges
        { get; set; }

        public IEnumerable<IEnumerable<int>> Combinations
        { get; set; }
    }
}
