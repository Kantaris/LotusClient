using System;
using System.Runtime.InteropServices;
namespace OpenWeb
{
	public static class URLMonInterop
	{
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
		private class INTERNET_PROXY_INFO
		{
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
			public uint dwAccessType;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
			public string lpszProxy;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
			public string lpszProxyBypass;
		}
		private const uint URLMON_OPTION_USERAGENT = 268435457u;
		private const uint URLMON_OPTION_USERAGENT_REFRESH = 268435458u;
		private const uint INTERNET_OPEN_TYPE_PRECONFIG = 0u;
		private const uint INTERNET_OPEN_TYPE_DIRECT = 1u;
		private const uint INTERNET_OPEN_TYPE_PROXY = 3u;
		private const uint INTERNET_OPEN_TYPE_PRECONFIG_WITH_NO_AUTOPROXY = 4u;
		private const uint INTERNET_OPTION_REFRESH = 37u;
		private const uint INTERNET_OPTION_PROXY = 38u;
		[System.Runtime.InteropServices.DllImport("urlmon.dll", CharSet = CharSet.Ansi, EntryPoint = "UrlMkSetSessionOption", SetLastError = true)]
		private static extern int UrlMkSetSessionOptionUA(uint dwOption, string sNewUA, uint dwLen, uint dwZero);
		[System.Runtime.InteropServices.DllImport("urlmon.dll", CharSet = CharSet.Auto, EntryPoint = "UrlMkSetSessionOption", SetLastError = true)]
		private static extern int UrlMkSetSessionOptionProxy(uint dwOption, URLMonInterop.INTERNET_PROXY_INFO structNewProxy, uint dwLen, uint dwZero);
		[System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool InternetQueryOption(System.IntPtr hInternet, int Option, byte[] OptionInfo, ref int size);
		public static void SetUAStringInProcess(string sUA)
		{
			URLMonInterop.UrlMkSetSessionOptionUA(268435457u, sUA, (uint)sUA.Length, 0u);
		}
		public static string GetProxyInProcess()
		{
			int num = 0;
			byte[] array = new byte[1];
			num = array.Length;
			if (!URLMonInterop.InternetQueryOption(System.IntPtr.Zero, 38, array, ref num) && num != array.Length)
			{
				array = new byte[num];
				num = array.Length;
				bool flag = URLMonInterop.InternetQueryOption(System.IntPtr.Zero, 38, array, ref num);
			}
			return Utilities.ByteArrayToHexView(array, 16);
		}
		public static void ResetProxyInProcessToDefault()
		{
			URLMonInterop.UrlMkSetSessionOptionProxy(37u, null, 0u, 0u);
		}
		public static void SetProxyDisabledForProcess()
		{
			URLMonInterop.INTERNET_PROXY_INFO iNTERNET_PROXY_INFO = new URLMonInterop.INTERNET_PROXY_INFO();
			iNTERNET_PROXY_INFO.dwAccessType = 1u;
			iNTERNET_PROXY_INFO.lpszProxy = (iNTERNET_PROXY_INFO.lpszProxyBypass = null);
			uint dwLen = (uint)System.Runtime.InteropServices.Marshal.SizeOf(iNTERNET_PROXY_INFO);
			URLMonInterop.UrlMkSetSessionOptionProxy(38u, iNTERNET_PROXY_INFO, dwLen, 0u);
		}
		public static void SetProxyInProcess(string sProxy, string sBypassList)
		{
			URLMonInterop.INTERNET_PROXY_INFO iNTERNET_PROXY_INFO = new URLMonInterop.INTERNET_PROXY_INFO();
			iNTERNET_PROXY_INFO.dwAccessType = 3u;
			iNTERNET_PROXY_INFO.lpszProxy = sProxy;
			iNTERNET_PROXY_INFO.lpszProxyBypass = sBypassList;
			uint dwLen = (uint)System.Runtime.InteropServices.Marshal.SizeOf(iNTERNET_PROXY_INFO);
			URLMonInterop.UrlMkSetSessionOptionProxy(38u, iNTERNET_PROXY_INFO, dwLen, 0u);
		}
	}
}
