using ACMESharp.Ext;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
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
		private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

		static AcmeCmdlet()
		{
			InitModuleLogging();
			InitModuleExt();
		}

		public static string UserDataRoot => Path.Combine(Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData), "ACMESharp");

		public static string SystemDataRoot => Path.Combine(Environment.GetFolderPath(
				Environment.SpecialFolder.CommonApplicationData), "ACMESharp");

		public static string UserLoggerConfig => Path.Combine(UserDataRoot, "nlog.config");

		public static string SystemLoggerConfig => Path.Combine(SystemDataRoot, "nlog.config");

		public static string UserExtensionsRoot => Path.Combine(UserDataRoot, "user-exts");

		public static string SystemExtensionsRoot => Path.Combine(SystemDataRoot, "sys-exts");

		static void InitModuleLogging()
		{
			if (File.Exists(UserLoggerConfig))
			{
				LogManager.Configuration = new XmlLoggingConfiguration(UserLoggerConfig, true);
				LOG.Debug("Detected custom user logging configuration at [{0}]", UserLoggerConfig);
			}
			else if (File.Exists(SystemLoggerConfig))
			{
				LogManager.Configuration = new XmlLoggingConfiguration(SystemLoggerConfig, true);
				LOG.Debug("Detected custom system logging configuration at [{0}]", SystemLoggerConfig);
			}
			// We check for null in case the configuration has been set
			// externally by something other in the current PS session
			else if (LogManager.Configuration == null)
			{
				var cfg = new LoggingConfiguration();
				cfg.AddTarget(new ColoredConsoleTarget("console")
				{
					// Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}",
				});
				//cfg.AddRuleForOneLevel(LogLevel.Info, "console");
				LogManager.Configuration = cfg;
			}
		}

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
