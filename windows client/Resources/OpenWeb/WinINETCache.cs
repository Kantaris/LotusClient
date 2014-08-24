using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
namespace OpenWeb
{
	public class WinINETCache
	{
		private enum WININETCACHEENTRYTYPE
		{
			None,
			NORMAL_CACHE_ENTRY,
			STICKY_CACHE_ENTRY = 4,
			EDITED_CACHE_ENTRY = 8,
			TRACK_OFFLINE_CACHE_ENTRY = 16,
			TRACK_ONLINE_CACHE_ENTRY = 32,
			SPARSE_CACHE_ENTRY = 65536,
			COOKIE_CACHE_ENTRY = 1048576,
			URLHISTORY_CACHE_ENTRY = 2097152,
			ALL = 3211325
		}
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
		private class INTERNET_CACHE_ENTRY_INFOA
		{
			public uint dwStructureSize;
			public System.IntPtr lpszSourceUrlName;
			public System.IntPtr lpszLocalFileName;
			public WinINETCache.WININETCACHEENTRYTYPE CacheEntryType;
			public uint dwUseCount;
			public uint dwHitRate;
			public uint dwSizeLow;
			public uint dwSizeHigh;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastModifiedTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME ExpireTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastSyncTime;
			public System.IntPtr lpHeaderInfo;
			public uint dwHeaderInfoSize;
			public System.IntPtr lpszFileExtension;
			public WinINETCache.WININETCACHEENTRYINFOUNION _Union;
		}
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
		private struct WININETCACHEENTRYINFOUNION
		{
			[System.Runtime.InteropServices.FieldOffset(0)]
			public uint dwReserved;
			[System.Runtime.InteropServices.FieldOffset(0)]
			public uint dwExemptDelta;
		}
		private const int CACHEGROUP_SEARCH_ALL = 0;
		private const int CACHEGROUP_FLAG_FLUSHURL_ONDELETE = 2;
		private const int ERROR_FILE_NOT_FOUND = 2;
		private const int ERROR_NO_MORE_ITEMS = 259;
		private const int ERROR_INSUFFICENT_BUFFER = 122;
		internal static string GetCacheItemInfo(string sURL)
		{
			int num = 0;
			System.IntPtr intPtr = System.IntPtr.Zero;
			bool urlCacheEntryInfoA = WinINETCache.GetUrlCacheEntryInfoA(sURL, intPtr, ref num);
			int lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			string result;
			if (urlCacheEntryInfoA || lastWin32Error != 122)
			{
				result = string.Format("This URL is not present in the WinINET cache. [Code: {0}]", lastWin32Error);
			}
			else
			{
				int num2 = num;
				intPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(num2);
				urlCacheEntryInfoA = WinINETCache.GetUrlCacheEntryInfoA(sURL, intPtr, ref num);
				lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
				if (!urlCacheEntryInfoA)
				{
					System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
					result = "GetUrlCacheEntryInfoA with buffer failed. 2=filenotfound 122=insufficient buffer, 259=nomoreitems. Last error: " + lastWin32Error.ToString() + "\n";
				}
				else
				{
					WinINETCache.INTERNET_CACHE_ENTRY_INFOA iNTERNET_CACHE_ENTRY_INFOA = (WinINETCache.INTERNET_CACHE_ENTRY_INFOA)System.Runtime.InteropServices.Marshal.PtrToStructure(intPtr, typeof(WinINETCache.INTERNET_CACHE_ENTRY_INFOA));
					num = num2;
					long fileTime = (long)iNTERNET_CACHE_ENTRY_INFOA.LastModifiedTime.dwHighDateTime << 32 | (long)iNTERNET_CACHE_ENTRY_INFOA.LastModifiedTime.dwLowDateTime;
					long fileTime2 = (long)iNTERNET_CACHE_ENTRY_INFOA.LastAccessTime.dwHighDateTime << 32 | (long)iNTERNET_CACHE_ENTRY_INFOA.LastAccessTime.dwLowDateTime;
					long fileTime3 = (long)iNTERNET_CACHE_ENTRY_INFOA.LastSyncTime.dwHighDateTime << 32 | (long)iNTERNET_CACHE_ENTRY_INFOA.LastSyncTime.dwLowDateTime;
					long fileTime4 = (long)iNTERNET_CACHE_ENTRY_INFOA.ExpireTime.dwHighDateTime << 32 | (long)iNTERNET_CACHE_ENTRY_INFOA.ExpireTime.dwLowDateTime;
					string text = string.Concat(new string[]
					{
						"Url:\t\t",
						System.Runtime.InteropServices.Marshal.PtrToStringAnsi(iNTERNET_CACHE_ENTRY_INFOA.lpszSourceUrlName),
						"\nCache File:\t",
						System.Runtime.InteropServices.Marshal.PtrToStringAnsi(iNTERNET_CACHE_ENTRY_INFOA.lpszLocalFileName),
						"\nSize:\t\t",
						(((ulong)iNTERNET_CACHE_ENTRY_INFOA.dwSizeHigh << 32) + (ulong)iNTERNET_CACHE_ENTRY_INFOA.dwSizeLow).ToString("0,0"),
						" bytes\nFile Extension:\t",
						System.Runtime.InteropServices.Marshal.PtrToStringAnsi(iNTERNET_CACHE_ENTRY_INFOA.lpszFileExtension),
						"\nHit Rate:\t",
						iNTERNET_CACHE_ENTRY_INFOA.dwHitRate.ToString(),
						"\nUse Count:\t",
						iNTERNET_CACHE_ENTRY_INFOA.dwUseCount.ToString(),
						"\nDon't Scavenge for:\t",
						iNTERNET_CACHE_ENTRY_INFOA._Union.dwExemptDelta.ToString(),
						" seconds\nLast Modified:\t",
						System.DateTime.FromFileTime(fileTime).ToString(),
						"\nLast Accessed:\t",
						System.DateTime.FromFileTime(fileTime2).ToString(),
						"\nLast Synced:  \t",
						System.DateTime.FromFileTime(fileTime3).ToString(),
						"\nEntry Expires:\t",
						System.DateTime.FromFileTime(fileTime4).ToString(),
						"\n"
					});
					System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
					result = text;
				}
			}
			return result;
		}
		public static void ClearCookies()
		{
			WinINETCache.ClearCacheItems(false, true);
		}
		public static void ClearFiles()
		{
			WinINETCache.ClearCacheItems(true, false);
		}
		[CodeDescription("Delete all permanent WinINET cookies for sHost; won't clear memory-only session cookies. Supports hostnames with an optional leading wildcard, e.g. *example.com. NOTE: Will not work on VistaIE Protected Mode cookies.")]
		public static void ClearCookiesForHost(string sHost)
		{
			sHost = sHost.Trim();
			if (sHost.Length >= 1)
			{
				string text;
				if (sHost == "*")
				{
					text = string.Empty;
					if (System.Environment.OSVersion.Version.Major > 5)
					{
						WinINETCache.VistaClearTracks(false, true);
						return;
					}
				}
				else
				{
					text = (sHost.StartsWith("*") ? sHost.Substring(1).ToLower() : ("@" + sHost.ToLower()));
				}
				int num = 0;
				System.IntPtr intPtr = System.IntPtr.Zero;
				System.IntPtr intPtr2 = System.IntPtr.Zero;
				intPtr2 = WinINETCache.FindFirstUrlCacheEntry("cookie:", System.IntPtr.Zero, ref num);
				if (!(intPtr2 == System.IntPtr.Zero) || 259 != System.Runtime.InteropServices.Marshal.GetLastWin32Error())
				{
					int num2 = num;
					intPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(num2);
					intPtr2 = WinINETCache.FindFirstUrlCacheEntry("cookie:", intPtr, ref num);
					while (true)
					{
						WinINETCache.INTERNET_CACHE_ENTRY_INFOA iNTERNET_CACHE_ENTRY_INFOA = (WinINETCache.INTERNET_CACHE_ENTRY_INFOA)System.Runtime.InteropServices.Marshal.PtrToStructure(intPtr, typeof(WinINETCache.INTERNET_CACHE_ENTRY_INFOA));
						num = num2;
						if (WinINETCache.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY == (iNTERNET_CACHE_ENTRY_INFOA.CacheEntryType & WinINETCache.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY))
						{
							bool flag;
							if (text.Length == 0)
							{
								flag = true;
							}
							else
							{
								string text2 = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(iNTERNET_CACHE_ENTRY_INFOA.lpszSourceUrlName);
								int num3 = text2.IndexOf('/');
								if (num3 > 0)
								{
									text2 = text2.Remove(num3);
								}
								text2 = text2.ToLower();
								flag = text2.EndsWith(text);
							}
							if (flag)
							{
								bool flag2 = WinINETCache.DeleteUrlCacheEntry(iNTERNET_CACHE_ENTRY_INFOA.lpszSourceUrlName);
							}
						}
						while (true)
						{
							bool flag2 = WinINETCache.FindNextUrlCacheEntry(intPtr2, intPtr, ref num);
							if (!flag2 && 259 == System.Runtime.InteropServices.Marshal.GetLastWin32Error())
							{
								goto Block_12;
							}
							if (flag2 || num <= num2)
							{
								break;
							}
							num2 = num;
							intPtr = System.Runtime.InteropServices.Marshal.ReAllocHGlobal(intPtr, (System.IntPtr)num2);
						}
					}
					Block_12:
					System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
				}
			}
		}
		public static void ClearCacheItems(bool bClearFiles, bool bClearCookies)
		{
			if (!bClearCookies && !bClearFiles)
			{
				throw new System.ArgumentException("You must call ClearCacheItems with at least one target");
			}
			if (OpenWebApplication.DoClearCache(bClearFiles, bClearCookies))
			{
				if (System.Environment.OSVersion.Version.Major > 5)
				{
					WinINETCache.VistaClearTracks(bClearFiles, bClearCookies);
				}
				else
				{
					if (bClearCookies)
					{
						WinINETCache.ClearCookiesForHost("*");
					}
					if (bClearFiles)
					{
						long groupId = 0L;
						int num = 0;
						System.IntPtr intPtr = System.IntPtr.Zero;
						System.IntPtr intPtr2 = System.IntPtr.Zero;
						intPtr2 = WinINETCache.FindFirstUrlCacheGroup(0, 0, System.IntPtr.Zero, 0, ref groupId, System.IntPtr.Zero);
						int lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
						if (intPtr2 != System.IntPtr.Zero && 259 != lastWin32Error && 2 != lastWin32Error)
						{
							bool flag;
							do
							{
								flag = WinINETCache.DeleteUrlCacheGroup(groupId, 2, System.IntPtr.Zero);
								lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
								if (!flag && 2 == lastWin32Error)
								{
									flag = WinINETCache.FindNextUrlCacheGroup(intPtr2, ref groupId, System.IntPtr.Zero);
									lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
								}
							}
							while (flag || (259 != lastWin32Error && 2 != lastWin32Error));
						}
						intPtr2 = WinINETCache.FindFirstUrlCacheEntryEx(null, 0, WinINETCache.WININETCACHEENTRYTYPE.ALL, 0L, System.IntPtr.Zero, ref num, System.IntPtr.Zero, System.IntPtr.Zero, System.IntPtr.Zero);
						lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
						if (!(System.IntPtr.Zero == intPtr2) || 259 != lastWin32Error)
						{
							int num2 = num;
							intPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(num2);
							intPtr2 = WinINETCache.FindFirstUrlCacheEntryEx(null, 0, WinINETCache.WININETCACHEENTRYTYPE.ALL, 0L, intPtr, ref num, System.IntPtr.Zero, System.IntPtr.Zero, System.IntPtr.Zero);
							lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
							bool flag;
							do
							{
								WinINETCache.INTERNET_CACHE_ENTRY_INFOA iNTERNET_CACHE_ENTRY_INFOA = (WinINETCache.INTERNET_CACHE_ENTRY_INFOA)System.Runtime.InteropServices.Marshal.PtrToStructure(intPtr, typeof(WinINETCache.INTERNET_CACHE_ENTRY_INFOA));
								num = num2;
								if (WinINETCache.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY != (iNTERNET_CACHE_ENTRY_INFOA.CacheEntryType & WinINETCache.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY))
								{
									flag = WinINETCache.DeleteUrlCacheEntry(iNTERNET_CACHE_ENTRY_INFOA.lpszSourceUrlName);
								}
								flag = WinINETCache.FindNextUrlCacheEntryEx(intPtr2, intPtr, ref num, System.IntPtr.Zero, System.IntPtr.Zero, System.IntPtr.Zero);
								lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
								if (!flag && 259 == lastWin32Error)
								{
									break;
								}
								if (!flag && num > num2)
								{
									num2 = num;
									intPtr = System.Runtime.InteropServices.Marshal.ReAllocHGlobal(intPtr, (System.IntPtr)num2);
									flag = WinINETCache.FindNextUrlCacheEntryEx(intPtr2, intPtr, ref num, System.IntPtr.Zero, System.IntPtr.Zero, System.IntPtr.Zero);
								}
							}
							while (flag);
							System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
						}
					}
				}
			}
		}
		private static void VistaClearTracks(bool bClearFiles, bool bClearCookies)
		{
			int num = 0;
			if (bClearCookies)
			{
				num |= 2;
			}
			if (bClearFiles)
			{
				num |= 4108;
			}
			try
			{
				using (Process.Start("rundll32.exe", "inetcpl.cpl,ClearMyTracksByProcess " + num.ToString()))
				{
				}
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser("Failed to launch ClearMyTracksByProcess.\n" + ex.Message, "Error");
			}
		}
		[System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool GetUrlCacheEntryInfoA(string lpszUrlName, System.IntPtr lpCacheEntryInfo, ref int lpdwCacheEntryInfoBufferSize);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern System.IntPtr FindFirstUrlCacheGroup(int dwFlags, int dwFilter, System.IntPtr lpSearchCondition, int dwSearchCondition, ref long lpGroupId, System.IntPtr lpReserved);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool FindNextUrlCacheGroup(System.IntPtr hFind, ref long lpGroupId, System.IntPtr lpReserved);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool DeleteUrlCacheGroup(long GroupId, int dwFlags, System.IntPtr lpReserved);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindFirstUrlCacheEntryA", ExactSpelling = true, SetLastError = true)]
		private static extern System.IntPtr FindFirstUrlCacheEntry([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string lpszUrlSearchPattern, System.IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindNextUrlCacheEntryA", ExactSpelling = true, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool FindNextUrlCacheEntry(System.IntPtr hFind, System.IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindFirstUrlCacheEntryExA", ExactSpelling = true, SetLastError = true)]
		private static extern System.IntPtr FindFirstUrlCacheEntryEx([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string lpszUrlSearchPattern, int dwFlags, WinINETCache.WININETCACHEENTRYTYPE dwFilter, long GroupId, System.IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize, System.IntPtr lpReserved, System.IntPtr pcbReserved2, System.IntPtr lpReserved3);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindNextUrlCacheEntryExA", ExactSpelling = true, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool FindNextUrlCacheEntryEx(System.IntPtr hEnumHandle, System.IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize, System.IntPtr lpReserved, System.IntPtr pcbReserved2, System.IntPtr lpReserved3);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "DeleteUrlCacheEntryA", ExactSpelling = true, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool DeleteUrlCacheEntry(System.IntPtr lpszUrlName);
	}
}
