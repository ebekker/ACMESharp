using ACMESharp.Ext;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
	/// <summary>
	/// Base class for all ACMESharp cmdlets implementing common logic.
	/// </summary>
	public abstract class AcmeCmdlet : PSCmdlet
	{
		static AcmeCmdlet()
		{
			InitModuleExt();
		}

		public static string UserDataRoot => Path.Combine(Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData), "ACMESharp");

		public static string SystemDataRoot => Path.Combine(Environment.GetFolderPath(
				Environment.SpecialFolder.CommonApplicationData), "ACMESharp");

		public static string UserExtensionsRoot => Path.Combine(UserDataRoot, "user-exts");

		public static string SystemExtensionsRoot => Path.Combine(SystemDataRoot, "sys-exts");

		static void InitModuleExt()
		{
			var oldExts = ExtCommon.ExtensionPaths;
			var newExts = new[] { UserExtensionsRoot, SystemExtensionsRoot };

			ExtCommon.ExtensionPaths = oldExts == null
					? newExts
					: oldExts.Concat(newExts);
		}
	}
}
