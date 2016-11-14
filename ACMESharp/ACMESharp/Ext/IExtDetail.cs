using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Ext
{
    public interface IExtDetail
    {
        CompositionContainer CompositionContainer
        { get;  set; }
    }
}
