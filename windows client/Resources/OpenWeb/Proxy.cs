using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace OpenWeb
{
	public class Proxy : System.IDisposable
	{
		internal static string sUpstreamPACScript;
		internal RegistryWatcher oRegistryWatcher;
		private string _sHTTPSHostname;
		private X509Certificate2 _oHTTPSCertificate;
		internal WinINETConnectoids oAllConnectoids;
		private WinINETProxyInfo piSystemGateway;
		private WinHTTPAutoProxy oAutoProxy;
		private IPEndPoint _ipepFtpGateway;
		private IPEndPoint _ipepHttpGateway;
		private IPEndPoint _ipepHttpsGateway;
		internal IPEndPoint _DefaultEgressEndPoint;
		private PreferenceBag.PrefWatcher? watcherPrefNotify = null;
		internal static PipePool htServerPipePool = new PipePool();
		private Socket oAcceptor;
		private bool _bIsAttached;
		private bool _bDetaching;
		private ProxyBypassList oBypassList;
		public event System.EventHandler DetachedUnexpectedly;
		public int ListenPort
		{
			get
			{
				int result;
				if (this.oAcceptor != null)
				{
					result = (this.oAcceptor.LocalEndPoint as IPEndPoint).Port;
				}
				else
				{
					result = 0;
				}
				return result;
			}
		}
		public bool IsAttached
		{
			get
			{
				return this._bIsAttached;
			}
			set
			{
				if (value)
				{
					this.Attach();
				}
				else
				{
					this.Detach();
				}
			}
		}
		public override string ToString()
		{
			return string.Format("Proxy instance is listening for requests on Port #{0}. HTTPS SubjectCN: {1}\n\n{2}", this.ListenPort, this._sHTTPSHostname ?? "<None>", Proxy.htServerPipePool.InspectPool());
		}
		protected virtual void OnDetachedUnexpectedly()
		{
			System.EventHandler detachedUnexpectedly = this.DetachedUnexpectedly;
			if (detachedUnexpectedly != null)
			{
				detachedUnexpectedly(this, null);
			}
		}
		internal Proxy(bool bIsPrimary)
		{
			if (bIsPrimary)
			{
				try
				{
					NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(this.NetworkChange_NetworkAvailabilityChanged);
					NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(this.NetworkChange_NetworkAddressChanged);
				}
				catch (System.Exception)
				{
				}
				try
				{
					this.watcherPrefNotify = new PreferenceBag.PrefWatcher?(OpenWebApplication.Prefs.AddWatcher("OpenWeb.network", new System.EventHandler<PrefChangeEventArgs>(this.onNetworkPrefsChange)));
					this.SetDefaultEgressEndPoint(OpenWebApplication.Prefs["OpenWeb.network.egress.ip"]);
					CONFIG.SetNoDecryptList(OpenWebApplication.Prefs["OpenWeb.network.https.NoDecryptionHosts"]);
					CONFIG.sOpenWebListenHostPort = string.Format("{0}:{1}", OpenWebApplication.Prefs.GetStringPref("OpenWeb.network.proxy.RegistrationHostName", "127.0.0.1").ToLower(), CONFIG.ListenPort);
					ClientChatter._cbClientReadBuffer = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.sockets.ClientReadBufferSize", 8192);
					ServerChatter._cbServerReadBuffer = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.sockets.ServerReadBufferSize", 32768);
				}
				catch (System.Exception)
				{
				}
			}
		}
		private void SetDefaultEgressEndPoint(string sEgressIP)
		{
			if (string.IsNullOrEmpty(sEgressIP))
			{
				this._DefaultEgressEndPoint = null;
			}
			else
			{
				IPAddress address;
				if (IPAddress.TryParse(sEgressIP, out address))
				{
					this._DefaultEgressEndPoint = new IPEndPoint(address, 0);
				}
				else
				{
					this._DefaultEgressEndPoint = null;
				}
			}
		}
		private void onNetworkPrefsChange(object sender, PrefChangeEventArgs oPCE)
		{
			if (oPCE.PrefName.OICStartsWith("OpenWeb.network.timeouts."))
			{
				if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.serverpipe.send.initial"))
				{
					ServerPipe._timeoutSendInitial = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.send.initial", -1);
				}
				else
				{
					if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.serverpipe.send.reuse"))
					{
						ServerPipe._timeoutSendReused = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.send.reuse", -1);
					}
					else
					{
						if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.serverpipe.receive.initial"))
						{
							ServerPipe._timeoutReceiveInitial = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.receive.initial", -1);
						}
						else
						{
							if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.serverpipe.receive.reuse"))
							{
								ServerPipe._timeoutReceiveReused = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.receive.reuse", -1);
							}
							else
							{
								if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.serverpipe.reuse"))
								{
									PipePool.MSEC_PIPE_POOLED_LIFETIME = (uint)OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.reuse", 120000);
								}
								else
								{
									if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.clientpipe.receive.initial"))
									{
										ClientPipe._timeoutReceiveInitial = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.clientpipe.receive.initial", 60000);
									}
									else
									{
										if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.clientpipe.receive.reuse"))
										{
											ClientPipe._timeoutReceiveReused = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.clientpipe.receive.reuse", 30000);
										}
										else
										{
											if (oPCE.PrefName.OICEquals("OpenWeb.network.timeouts.dnscache"))
											{
												DNSResolver.MSEC_DNS_CACHE_LIFETIME = (ulong)((long)OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.dnscache", 150000));
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				if (oPCE.PrefName.OICEquals("OpenWeb.network.sockets.ClientReadBufferSize"))
				{
					ClientChatter._cbClientReadBuffer = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.sockets.ClientReadBufferSize", 8192);
				}
				else
				{
					if (oPCE.PrefName.OICEquals("OpenWeb.network.sockets.ServerReadBufferSize"))
					{
						ServerChatter._cbServerReadBuffer = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.sockets.ServerReadBufferSize", 32768);
					}
					else
					{
						if (oPCE.PrefName.OICEquals("OpenWeb.network.egress.ip"))
						{
							this.SetDefaultEgressEndPoint(oPCE.ValueString);
						}
						else
						{
							if (oPCE.PrefName.OICEquals("OpenWeb.network.https.NoDecryptionHosts"))
							{
								CONFIG.SetNoDecryptList(oPCE.ValueString);
							}
							else
							{
								if (oPCE.PrefName.OICEquals("OpenWeb.network.proxy.RegistrationHostName"))
								{
									CONFIG.sOpenWebListenHostPort = string.Format("{0}:{1}", OpenWebApplication.Prefs.GetStringPref("OpenWeb.network.proxy.RegistrationHostName", "127.0.0.1").ToLower(), CONFIG.ListenPort);
								}
							}
						}
					}
				}
			}
		}
		private void NetworkChange_NetworkAddressChanged(object sender, System.EventArgs e)
		{
			try
			{
				DNSResolver.ClearCache();
				if (this.oAutoProxy != null)
				{
					this.oAutoProxy.iAutoProxySuccessCount = 0;
				}
				if (this.piSystemGateway != null && this.piSystemGateway.bUseManualProxies)
				{
					this._DetermineGatewayIPEndPoints();
				}
			}
			catch (System.Exception eX)
			{
				OpenWebApplication.ReportException(eX);
			}
		}
		private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			try
			{
				this.PurgeServerPipePool();
			}
			catch (System.Exception)
			{
			}
		}
		[CodeDescription("Send a custom request through the proxy, blocking until it completes (or aborts).")]
		public Session SendRequestAndWait(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags, System.EventHandler<StateChangeEventArgs> onStateChange)
		{
			System.Threading.ManualResetEvent oMRE = new System.Threading.ManualResetEvent(false);
			Session session = this.SendRequest(oHeaders, arrRequestBodyBytes, oNewFlags, onStateChange);
			session.OnStateChanged += delegate(object o, StateChangeEventArgs scea)
			{
				if (scea.newState >= SessionStates.Done)
				{
					OpenWebApplication.DebugSpew("SendRequestAndWait Session #{0} reached state {1}", new object[]
					{
						(o as Session).id,
						scea.newState
					});
					oMRE.Set();
				}
			};
			oMRE.WaitOne();
			return session;
		}
		[CodeDescription("Send a custom request through the proxy. Hook the OnStateChanged event of the returned Session to monitor progress")]
		public Session SendRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags)
		{
			return this.SendRequest(oHeaders, arrRequestBodyBytes, oNewFlags, null);
		}
		[CodeDescription("Send a custom request through the proxy. Hook the OnStateChanged event of the returned Session to monitor progress")]
		public Session SendRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags, System.EventHandler<StateChangeEventArgs> onStateChange)
		{
			if (oHeaders.ExistsAndContains("OpenWeb-Encoding", "base64"))
			{
				oHeaders.Remove("OpenWeb-Encoding");
				if (!Utilities.IsNullOrEmpty(arrRequestBodyBytes))
				{
					arrRequestBodyBytes = System.Convert.FromBase64String(System.Text.Encoding.ASCII.GetString(arrRequestBodyBytes));
					if (oNewFlags == null)
					{
						oNewFlags = new StringDictionary();
					}
					oNewFlags["x-Builder-FixContentLength"] = "CFE-required";
				}
			}
			if (oHeaders.Exists("OpenWeb-Host"))
			{
				if (oNewFlags == null)
				{
					oNewFlags = new StringDictionary();
				}
				oNewFlags["x-OverrideHost"] = oHeaders["OpenWeb-Host"];
				oNewFlags["X-IgnoreCertCNMismatch"] = "Overrode HOST";
				oHeaders.Remove("OpenWeb-Host");
			}
			if (oNewFlags != null && oNewFlags.ContainsKey("x-Builder-FixContentLength"))
			{
				if (arrRequestBodyBytes != null && !oHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
				{
					if (!Utilities.HTTPMethodAllowsBody(oHeaders.HTTPMethod) && arrRequestBodyBytes.Length == 0)
					{
						oHeaders.Remove("Content-Length");
					}
					else
					{
						oHeaders["Content-Length"] = arrRequestBodyBytes.LongLength.ToString();
					}
				}
				else
				{
					oHeaders.Remove("Content-Length");
				}
			}
			Session session = new Session((HTTPRequestHeaders)oHeaders.Clone(), arrRequestBodyBytes);
			session.SetBitFlag(SessionFlags.RequestGeneratedByOpenWeb, true);
			if (onStateChange != null)
			{
				session.OnStateChanged += onStateChange;
			}
			if (oNewFlags != null && oNewFlags.Count > 0)
			{
				foreach (System.Collections.DictionaryEntry dictionaryEntry in oNewFlags)
				{
					session.oFlags[(string)dictionaryEntry.Key] = oNewFlags[(string)dictionaryEntry.Key];
				}
			}
			if (session.oFlags.ContainsKey("x-AutoAuth"))
			{
				string inStr = session.oRequest.headers["Authorization"];
				if (inStr.OICContains("NTLM") || inStr.OICContains("Negotiate") || inStr.OICContains("Digest"))
				{
					session.oRequest.headers.Remove("Authorization");
				}
				inStr = session.oRequest.headers["Proxy-Authorization"];
				if (inStr.OICContains("NTLM") || inStr.OICContains("Negotiate") || inStr.OICContains("Digest"))
				{
					session.oRequest.headers.Remove("Proxy-Authorization");
				}
			}
			session.ExecuteUponAsyncRequest();
			return session;
		}
		public Session SendRequest(string sRequest, StringDictionary oNewFlags)
		{
			byte[] bytes = CONFIG.oHeaderEncoding.GetBytes(sRequest);
			int count;
			int num;
			HTTPHeaderParseWarnings hTTPHeaderParseWarnings;
			if (!Parser.FindEntityBodyOffsetFromArray(bytes, out count, out num, out hTTPHeaderParseWarnings))
			{
				throw new System.ArgumentException("sRequest did not represent a valid HTTP request", "sRequest");
			}
			string sHeaders = CONFIG.oHeaderEncoding.GetString(bytes, 0, count) + "\r\n\r\n";
			HTTPRequestHeaders hTTPRequestHeaders = new HTTPRequestHeaders();
			if (!hTTPRequestHeaders.AssignFromString(sHeaders))
			{
				throw new System.ArgumentException("sRequest did not contain valid HTTP headers", "sRequest");
			}
			byte[] array;
			if (1 > bytes.Length - num)
			{
				array = Utilities.emptyByteArray;
			}
			else
			{
				array = new byte[bytes.Length - num];
				System.Buffer.BlockCopy(bytes, num, array, 0, array.Length);
			}
			return this.SendRequest(hTTPRequestHeaders, array, oNewFlags, null);
		}
		[System.Obsolete("This overload of InjectCustomRequest is obsolete. Use a different version.", true)]
		public void InjectCustomRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, bool bRunRequestRules, bool bViewResult)
		{
			StringDictionary stringDictionary = new StringDictionary();
			stringDictionary["x-From-Builder"] = "true";
			if (bViewResult)
			{
				stringDictionary["x-Builder-Inspect"] = "1";
			}
			this.InjectCustomRequest(oHeaders, arrRequestBodyBytes, stringDictionary);
		}
		public void InjectCustomRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags)
		{
			this.SendRequest(oHeaders, arrRequestBodyBytes, oNewFlags);
		}
		public void InjectCustomRequest(string sRequest, StringDictionary oNewFlags)
		{
			this.SendRequest(sRequest, oNewFlags);
		}
		public void InjectCustomRequest(string sRequest)
		{
			if (this.oAcceptor == null)
			{
				this.InjectCustomRequest(sRequest, null);
			}
			else
			{
				Socket socket = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				socket.Connect(new IPEndPoint(IPAddress.Loopback, CONFIG.ListenPort));
				socket.Send(System.Text.Encoding.UTF8.GetBytes(sRequest));
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
		}
		public IPEndPoint FindGatewayForOrigin(string sURIScheme, string sHostAndPort)
		{
			IPEndPoint result;
			if (string.IsNullOrEmpty(sURIScheme))
			{
				result = null;
			}
			else
			{
				if (string.IsNullOrEmpty(sHostAndPort))
				{
					result = null;
				}
				else
				{
					if (CONFIG.UpstreamGateway == GatewayType.None)
					{
						result = null;
					}
					else
					{
						if (Utilities.isLocalhost(sHostAndPort))
						{
							result = null;
						}
						else
						{
							if (sURIScheme.OICEquals("http"))
							{
								if (sHostAndPort.EndsWith(":80", System.StringComparison.Ordinal))
								{
									sHostAndPort = sHostAndPort.Substring(0, sHostAndPort.Length - 3);
								}
							}
							else
							{
								if (sURIScheme.OICEquals("https"))
								{
									if (sHostAndPort.EndsWith(":443", System.StringComparison.Ordinal))
									{
										sHostAndPort = sHostAndPort.Substring(0, sHostAndPort.Length - 4);
									}
								}
								else
								{
									if (sURIScheme.OICEquals("ftp") && sHostAndPort.EndsWith(":21", System.StringComparison.Ordinal))
									{
										sHostAndPort = sHostAndPort.Substring(0, sHostAndPort.Length - 3);
									}
								}
							}
							if (this.oAutoProxy != null && this.oAutoProxy.iAutoProxySuccessCount > -1)
							{
								IPEndPoint iPEndPoint;
								if (this.oAutoProxy.GetAutoProxyForUrl(sURIScheme + "://" + sHostAndPort, out iPEndPoint))
								{
									this.oAutoProxy.iAutoProxySuccessCount = 1;
									result = iPEndPoint;
									return result;
								}
								if (this.oAutoProxy.iAutoProxySuccessCount == 0 && !OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.gateway.UseFailedAutoProxy", false))
								{
									this.oAutoProxy.iAutoProxySuccessCount = -1;
								}
							}
							if (this.oBypassList != null && this.oBypassList.IsBypass(sURIScheme, sHostAndPort))
							{
								result = null;
							}
							else
							{
								if (sURIScheme.OICEquals("http"))
								{
									result = this._ipepHttpGateway;
								}
								else
								{
									if (sURIScheme.OICEquals("https"))
									{
										result = this._ipepHttpsGateway;
									}
									else
									{
										if (sURIScheme.OICEquals("ftp"))
										{
											result = this._ipepFtpGateway;
										}
										else
										{
											result = null;
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
		private void AcceptConnection(System.IAsyncResult ar)
		{
			try
			{
				ProxyExecuteParams state = new ProxyExecuteParams(this.oAcceptor.EndAccept(ar), this._oHTTPSCertificate);
				System.Threading.ThreadPool.UnsafeQueueUserWorkItem(new System.Threading.WaitCallback(Session.CreateAndExecute), state);
			}
			catch (System.ObjectDisposedException)
			{
				return;
			}
			catch (System.Exception)
			{
			}
			try
			{
				this.oAcceptor.BeginAccept(new System.AsyncCallback(this.AcceptConnection), null);
			}
			catch (System.Exception)
			{
			}
		}
		public bool Attach()
		{
			return this.Attach(false);
		}
		private void ProxyRegistryKeysChanged(object sender, System.EventArgs e)
		{
			if (this._bIsAttached && !this._bDetaching)
			{
				if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.proxy.WatchRegistry", true))
				{
					ScheduledTasks.ScheduleWork("VerifyAttached", 50u, new SimpleEventHandler(this.VerifyAttached));
				}
			}
		}
		internal void VerifyAttached()
		{
			bool flag = true;
			try
			{
				if (this.oAllConnectoids != null)
				{
					flag = !this.oAllConnectoids.MarkUnhookedConnections(CONFIG.sOpenWebListenHostPort);
				}
			}
			catch (System.Exception)
			{
			}
			if (flag)
			{
				Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", false);
				if (registryKey != null)
				{
					if (1 != Utilities.GetRegistryInt(registryKey, "ProxyEnable", 0))
					{
						flag = false;
					}
					string text = registryKey.GetValue("ProxyServer") as string;
					if (string.IsNullOrEmpty(text))
					{
						flag = false;
					}
					else
					{
						if (!text.OICEquals(CONFIG.sOpenWebListenHostPort) && !text.OICContains("http=" + CONFIG.sOpenWebListenHostPort))
						{
							flag = false;
						}
					}
					registryKey.Close();
					if (!flag && this.oAllConnectoids != null)
					{
						this.oAllConnectoids.MarkDefaultLANAsUnhooked();
					}
				}
			}
			if (!flag)
			{
				this.OnDetachedUnexpectedly();
			}
		}
		internal bool Attach(bool bCollectGWInfo)
		{
			bool result;
			if (this._bIsAttached)
			{
				result = true;
			}
			else
			{
				if (CONFIG.bIsViewOnly)
				{
					result = false;
				}
				else
				{
					if (bCollectGWInfo)
					{
						this.CollectConnectoidAndGatewayInfo();
					}
					WinINETProxyInfo winINETProxyInfo = new WinINETProxyInfo();
					winINETProxyInfo.bUseManualProxies = true;
					winINETProxyInfo.bAllowDirect = true;
					winINETProxyInfo.sHttpProxy = CONFIG.sOpenWebListenHostPort;
					if (CONFIG.bCaptureCONNECT)
					{
						winINETProxyInfo.sHttpsProxy = CONFIG.sOpenWebListenHostPort;
					}
					else
					{
						if (this.piSystemGateway != null && this.piSystemGateway.bUseManualProxies)
						{
							winINETProxyInfo.sHttpsProxy = this.piSystemGateway.sHttpsProxy;
						}
					}
					if (CONFIG.bCaptureFTP)
					{
						winINETProxyInfo.sFtpProxy = CONFIG.sOpenWebListenHostPort;
					}
					else
					{
						if (this.piSystemGateway != null && this.piSystemGateway.bUseManualProxies)
						{
							winINETProxyInfo.sFtpProxy = this.piSystemGateway.sFtpProxy;
						}
					}
					if (this.piSystemGateway != null && this.piSystemGateway.bUseManualProxies)
					{
						winINETProxyInfo.sSocksProxy = this.piSystemGateway.sSocksProxy;
					}
					winINETProxyInfo.sHostsThatBypass = CONFIG.sHostsThatBypassOpenWeb;
					if (CONFIG.bHookWithPAC)
					{
						if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.proxy.pacfile.usefileprotocol", true))
						{
							winINETProxyInfo.sPACScriptLocation = "file://" + CONFIG.GetPath("Pac");
						}
						else
						{
							winINETProxyInfo.sPACScriptLocation = "http://" + CONFIG.sOpenWebListenHostPort + "/proxy.pac";
						}
					}
					if (this.oAllConnectoids == null)
					{
						this.CollectConnectoidAndGatewayInfo();
					}
					if (this.oAllConnectoids.HookConnections(winINETProxyInfo))
					{
						this._bIsAttached = true;
						OpenWebApplication.OnOpenWebAttach();
						this.WriteAutoProxyPACFile(true);
						if (this.oRegistryWatcher == null && OpenWebApplication.Prefs.GetBoolPref("OpenWeb.proxy.WatchRegistry", true))
						{
							this.oRegistryWatcher = RegistryWatcher.WatchKey(Microsoft.Win32.RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", new System.EventHandler(this.ProxyRegistryKeysChanged));
						}
						Proxy._setDynamicRegistryKey(true);
						result = true;
					}
					else
					{
						OpenWebApplication.DoNotifyUser("Failed to register OpenWeb as the system proxy.", "Error");
						Proxy._setDynamicRegistryKey(false);
						result = false;
					}
				}
			}
			return result;
		}
		internal void CollectConnectoidAndGatewayInfo()
		{
			try
			{
				this.oAllConnectoids = new WinINETConnectoids();
				this.RefreshUpstreamGatewayInformation();
			}
			catch (System.Exception eX)
			{
				OpenWebApplication.ReportException(eX, "System Error");
			}
		}
		private static IPEndPoint GetFirstRespondingEndpoint(string sHostPortList)
		{
			IPEndPoint result;
			if (Utilities.IsNullOrWhiteSpace(sHostPortList))
			{
				result = null;
			}
			else
			{
				sHostPortList = Utilities.TrimAfter(sHostPortList, ';');
				IPEndPoint iPEndPoint = null;
				int port = 80;
				string sRemoteHost;
				Utilities.CrackHostAndPort(sHostPortList, out sRemoteHost, ref port);
				IPAddress[] iPAddressList;
				IPEndPoint iPEndPoint2;
				try
				{
					iPAddressList = DNSResolver.GetIPAddressList(sRemoteHost, true, null);
				}
				catch
				{
					iPEndPoint2 = null;
					result = iPEndPoint2;
					return result;
				}
				try
				{
					IPAddress[] array = iPAddressList;
					for (int i = 0; i < array.Length; i++)
					{
						IPAddress iPAddress = array[i];
						try
						{
							using (Socket socket = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
							{
								socket.NoDelay = true;
								if (OpenWebApplication.oProxy._DefaultEgressEndPoint != null)
								{
									socket.Bind(OpenWebApplication.oProxy._DefaultEgressEndPoint);
								}
								socket.Connect(iPAddress, port);
								iPEndPoint = new IPEndPoint(iPAddress, port);
							}
							break;
						}
						catch (System.Exception var_9_D3)
						{
							if (!OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.dns.fallback", true))
							{
								break;
							}
						}
					}
					iPEndPoint2 = iPEndPoint;
				}
				catch (System.Exception)
				{
					iPEndPoint2 = null;
				}
				result = iPEndPoint2;
			}
			return result;
		}
		private void _DetermineGatewayIPEndPoints()
		{
			this._ipepHttpGateway = Proxy.GetFirstRespondingEndpoint(this.piSystemGateway.sHttpProxy);
			if (this.piSystemGateway.sHttpsProxy == this.piSystemGateway.sHttpProxy)
			{
				this._ipepHttpsGateway = this._ipepHttpGateway;
			}
			else
			{
				this._ipepHttpsGateway = Proxy.GetFirstRespondingEndpoint(this.piSystemGateway.sHttpsProxy);
			}
			if (this.piSystemGateway.sFtpProxy == this.piSystemGateway.sHttpProxy)
			{
				this._ipepFtpGateway = this._ipepHttpGateway;
			}
			else
			{
				this._ipepFtpGateway = Proxy.GetFirstRespondingEndpoint(this.piSystemGateway.sFtpProxy);
			}
		}
		private static void _setDynamicRegistryKey(bool bAttached)
		{
			if (!CONFIG.bIsViewOnly)
			{
				try
				{
					Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(CONFIG.GetRegPath("Dynamic"));
					if (registryKey != null)
					{
						registryKey.SetValue("Attached", bAttached ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
						registryKey.Close();
					}
				}
				catch (System.Exception var_1_53)
				{
				}
			}
		}
		public bool Detach()
		{
			return this.Detach(false);
		}
		internal bool Detach(bool bDontCheckIfAttached)
		{
			bool result;
			if (!bDontCheckIfAttached && !this._bIsAttached)
			{
				result = true;
			}
			else
			{
				if (CONFIG.bIsViewOnly)
				{
					result = true;
				}
				else
				{
					try
					{
						this._bDetaching = true;
						Proxy._setDynamicRegistryKey(false);
						if (!this.oAllConnectoids.UnhookAllConnections())
						{
							result = false;
							return result;
						}
						this._bIsAttached = false;
						OpenWebApplication.OnOpenWebDetach();
						this.WriteAutoProxyPACFile(false);
					}
					finally
					{
						this._bDetaching = false;
					}
					result = true;
				}
			}
			return result;
		}
		internal string _GetUpstreamPACScriptText()
		{
			return Proxy.sUpstreamPACScript;
		}
		internal string _GetPACScriptText(bool bUseOpenWeb)
		{
			string str;
			if (bUseOpenWeb)
			{
				str = OpenWebApplication.Prefs.GetStringPref("OpenWeb.proxy.pacfile.text", "return 'PROXY " + CONFIG.sOpenWebListenHostPort + "';");
			}
			else
			{
				str = "return 'DIRECT';";
			}
			return "//Autogenerated file; do not edit. Rewritten on attach and detach of OpenWeb.\r\n//This Automatic Proxy Configuration script can be used by non-WinINET browsers.\r\nfunction FindProxyForURL(url, host){\r\n  " + str + "\r\n}";
		}
		private void WriteAutoProxyPACFile(bool bUseOpenWeb)
		{
			bool bIsViewOnly = CONFIG.bIsViewOnly;
		}
		internal void Stop()
		{
			if (this.oAcceptor != null)
			{
				try
				{
					this.oAcceptor.LingerState = new LingerOption(true, 0);
					this.oAcceptor.Close();
				}
				catch (System.Exception ex)
				{
					Trace.WriteLine("oProxy.Dispose threw an exception: " + ex.Message);
				}
			}
		}
		internal bool Start(int iPort, bool bAllowRemote)
		{
			bool result;
			if (CONFIG.bIsViewOnly && DialogResult.No == MessageBox.Show("This instance is running in OpenWeb's Viewer Mode. Do you want to start the listener anyway?", "Warning: Viewer Mode", MessageBoxButtons.YesNo))
			{
				result = false;
			}
			else
			{
				bool flag = false;
				try
				{
					flag = (bAllowRemote && CONFIG.bEnableIPv6 && Socket.OSSupportsIPv6);
				}
				catch (System.Exception var_1_49)
				{
					bool flag2 = 0 == 0;
				}
				try
				{
					if (flag)
					{
						this.oAcceptor = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
						if (System.Environment.OSVersion.Version.Major > 5)
						{
							this.oAcceptor.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
						}
					}
					else
					{
						this.oAcceptor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					}
					if (CONFIG.ForceExclusivePort)
					{
						this.oAcceptor.ExclusiveAddressUse = true;
					}
					if (bAllowRemote)
					{
						if (flag)
						{
							this.oAcceptor.Bind(new IPEndPoint(IPAddress.IPv6Any, iPort));
						}
						else
						{
							this.oAcceptor.Bind(new IPEndPoint(IPAddress.Any, iPort));
						}
					}
					else
					{
						this.oAcceptor.Bind(new IPEndPoint(IPAddress.Loopback, iPort));
					}
					this.oAcceptor.Listen(50);
				}
				catch (SocketException ex)
				{
					string text = string.Empty;
					string sTitle = "OpenWeb Cannot Listen";
					int errorCode = ex.ErrorCode;
					if (errorCode != 10013)
					{
						switch (errorCode)
						{
						case 10047:
						case 10049:
							if (flag)
							{
								text = "\nThis often means that you've enabled IPv6 support inside Tools > OpenWeb Options, but your computer has IPv6 disabled.";
								goto IL_1D2;
							}
							goto IL_1D2;
						case 10048:
							break;
						default:
							goto IL_1D2;
						}
					}
					text = string.Format("\nThis is usually due to another service running on this port. Use NETSTAT -AB at a command prompt to identify it.\n{0}", (iPort == CONFIG.ListenPort) ? "If you don't want to stop using the other program, simply change the port used by OpenWeb.\nClick Tools > OpenWeb Options > Connections, select a new port, and restart OpenWeb." : string.Empty);
					sTitle = "Port in Use";
					IL_1D2:
					this.oAcceptor = null;
					OpenWebApplication.DoNotifyUser(string.Format("Unable to bind to port [{0}]. ErrorCode: {1}.\n{2}\n\n{3}\n\n{4}", new object[]
					{
						iPort,
						ex.ErrorCode,
						text,
						ex.ToString(),
						string.Concat(new object[]
						{
							"OpenWeb v",
							Application.ProductVersion,
							" [.NET ",
							System.Environment.Version,
							" on ",
							System.Environment.OSVersion.VersionString,
							"]"
						})
					}), sTitle, MessageBoxIcon.Hand);
					bool flag3 = false;
					result = flag3;
					return result;
				}
				catch (System.Exception eX)
				{
					this.oAcceptor = null;
					OpenWebApplication.ReportException(eX);
					bool flag3 = false;
					result = flag3;
					return result;
				}
				try
				{
					this.oAcceptor.BeginAccept(new System.AsyncCallback(this.AcceptConnection), null);
				}
				catch (System.Exception var_8_2B5)
				{
					this.oAcceptor = null;
					bool flag3 = false;
					result = flag3;
					return result;
				}
				result = true;
			}
			return result;
		}
		public void Dispose()
		{
			if (this.watcherPrefNotify.HasValue)
			{
				OpenWebApplication.Prefs.RemoveWatcher(this.watcherPrefNotify.Value);
			}
			if (this.oRegistryWatcher != null)
			{
				this.oRegistryWatcher.Dispose();
			}
			this.Stop();
			if (this.oAutoProxy != null)
			{
				this.oAutoProxy.Dispose();
				this.oAutoProxy = null;
			}
		}
		public void PurgeServerPipePool()
		{
			Proxy.htServerPipePool.Clear();
		}
		public void AssignEndpointCertificate(X509Certificate2 certHTTPS)
		{
			this._oHTTPSCertificate = certHTTPS;
			if (certHTTPS != null)
			{
				this._sHTTPSHostname = certHTTPS.Subject;
			}
			else
			{
				this._sHTTPSHostname = null;
			}
		}
		internal void RefreshUpstreamGatewayInformation()
		{
			this._ipepFtpGateway = (this._ipepHttpGateway = (this._ipepHttpsGateway = null));
			this.piSystemGateway = null;
			this.oBypassList = null;
			if (this.oAutoProxy != null)
			{
				this.oAutoProxy.Dispose();
				this.oAutoProxy = null;
			}
			switch (CONFIG.UpstreamGateway)
			{
			case GatewayType.Manual:
			{
				WinINETProxyInfo oPI = WinINETProxyInfo.CreateFromStrings(OpenWebApplication.Prefs.GetStringPref("OpenWeb.network.gateway.proxies", string.Empty), OpenWebApplication.Prefs.GetStringPref("OpenWeb.network.gateway.exceptions", string.Empty));
				this.AssignGateway(oPI);
				break;
			}
			case GatewayType.System:
				this.AssignGateway(this.oAllConnectoids.GetDefaultConnectionGatewayInfo());
				break;
			case GatewayType.WPAD:
				this.oAutoProxy = new WinHTTPAutoProxy(true, null);
				break;
			}
		}
		private void AssignGateway(WinINETProxyInfo oPI)
		{
			this.piSystemGateway = oPI;
			if (this.piSystemGateway.bAutoDetect || this.piSystemGateway.sPACScriptLocation != null)
			{
				this.oAutoProxy = new WinHTTPAutoProxy(this.piSystemGateway.bAutoDetect, this.piSystemGateway.sPACScriptLocation);
			}
			if (this.piSystemGateway.bUseManualProxies)
			{
				this._DetermineGatewayIPEndPoints();
				if (!string.IsNullOrEmpty(this.piSystemGateway.sHostsThatBypass))
				{
					this.oBypassList = new ProxyBypassList(this.piSystemGateway.sHostsThatBypass);
					if (!this.oBypassList.HasEntries)
					{
						this.oBypassList = null;
					}
				}
			}
		}
		internal bool ActAsHTTPSEndpointForHostname(string sHTTPSHostname)
		{
			bool result;
			try
			{
				if (string.IsNullOrEmpty(sHTTPSHostname))
				{
					throw new System.ArgumentException();
				}
				this._sHTTPSHostname = this._oHTTPSCertificate.Subject;
				result = true;
				return result;
			}
			catch (System.Exception)
			{
				this._oHTTPSCertificate = null;
				this._sHTTPSHostname = null;
			}
			result = false;
			return result;
		}
		internal string GetGatewayInformation()
		{
			string result;
			if (OpenWebApplication.oProxy.oAutoProxy != null)
			{
				result = string.Format("Gateway: Auto-Config\n{0}", OpenWebApplication.oProxy.oAutoProxy.ToString());
			}
			else
			{
				IPEndPoint iPEndPoint = this.FindGatewayForOrigin("http", "OpenWeb2.com");
				if (iPEndPoint != null)
				{
					result = string.Format("Gateway: {0}:{1}\n", iPEndPoint.Address.ToString(), iPEndPoint.Port.ToString());
				}
				else
				{
					result = string.Format("Gateway: No Gateway\n", new object[0]);
				}
			}
			return result;
		}
		internal void ShowGatewayInformation()
		{
			if (this.piSystemGateway == null && this.oAutoProxy == null)
			{
				MessageBox.Show("No upstream gateway proxy is configured.", "No Upstream Gateway");
			}
			else
			{
				if (this.piSystemGateway != null && this.piSystemGateway.bUseManualProxies)
				{
					System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
					stringBuilder.Append(this.piSystemGateway.ToString());
					if (!string.IsNullOrEmpty(this.piSystemGateway.sHttpProxy) && this._ipepHttpGateway == null)
					{
						stringBuilder.AppendLine("\nWARNING: HTTP Gateway specified was unreachable and is being ignored.");
					}
					if (!string.IsNullOrEmpty(this.piSystemGateway.sHttpsProxy) && this._ipepHttpsGateway == null)
					{
						stringBuilder.AppendLine("\nWARNING: HTTPS Gateway specified was unreachable and is being ignored.");
					}
					if (!string.IsNullOrEmpty(this.piSystemGateway.sFtpProxy) && this._ipepFtpGateway == null)
					{
						stringBuilder.AppendLine("\nWARNING: FTP Gateway specified was unreachable and is being ignored.");
					}
					MessageBox.Show(stringBuilder.ToString(), "Manually-Configured Gateway");
				}
				else
				{
					if (this.oAutoProxy != null)
					{
						MessageBox.Show(this.oAutoProxy.ToString().Trim(), "Auto-Configured Gateway");
					}
					else
					{
						MessageBox.Show("No upstream gateway proxy is configured.", "No Upstream Gateway");
					}
				}
			}
		}
	}
}
