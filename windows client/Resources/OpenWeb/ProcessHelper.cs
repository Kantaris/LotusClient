using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace OpenWeb
{
	internal static class ProcessHelper
	{
		internal struct ProcessNameCacheEntry
		{
			internal readonly ulong ulLastLookup;
			internal readonly string sProcessName;
			internal ProcessNameCacheEntry(string _sProcessName)
			{
				this.ulLastLookup = Utilities.GetTickCount();
				this.sProcessName = _sProcessName;
			}
		}
		private const int QueryLimitedInformation = 4096;
		private const int ERROR_INSUFFICIENT_BUFFER = 122;
		private const int ERROR_SUCCESS = 0;
		private const uint MSEC_PROCESSNAME_CACHE_LIFETIME = 30000u;
		private static bool bDisambiguateWWAHostApps;
		private static readonly System.Collections.Generic.Dictionary<int, ProcessHelper.ProcessNameCacheEntry> dictProcessNames;
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		internal static extern System.IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		private static extern bool CloseHandle(System.IntPtr hHandle);
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		internal static extern int GetApplicationUserModelId(System.IntPtr hProcess, ref uint AppModelIDLength, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder sbAppUserModelID);
		static ProcessHelper()
		{
			ProcessHelper.bDisambiguateWWAHostApps = false;
			ProcessHelper.dictProcessNames = new System.Collections.Generic.Dictionary<int, ProcessHelper.ProcessNameCacheEntry>();
			OpenWebApplication.Janitor.assignWork(new SimpleEventHandler(ProcessHelper.ScavengeCache), 60000u);
			if (System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor > 1 && OpenWebApplication.Prefs.GetBoolPref("OpenWeb.ProcessInfo.DecorateWithAppName", true))
			{
				ProcessHelper.bDisambiguateWWAHostApps = true;
			}
		}
		internal static void ScavengeCache()
		{
			lock (ProcessHelper.dictProcessNames)
			{
				System.Collections.Generic.List<int> list = new System.Collections.Generic.List<int>();
				foreach (System.Collections.Generic.KeyValuePair<int, ProcessHelper.ProcessNameCacheEntry> current in ProcessHelper.dictProcessNames)
				{
					if (current.Value.ulLastLookup < Utilities.GetTickCount() - 30000uL)
					{
						list.Add(current.Key);
					}
				}
				foreach (int current2 in list)
				{
					ProcessHelper.dictProcessNames.Remove(current2);
				}
			}
		}
		internal static string GetProcessName(int iPID)
		{
			string text;
			string result;
			try
			{
				ProcessHelper.ProcessNameCacheEntry processNameCacheEntry;
				if (ProcessHelper.dictProcessNames.TryGetValue(iPID, out processNameCacheEntry))
				{
					if (processNameCacheEntry.ulLastLookup > Utilities.GetTickCount() - 30000uL)
					{
						text = processNameCacheEntry.sProcessName;
						result = text;
						return result;
					}
					lock (ProcessHelper.dictProcessNames)
					{
						ProcessHelper.dictProcessNames.Remove(iPID);
					}
				}
				string text2 = Process.GetProcessById(iPID).ProcessName.ToLower();
				if (string.IsNullOrEmpty(text2))
				{
					text = string.Empty;
				}
				else
				{
					if (ProcessHelper.bDisambiguateWWAHostApps && text2.OICEquals("WWAHost"))
					{
						try
						{
							System.IntPtr intPtr = ProcessHelper.OpenProcess(4096, false, iPID);
							if (System.IntPtr.Zero != intPtr)
							{
								uint capacity = 130u;
								System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder((int)capacity);
								int applicationUserModelId = ProcessHelper.GetApplicationUserModelId(intPtr, ref capacity, stringBuilder);
								if (applicationUserModelId == 0)
								{
									text2 = string.Format("{0}!{1}", text2, stringBuilder);
								}
								else
								{
									if (122 == applicationUserModelId)
									{
										stringBuilder = new System.Text.StringBuilder((int)capacity);
										if (ProcessHelper.GetApplicationUserModelId(intPtr, ref capacity, stringBuilder) == 0)
										{
											text2 = string.Format("{0}!{1}", text2, stringBuilder);
										}
									}
								}
								ProcessHelper.CloseHandle(intPtr);
							}
						}
						catch
						{
						}
					}
					lock (ProcessHelper.dictProcessNames)
					{
						if (!ProcessHelper.dictProcessNames.ContainsKey(iPID))
						{
							ProcessHelper.dictProcessNames.Add(iPID, new ProcessHelper.ProcessNameCacheEntry(text2));
						}
					}
					text = text2;
				}
			}
			catch (System.Exception)
			{
				text = string.Empty;
			}
			result = text;
			return result;
		}
	}
}
