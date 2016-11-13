using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using System.Security.Cryptography.X509Certificates;

namespace ACMESharp.Providers.Windows
{
    [InstallerProvider("win-cert",
        Aliases = new[] { "cert-store", "certificate-store"},
        Label = "Windows Certificate Store",
        Description = "Installer supprts importing certificate to the"
                + " user or system Windows Certificate Store")]
    public class WindowsCertificateStoreInstallerProvider : IInstallerProvider
    {
        public static readonly ParameterDetail STORE_LOCATION = new ParameterDetail(
                nameof(WindowsCertificateStoreInstaller.StoreLocation),
                ParameterType.TEXT, label: "Store Location",
                desc: "Optional store location (CurrentUser or LocalMachine);"
                        + "defaults to CurrentUser");

        public static readonly ParameterDetail STORE_NAME = new ParameterDetail(
                nameof(WindowsCertificateStoreInstaller.StoreName),
                ParameterType.TEXT, label: "Store Name",
                desc: "Optional store name");

        public static readonly ParameterDetail FRIENDLY_NAME = new ParameterDetail(
                nameof(WindowsCertificateStoreInstaller.FriendlyName),
                ParameterType.TEXT, label: "Friendly Name",
                desc: "Optional user-facing label to assign the certificate upon import");

        static readonly ParameterDetail[] PARAMS =
        {
            STORE_LOCATION,
            STORE_NAME,
            FRIENDLY_NAME,
        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public IInstaller GetInstaller(IReadOnlyDictionary<string, object> initParams)
        {
            var inst = new WindowsCertificateStoreInstaller();

            if (initParams == null)
                initParams = new Dictionary<string, object>();

            // Required params
            // (none)

            // Optional params
            if (initParams.ContainsKey(STORE_LOCATION.Name))
                inst.StoreLocation = (StoreLocation)Enum.Parse(
                        typeof(StoreLocation),
                        (string)initParams[STORE_LOCATION.Name],
                        true);
            if (initParams.ContainsKey(STORE_NAME.Name))
                inst.StoreName = (StoreName)Enum.Parse(
                        typeof(StoreName),
                        (string)initParams[STORE_NAME.Name],
                        true);
            if (initParams.ContainsKey(FRIENDLY_NAME.Name))
                inst.FriendlyName = (string)initParams[FRIENDLY_NAME.Name];

            return inst;
        }
    }
}
