using ACMESharp.ACME;
using System;
using System.Linq;
using System.Management;

namespace ACMESharp.Providers.Windows
{
	public class MSDNSChallengeHandler : IChallengeHandler
	{
		public bool IsDisposed
		{
			get; private set;
		}

		public void CleanUp(Challenge c)
		{
		}

		public void Dispose()
		{
			IsDisposed = true;
		}

		public void Handle(Challenge c)
		{
			DnsChallenge dnsChallenge = c as DnsChallenge;

			ManagementScope mgmtScope = new ManagementScope(@"\\.\Root\MicrosoftDNS");
			ManagementClass mgmtClass = null;
			ManagementBaseObject mgmtParams = null;
			ManagementObjectSearcher mgmtSearch = null;
			ManagementObjectCollection mgmtDNSRecords = null;
			string strQuery;

			strQuery = string.Format("SELECT * FROM MicrosoftDNS_TXTType WHERE OwnerName = '{0}'", dnsChallenge.RecordName);

			mgmtScope.Connect();

			mgmtSearch = new ManagementObjectSearcher(mgmtScope, new ObjectQuery(strQuery));

			mgmtDNSRecords = mgmtSearch.Get();

			if (mgmtDNSRecords.Count == 1)
			{
				foreach (ManagementObject mgmtDNSRecord in mgmtDNSRecords)
				{
					mgmtParams = mgmtDNSRecord.GetMethodParameters("modify");
					mgmtParams["DescriptiveText"] = dnsChallenge.RecordValue;

					mgmtDNSRecord.InvokeMethod("modify", mgmtParams, null);

					break;
				}
			}
			else if (mgmtDNSRecords.Count == 0)
			{
				mgmtClass = new ManagementClass(mgmtScope, new ManagementPath("MicrosoftDNS_TXTType"), null);

				mgmtParams = mgmtClass.GetMethodParameters("CreateInstanceFromPropertyData");
				mgmtParams["DnsServerName"] = Environment.MachineName;
				mgmtParams["ContainerName"] = dnsChallenge.RecordName.Split('.')[dnsChallenge.RecordName.Split('.').Count() - 2] + "." + dnsChallenge.RecordName.Split('.')[dnsChallenge.RecordName.Split('.').Count() - 1];
				mgmtParams["OwnerName"] = dnsChallenge.RecordName;
				mgmtParams["DescriptiveText"] = dnsChallenge.RecordValue;

				mgmtClass.InvokeMethod("CreateInstanceFromPropertyData", mgmtParams, null);
			}
			else
			{
				throw new InvalidOperationException("There should not be more than one DNS txt record for the name.");
			}
		}
	}
}
