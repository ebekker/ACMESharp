using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Util
{
    public interface IIdentifiable
    {
        Guid Id
        { get; }

        string Alias
        { get; }
    }
}
