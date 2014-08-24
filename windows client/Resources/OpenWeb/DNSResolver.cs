using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace OpenWeb
{
	internal class DNSResolver
	{
		private class DNSCacheEntry
		{
			internal ulong iLastLookup;
			internal IPAddress[] arrAddressList;
			internal DNSCacheEntry(IPAddress[] arrIPs)
			{
				this.iLastLookup = Utilities.GetTickCount();
				this.arrAddressList = arrIPs;
			}
		}
		private static readonly System.Collections.Generic.Dictionary<string, DNSResolver.DNSCacheEntry> dictAddresses;
		internal static ulong MSEC_DNS_CACHE_LIFETIME;
		private static readonly int COUNT_MAX_A_RECORDS;
		static DNSResolver()
		{
			DNSResolver.COUNT_MAX_A_RECORDS = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.dns.MaxAddressCount", 5);
			DNSResolver.MSEC_DNS_CACHE_LIFETIME = (ulong)((long)OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.dnscache", 150000));
			DNSResolver.dictAddresses = new System.Collections.Generic.Dictionary<string, DNSResolver.DNSCacheEntry>();
			OpenWebApplication.Janitor.assignWork(new SimpleEventHandler(DNSResolver.ScavengeCache), 30000u);
		}
		internal static void ClearCache()
		{
			lock (DNSResolver.dictAddresses)
			{
				DNSResolver.dictAddresses.Clear();
			}
		}
		public static void ScavengeCache()
		{
			if (DNSResolver.dictAddresses.Count >= 1)
			{
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew("Scavenging DNS Cache...");
				}
				System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
				lock (DNSResolver.dictAddresses)
				{
					foreach (System.Collections.Generic.KeyValuePair<string, DNSResolver.DNSCacheEntry> current in DNSResolver.dictAddresses)
					{
						if (current.Value.iLastLookup < Utilities.GetTickCount() - DNSResolver.MSEC_DNS_CACHE_LIFETIME)
						{
							list.Add(current.Key);
						}
					}
					if (CONFIG.bDebugSpew)
					{
						OpenWebApplication.DebugSpew(string.Concat(new string[]
						{
							"Expiring ",
							list.Count.ToString(),
							" of ",
							DNSResolver.dictAddresses.Count.ToString(),
							" DNS Records."
						}));
					}
					foreach (string current2 in list)
					{
						DNSResolver.dictAddresses.Remove(current2);
					}
				}
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew("Done scavenging DNS Cache...");
				}
			}
		}
		public static string InspectCache()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(8192);
			stringBuilder.AppendFormat("DNSResolver Cache\nOpenWeb.network.timeouts.dnscache: {0}ms\nContents\n--------\n", DNSResolver.MSEC_DNS_CACHE_LIFETIME);
			lock (DNSResolver.dictAddresses)
			{
				foreach (System.Collections.Generic.KeyValuePair<string, DNSResolver.DNSCacheEntry> current in DNSResolver.dictAddresses)
				{
					System.Text.StringBuilder stringBuilder2 = new System.Text.StringBuilder();
					stringBuilder2.Append(" [");
					IPAddress[] arrAddressList = current.Value.arrAddressList;
					for (int i = 0; i < arrAddressList.Length; i++)
					{
						IPAddress iPAddress = arrAddressList[i];
						stringBuilder2.Append(iPAddress.ToString());
						stringBuilder2.Append(", ");
					}
					stringBuilder2.Remove(stringBuilder2.Length - 2, 2);
					stringBuilder2.Append("]");
					stringBuilder.AppendFormat("\tHostName: {0}, Age: {1}ms, AddressList:{2}\n", current.Key, Utilities.GetTickCount() - current.Value.iLastLookup, stringBuilder2.ToString());
				}
			}
			stringBuilder.Append("--------\n");
			return stringBuilder.ToString();
		}
		public static IPAddress GetIPAddress(string sRemoteHost, bool bCheckCache)
		{
			return DNSResolver.GetIPAddressList(sRemoteHost, bCheckCache, null)[0];
		}
		public static IPAddress[] GetIPAddressList(string sRemoteHost, bool bCheckCache, SessionTimers oTimers)
		{
			IPAddress[] array = null;
			Stopwatch stopwatch = Stopwatch.StartNew();
			IPAddress iPAddress = Utilities.IPFromString(sRemoteHost);
			IPAddress[] result;
			if (iPAddress != null)
			{
				array = new IPAddress[]
				{
					iPAddress
				};
				if (oTimers != null)
				{
					oTimers.DNSTime = (int)stopwatch.ElapsedMilliseconds;
				}
				result = array;
			}
			else
			{
				DNSResolver.DNSCacheEntry dNSCacheEntry;
				if (bCheckCache && DNSResolver.dictAddresses.TryGetValue(sRemoteHost, out dNSCacheEntry))
				{
					if (dNSCacheEntry.iLastLookup > Utilities.GetTickCount() - DNSResolver.MSEC_DNS_CACHE_LIFETIME)
					{
						array = dNSCacheEntry.arrAddressList;
					}
					else
					{
						lock (DNSResolver.dictAddresses)
						{
							DNSResolver.dictAddresses.Remove(sRemoteHost);
						}
					}
				}
				if (array == null)
				{
					if (sRemoteHost.OICEndsWith(".onion") && !OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.dns.ResolveOnionHosts", false))
					{
						throw new System.InvalidOperationException("Hostnames ending in '.onion' cannot be resolved by DNS. You must send this request through a TOR gateway, e.g. oSession[\"X-OverrideGateway\"] = \"socks=127.0.0.1:9150\";");
					}
					try
					{
						array = Dns.GetHostAddresses(sRemoteHost);
					}
					catch
					{
						if (oTimers != null)
						{
							oTimers.DNSTime = (int)stopwatch.ElapsedMilliseconds;
						}
						throw;
					}
					array = DNSResolver.trimAddressList(array);
					if (array.Length < 1)
					{
						throw new System.Exception("No valid IPv4 addresses were found for this host.");
					}
					if (array.Length > 0)
					{
						lock (DNSResolver.dictAddresses)
						{
							if (!DNSResolver.dictAddresses.ContainsKey(sRemoteHost))
							{
								DNSResolver.dictAddresses.Add(sRemoteHost, new DNSResolver.DNSCacheEntry(array));
							}
						}
					}
				}
				if (oTimers != null)
				{
					oTimers.DNSTime = (int)stopwatch.ElapsedMilliseconds;
				}
				result = array;
			}
			return result;
		}
		private static IPAddress[] trimAddressList(IPAddress[] arrResult)
		{
			System.Collections.Generic.List<IPAddress> list = new System.Collections.Generic.List<IPAddress>();
			for (int i = 0; i < arrResult.Length; i++)
			{
				if (!list.Contains(arrResult[i]) && (CONFIG.bEnableIPv6 || arrResult[i].AddressFamily == AddressFamily.InterNetwork))
				{
					list.Add(arrResult[i]);
					if (DNSResolver.COUNT_MAX_A_RECORDS == list.Count)
					{
						break;
					}
				}
			}
			return list.ToArray();
		}
		internal static string GetCanonicalName(string sHostname)
		{
			string result;
			try
			{
				IPHostEntry hostEntry = Dns.GetHostEntry(sHostname);
				result = hostEntry.HostName;
			}
			catch (System.Exception var_2_13)
			{
				result = string.Empty;
			}
			return result;
		}
		internal static string GetAllInfo(string sHostname)
		{
			IPHostEntry hostEntry;
			string result;
			try
			{
				hostEntry = Dns.GetHostEntry(sHostname);
			}
			catch (System.Exception eX)
			{
				result = string.Format("OpenWebDNS> DNS Lookup for \"{0}\" failed because '{1}'\n", sHostname, Utilities.DescribeException(eX));
				return result;
			}
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			stringBuilder.AppendFormat("OpenWebDNS> DNS Lookup for \"{0}\":\r\n", sHostname);
			stringBuilder.AppendFormat("CNAME:\t{0}\n", hostEntry.HostName);
			stringBuilder.AppendFormat("Aliases:\t{0}\n", string.Join(";", hostEntry.Aliases));
			stringBuilder.AppendLine("Addresses:");
			IPAddress[] addressList = hostEntry.AddressList;
			for (int i = 0; i < addressList.Length; i++)
			{
				IPAddress iPAddress = addressList[i];
				stringBuilder.AppendFormat("\t{0}\r\n", iPAddress.ToString());
			}
			result = stringBuilder.ToString();
			return result;
		}
	}
}
