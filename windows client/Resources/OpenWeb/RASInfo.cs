using System;
using System.Runtime.InteropServices;
namespace OpenWeb
{
	internal class RASInfo
	{
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct RASENTRYNAME
		{
			public int dwSize;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 257)]
			public string szEntryName;
			public int dwFlags;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 261)]
			public string szPhonebook;
		}
		[System.Flags]
		internal enum InternetConnectionState
		{
			INTERNET_CONNECTION_MODEM = 1,
			INTERNET_CONNECTION_LAN = 2,
			INTERNET_CONNECTION_PROXY = 4,
			INTERNET_RAS_INSTALLED = 16,
			INTERNET_CONNECTION_OFFLINE = 32,
			INTERNET_CONNECTION_CONFIGURED = 64
		}
		private const int MAX_PATH = 260;
		private const int RAS_MaxEntryName = 256;
		[System.Runtime.InteropServices.DllImport("rasapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern uint RasEnumEntries(System.IntPtr reserved, System.IntPtr lpszPhonebook, [System.Runtime.InteropServices.In] [System.Runtime.InteropServices.Out] RASInfo.RASENTRYNAME[] lprasentryname, ref int lpcb, ref int lpcEntries);
		[System.Runtime.InteropServices.DllImport("rasapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern uint RasEnumEntries(System.IntPtr reserved, System.IntPtr lpszPhonebook, [System.Runtime.InteropServices.In] [System.Runtime.InteropServices.Out] System.IntPtr lprasentryname, ref int lpcb, ref int lpcEntries);
		internal static string[] GetConnectionNames()
		{
			if (CONFIG.bDebugSpew)
			{
				OpenWebApplication.DebugSpew("WinINET indicates connectivity is via: " + RASInfo.GetConnectedState());
			}
			string[] result;
			try
			{
				int dwSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(RASInfo.RASENTRYNAME));
				int num = 0;
				uint num2;
				if (System.Environment.OSVersion.Version.Major < 6)
				{
					RASInfo.RASENTRYNAME[] array = new RASInfo.RASENTRYNAME[1];
					array[0].dwSize = dwSize;
					num2 = RASInfo.RasEnumEntries(System.IntPtr.Zero, System.IntPtr.Zero, array, ref dwSize, ref num);
				}
				else
				{
					num2 = RASInfo.RasEnumEntries(System.IntPtr.Zero, System.IntPtr.Zero, System.IntPtr.Zero, ref dwSize, ref num);
				}
				if (num2 != 0u && 603u != num2)
				{
					num = 0;
				}
				string[] array2 = new string[num + 1];
				array2[0] = "DefaultLAN";
				if (num == 0)
				{
					result = array2;
				}
				else
				{
					RASInfo.RASENTRYNAME[] array = new RASInfo.RASENTRYNAME[num];
					for (int i = 0; i < num; i++)
					{
						array[i].dwSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(RASInfo.RASENTRYNAME));
					}
					num2 = RASInfo.RasEnumEntries(System.IntPtr.Zero, System.IntPtr.Zero, array, ref dwSize, ref num);
					if (num2 != 0u)
					{
						result = new string[]
						{
							"DefaultLAN"
						};
					}
					else
					{
						for (int j = 0; j < num; j++)
						{
							array2[j + 1] = array[j].szEntryName;
						}
						result = array2;
					}
				}
			}
			catch (System.Exception var_8_18D)
			{
				result = new string[]
				{
					"DefaultLAN"
				};
			}
			return result;
		}
		[System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = CharSet.Unicode)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		internal static extern bool InternetGetConnectedState(ref RASInfo.InternetConnectionState lpdwFlags, int dwReserved);
		private static string GetConnectedState()
		{
			RASInfo.InternetConnectionState internetConnectionState = (RASInfo.InternetConnectionState)0;
			bool flag = RASInfo.InternetGetConnectedState(ref internetConnectionState, 0);
			string result;
			if (flag)
			{
				result = "CONNECTED (" + internetConnectionState.ToString() + ")";
			}
			else
			{
				result = "NOT_CONNECTED (" + internetConnectionState.ToString() + ")";
			}
			return result;
		}
	}
}
