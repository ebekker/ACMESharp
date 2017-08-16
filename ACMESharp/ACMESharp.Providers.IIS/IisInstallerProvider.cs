using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using System.ComponentModel;

namespace ACMESharp.Providers.IIS
{
	[InstallerProvider("iis",
		Label = "Internet Information Server (IIS)",
		Description = "Provider for installing certificates to a local IIS site",
		IsUninstallSupported = true)]
	public class IisInstallerProvider : IInstallerProvider
	{
		public static readonly ParameterDetail WEB_SITE_REF = new ParameterDetail(
				nameof(IisInstaller.WebSiteRef),
				ParameterType.TEXT, isRequired: true, label: "Web Site Ref",
				desc: "Either the Name or the ID of a local IIS Web Site");

		public static readonly ParameterDetail BINDING_ADDRESS = new ParameterDetail(
				nameof(IisInstaller.BindingAddress),
				ParameterType.TEXT, label: "Binding Address",
				desc: "An optional address to bind to; defaults to all interfaces");

		public static readonly ParameterDetail BINDING_PORT = new ParameterDetail(
				nameof(IisInstaller.BindingPort),
				ParameterType.NUMBER, label: "Binding Port",
				desc: "An optional port to bind to; defaults to HTTPS port (443)");

		public static readonly ParameterDetail BINDING_HOST = new ParameterDetail(
				nameof(IisInstaller.BindingHost),
				ParameterType.TEXT, label: "Binding Host",
				desc: "An optional host name to bind to using SNI (IIS8+)");

		public static readonly ParameterDetail BINDING_HOST_REQUIRED = new ParameterDetail(
				nameof(IisInstaller.BindingHostRequired),
				ParameterType.BOOLEAN, label: "Binding Host Required",
				desc: "An optional flag to indicate SNI is required (IIS8+)");

		public static readonly ParameterDetail FORCE = new ParameterDetail(
				nameof(IisInstaller.Force),
				ParameterType.BOOLEAN, label: "Force",
				desc: "An optional flag to overwrite an existing binding matching the target criteria");

		public static readonly ParameterDetail CERTIFICATE_FRIENDLY_NAME = new ParameterDetail(
				nameof(IisInstaller.CertificateFriendlyName),
				ParameterType.TEXT, label: "Certificate Friendly Name",
				desc: "An optional user-facing label to assign the certificate"
						+ " upon import to the Windows Certificate Store.");

		static readonly ParameterDetail[] PARAMS =
		{
			WEB_SITE_REF,
			BINDING_ADDRESS,
			BINDING_PORT,
			BINDING_HOST,
			BINDING_HOST_REQUIRED,
			FORCE,
			CERTIFICATE_FRIENDLY_NAME,
		};

		public IEnumerable<ParameterDetail> DescribeParameters()
		{
			return PARAMS;
		}

		public IInstaller GetInstaller(IReadOnlyDictionary<string, object> initParams)
		{
			var inst = new IisInstaller();

			if (initParams == null)
				initParams = new Dictionary<string, object>();

			// Required params
			initParams.GetParameter(WEB_SITE_REF,
					(string x) => inst.WebSiteRef = x);

			// Optional params
			initParams.GetParameter(BINDING_ADDRESS,
					(string x) => inst.BindingAddress = x);
			initParams.GetParameter(BINDING_PORT,
					(int x) => inst.BindingPort = x);
			initParams.GetParameter(BINDING_HOST,
					(string x) => inst.BindingHost = x);
			initParams.GetParameter(BINDING_HOST_REQUIRED,
					(bool x) => inst.BindingHostRequired = x);
			initParams.GetParameter(FORCE,
					(bool x) => inst.Force = x);
			initParams.GetParameter(CERTIFICATE_FRIENDLY_NAME,
					(string x) => inst.CertificateFriendlyName = x);

			return inst;
		}
	}
}
