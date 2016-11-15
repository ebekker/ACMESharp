using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI
{
    /// <summary>
    /// Defines the provider interface need to support discovery
    /// and instance-creation of a 
    /// </summary>
    public interface IPkiToolProvider
    {
        IEnumerable<ParameterDetail> DescribeParameters();

        IPkiTool GetPkiTool(IReadOnlyDictionary<string, object> initParams);
    }
}
