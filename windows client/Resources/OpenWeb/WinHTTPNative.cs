using System;
using System.Runtime.InteropServices;
namespace OpenWeb
{
	internal class WinHTTPNative
	{
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WINHTTP_AUTOPROXY_OPTIONS
		{
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
			public int dwFlags;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
			public int dwAutoDetectFlags;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
			public string lpszAutoConfigUrl;
			public System.IntPtr lpvReserved;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
			public int dwReserved;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
			public bool fAutoLoginIfChallenged;
		}
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WINHTTP_PROXY_INFO
		{
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
			public int dwAccessType;
			public System.IntPtr lpszProxy;
			public System.IntPtr lpszProxyBypass;
		}
		internal const int WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;
		internal const int WINHTTP_ACCESS_TYPE_NO_PROXY = 1;
		internal const int WINHTTP_ACCESS_TYPE_NAMED_PROXY = 3;
		internal const int WINHTTP_AUTOPROXY_AUTO_DETECT = 1;
		internal const int WINHTTP_AUTOPROXY_CONFIG_URL = 2;
		internal const int WINHTTP_AUTOPROXY_RUN_INPROCESS = 65536;
		internal const int WINHTTP_AUTOPROXY_RUN_OUTPROCESS_ONLY = 131072;
		internal const int WINHTTP_AUTO_DETECT_TYPE_DHCP = 1;
		internal const int WINHTTP_AUTO_DETECT_TYPE_DNS_A = 2;
		internal const int ERROR_WINHTTP_LOGIN_FAILURE = 12015;
		internal const int ERROR_WINHTTP_UNABLE_TO_DOWNLOAD_SCRIPT = 12167;
		internal const int ERROR_WINHTTP_UNRECOGNIZED_SCHEME = 12006;
		internal const int ERROR_WINHTTP_AUTODETECTION_FAILED = 12180;
		internal const int ERROR_WINHTTP_BAD_AUTO_PROXY_SCRIPT = 12166;
		[System.Runtime.InteropServices.DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		internal static extern bool WinHttpGetProxyForUrl(System.IntPtr hSession, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpcwszUrl, [System.Runtime.InteropServices.In] ref WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS pAutoProxyOptions, out WinHTTPNative.WINHTTP_PROXY_INFO pProxyInfo);
		[System.Runtime.InteropServices.DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern System.IntPtr WinHttpOpen([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] [System.Runtime.InteropServices.In] string pwszUserAgent, [System.Runtime.InteropServices.In] int dwAccessType, [System.Runtime.InteropServices.In] System.IntPtr pwszProxyName, [System.Runtime.InteropServices.In] System.IntPtr pwszProxyBypass, [System.Runtime.InteropServices.In] int dwFlags);
		[System.Runtime.InteropServices.DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		internal static extern bool WinHttpDetectAutoProxyConfigUrl([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)] int dwAutoDetectFlags, out System.IntPtr ppwszAutoConfigUrl);
		[System.Runtime.InteropServices.DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		internal static extern bool WinHttpCloseHandle([System.Runtime.InteropServices.In] System.IntPtr hInternet);
	}
}
