using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Installer
{
    /// <summary>
    /// Defines the provider interface need to support discovery
    /// and instance-creation of a 
    /// </summary>
    public interface IInstallerProvider
    {
        IEnumerable<ParameterDetail> DescribeParameters();

        IInstaller GetInstaller(IReadOnlyDictionary<string, object> initParams);
    }
}
