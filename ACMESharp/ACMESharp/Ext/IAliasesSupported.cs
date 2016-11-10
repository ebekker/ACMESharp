using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Ext
{
    public interface IAliasesSupported
    {
        IEnumerable<string> Aliases
        { get; }
    }
}
