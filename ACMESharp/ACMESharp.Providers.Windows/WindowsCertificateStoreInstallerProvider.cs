using ACMESharp.Ext;
using ACMESharp.Installer;
using System;
using System.Collections.Generic;
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

			initParams.GetParameter(STORE_LOCATION,
					(StoreLocation x) => inst.StoreLocation = x);
			initParams.GetParameter(STORE_NAME,
					(StoreName x) => inst.StoreName = x);
			initParams.GetParameter(FRIENDLY_NAME,
					(string x) => inst.FriendlyName = x);

            return inst;
        }
    }
}
