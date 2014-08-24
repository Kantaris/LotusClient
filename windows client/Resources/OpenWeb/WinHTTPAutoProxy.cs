using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
namespace OpenWeb
{
	internal class WinHTTPAutoProxy : System.IDisposable
	{
		internal int iAutoProxySuccessCount;
		private readonly string _sPACScriptLocation;
		private readonly bool _bUseAutoDiscovery = true;
		private readonly System.IntPtr _hSession;
		private WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS _oAPO;
		private static string GetPACFileText(string sURI)
		{
			string result;
			try
			{
				Uri uri = new Uri(sURI);
				if (!uri.IsFile)
				{
					result = null;
				}
				else
				{
					string localPath = uri.LocalPath;
					if (!System.IO.File.Exists(localPath))
					{
						result = string.Empty;
					}
					else
					{
						result = System.IO.File.ReadAllText(localPath);
					}
				}
			}
			catch (System.Exception var_3_46)
			{
				result = null;
			}
			return result;
		}
		public WinHTTPAutoProxy(bool bAutoDiscover, string sAutoConfigUrl)
		{
			this._bUseAutoDiscovery = bAutoDiscover;
			if (!string.IsNullOrEmpty(sAutoConfigUrl))
			{
				if (sAutoConfigUrl.OICStartsWith("file:") || sAutoConfigUrl.StartsWith("\\\\") || (sAutoConfigUrl.Length > 2 && sAutoConfigUrl[1] == ':'))
				{
					Proxy.sUpstreamPACScript = WinHTTPAutoProxy.GetPACFileText(sAutoConfigUrl);
					if (!string.IsNullOrEmpty(Proxy.sUpstreamPACScript))
					{
						sAutoConfigUrl = "http://" + CONFIG.sOpenWebListenHostPort + "/UpstreamProxy.pac";
					}
				}
				this._sPACScriptLocation = sAutoConfigUrl;
			}
			this._oAPO = WinHTTPAutoProxy.GetAutoProxyOptionsStruct(this._sPACScriptLocation, this._bUseAutoDiscovery);
			this._hSession = WinHTTPNative.WinHttpOpen("OpenWeb", 1, System.IntPtr.Zero, System.IntPtr.Zero, 0);
		}
		public override string ToString()
		{
			string text = null;
			if (this.iAutoProxySuccessCount < 0)
			{
				text = "\tOffline/disabled\n";
			}
			else
			{
				if (this._bUseAutoDiscovery)
				{
					string text2 = WinHTTPAutoProxy.GetWPADUrl();
					if (string.IsNullOrEmpty(text2))
					{
						text2 = "Not detected";
					}
					text = string.Format("\tWPAD: {0}\n", text2);
				}
				if (this._sPACScriptLocation != null)
				{
					text = text + "\tConfig script: " + this._sPACScriptLocation + "\n";
				}
			}
			return text ?? "\tDisabled";
		}
		private static string GetWPADUrl()
		{
			System.IntPtr intPtr;
			bool flag = WinHTTPNative.WinHttpDetectAutoProxyConfigUrl(3, out intPtr);
			if (!flag)
			{
				System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			}
			string result;
			if (flag && System.IntPtr.Zero != intPtr)
			{
				result = System.Runtime.InteropServices.Marshal.PtrToStringUni(intPtr);
			}
			else
			{
				result = string.Empty;
			}
			Utilities.GlobalFreeIfNonZero(intPtr);
			return result;
		}
		private static WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS GetAutoProxyOptionsStruct(string sPAC, bool bUseAutoDetect)
		{
			WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS result = default(WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS);
			if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.gateway.DetermineInProcess", false))
			{
				result.dwFlags = 65536;
			}
			else
			{
				result.dwFlags = 0;
			}
			if (bUseAutoDetect)
			{
				result.dwFlags |= 1;
				result.dwAutoDetectFlags = 3;
			}
			if (sPAC != null)
			{
				result.dwFlags |= 2;
				result.lpszAutoConfigUrl = sPAC;
			}
			result.fAutoLoginIfChallenged = CONFIG.bAutoProxyLogon;
			return result;
		}
		public bool GetAutoProxyForUrl(string sUrl, out IPEndPoint ipepResult)
		{
			int num = 0;
			WinHTTPNative.WINHTTP_PROXY_INFO wINHTTP_PROXY_INFO;
			bool flag = WinHTTPNative.WinHttpGetProxyForUrl(this._hSession, sUrl, ref this._oAPO, out wINHTTP_PROXY_INFO);
			if (!flag)
			{
				num = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			}
			bool result;
			if (flag)
			{
				if (System.IntPtr.Zero != wINHTTP_PROXY_INFO.lpszProxy)
				{
					string sHostAndPort = System.Runtime.InteropServices.Marshal.PtrToStringUni(wINHTTP_PROXY_INFO.lpszProxy);
					ipepResult = Utilities.IPEndPointFromHostPortString(sHostAndPort);
					if (ipepResult == null)
					{
					}
				}
				else
				{
					ipepResult = null;
				}
				Utilities.GlobalFreeIfNonZero(wINHTTP_PROXY_INFO.lpszProxy);
				Utilities.GlobalFreeIfNonZero(wINHTTP_PROXY_INFO.lpszProxyBypass);
				result = true;
			}
			else
			{
				int num2 = num;
				if (num2 <= 12015)
				{
					if (num2 != 12006)
					{
						if (num2 == 12015)
						{
						}
					}
				}
				else
				{
					switch (num2)
					{
					case 12166:
						break;
					case 12167:
						break;
					default:
						if (num2 == 12180)
						{
						}
						break;
					}
				}
				Utilities.GlobalFreeIfNonZero(wINHTTP_PROXY_INFO.lpszProxy);
				Utilities.GlobalFreeIfNonZero(wINHTTP_PROXY_INFO.lpszProxyBypass);
				ipepResult = null;
				result = false;
			}
			return result;
		}
		public void Dispose()
		{
			WinHTTPNative.WinHttpCloseHandle(this._hSession);
		}
	}
}
