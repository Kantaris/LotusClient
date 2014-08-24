using System;
using System.Collections.Generic;
using System.Text;
namespace OpenWeb
{
	internal class WinINETConnectoids
	{
		internal System.Collections.Generic.Dictionary<string, WinINETConnectoid> _oConnectoids = new System.Collections.Generic.Dictionary<string, WinINETConnectoid>();
		public override string ToString()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			stringBuilder.AppendFormat("RAS reports {0} Connectoids\n\n", this._oConnectoids.Count);
			foreach (System.Collections.Generic.KeyValuePair<string, WinINETConnectoid> current in this._oConnectoids)
			{
				stringBuilder.AppendFormat("-= WinINET settings for '{0}' =-\n{1}\n", current.Key, current.Value.oOriginalProxyInfo.ToString());
			}
			return stringBuilder.ToString();
		}
		public WinINETConnectoids()
		{
			string[] connectionNames = RASInfo.GetConnectionNames();
			string[] array = connectionNames;
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew("Collecting information for Connectoid '{0}'", new object[]
					{
						text
					});
				}
				if (!this._oConnectoids.ContainsKey(text))
				{
					try
					{
						WinINETProxyInfo winINETProxyInfo = WinINETProxyInfo.CreateFromNamedConnection(text);
						if (winINETProxyInfo != null)
						{
							WinINETConnectoid winINETConnectoid = new WinINETConnectoid();
							winINETConnectoid.sConnectionName = text;
							if (!string.IsNullOrEmpty(winINETProxyInfo.sHttpProxy) && winINETProxyInfo.sHttpProxy.Contains(CONFIG.sOpenWebListenHostPort))
							{
								winINETProxyInfo.sHttpProxy = (winINETProxyInfo.sHttpsProxy = (winINETProxyInfo.sFtpProxy = null));
								winINETProxyInfo.bUseManualProxies = false;
								winINETProxyInfo.bAllowDirect = true;
							}
							if (!string.IsNullOrEmpty(winINETProxyInfo.sPACScriptLocation) && (winINETProxyInfo.sPACScriptLocation == "file://" + CONFIG.GetPath("Pac") || winINETProxyInfo.sPACScriptLocation == "http://" + CONFIG.sOpenWebListenHostPort + "/proxy.pac"))
							{
								winINETProxyInfo.sPACScriptLocation = null;
							}
							winINETConnectoid.oOriginalProxyInfo = winINETProxyInfo;
							this._oConnectoids.Add(text, winINETConnectoid);
						}
					}
					catch (System.Exception var_6_17D)
					{
					}
				}
			}
		}
		internal WinINETProxyInfo GetDefaultConnectionGatewayInfo()
		{
			string text = CONFIG.sHookConnectionNamed;
			if (string.IsNullOrEmpty(text))
			{
				text = "DefaultLAN";
			}
			WinINETProxyInfo result;
			if (!this._oConnectoids.ContainsKey(text))
			{
				text = "DefaultLAN";
				if (!this._oConnectoids.ContainsKey(text))
				{
					result = WinINETProxyInfo.CreateFromStrings(string.Empty, string.Empty);
					return result;
				}
			}
			result = this._oConnectoids[text].oOriginalProxyInfo;
			return result;
		}
		internal void MarkDefaultLANAsUnhooked()
		{
			foreach (WinINETConnectoid current in this._oConnectoids.Values)
			{
				if (current.sConnectionName == "DefaultLAN")
				{
					current.bIsHooked = false;
					break;
				}
			}
		}
		internal bool MarkUnhookedConnections(string sLookFor)
		{
			bool result;
			if (CONFIG.bIsViewOnly)
			{
				result = false;
			}
			else
			{
				bool flag = false;
				foreach (WinINETConnectoid current in this._oConnectoids.Values)
				{
					if (current.bIsHooked)
					{
						WinINETProxyInfo winINETProxyInfo = WinINETProxyInfo.CreateFromNamedConnection(current.sConnectionName);
						bool flag2 = false;
						if (!winINETProxyInfo.bUseManualProxies)
						{
							flag2 = true;
						}
						else
						{
							string text = winINETProxyInfo.CalculateProxyString();
							if (text != sLookFor && !text.Contains("http=" + sLookFor))
							{
								flag2 = true;
							}
						}
						if (flag2)
						{
							current.bIsHooked = false;
							flag = true;
						}
					}
				}
				result = flag;
			}
			return result;
		}
		internal bool HookConnections(WinINETProxyInfo oNewInfo)
		{
			bool result;
			if (CONFIG.bIsViewOnly)
			{
				result = false;
			}
			else
			{
				bool flag = false;
				foreach (WinINETConnectoid current in this._oConnectoids.Values)
				{
					if ((CONFIG.bHookAllConnections || current.sConnectionName == CONFIG.sHookConnectionNamed) && oNewInfo.SetToWinINET(current.sConnectionName))
					{
						flag = true;
						current.bIsHooked = true;
					}
				}
				result = flag;
			}
			return result;
		}
		internal bool UnhookAllConnections()
		{
			bool result;
			if (CONFIG.bIsViewOnly)
			{
				result = true;
			}
			else
			{
				bool flag = true;
				foreach (WinINETConnectoid current in this._oConnectoids.Values)
				{
					if (current.bIsHooked)
					{
						if (current.oOriginalProxyInfo.SetToWinINET(current.sConnectionName))
						{
							current.bIsHooked = false;
						}
						else
						{
							flag = true;
						}
					}
				}
				result = flag;
			}
			return result;
		}
	}
}
