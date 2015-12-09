using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Ext
{
    public struct NamedInfo<TInfo>
    {
        public NamedInfo(string name, TInfo info)
        {
            Name = name;
            Info = info;
        }

        public string Name
        { get; private set; }

        public TInfo Info
        { get; private set; }
    }
}
