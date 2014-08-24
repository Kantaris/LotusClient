using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace OpenWeb
{
	public class OpenWebApplication
	{
		public static bool isClosing;
		public static System.Security.Cryptography.X509Certificates.X509Certificate oDefaultClientCertificate;
		public static LocalCertificateSelectionCallback ClientCertificateProvider;
		internal static readonly PeriodicWorker Janitor;
		internal static PreferenceBag _Prefs;
		[CodeDescription("OpenWeb's core proxy engine.")]
		public static Proxy oProxy;
		public static OpenWebTranscoders oTranscoders;
		private static System.Collections.Generic.List<string> slLeakedFiles;
		internal static readonly int iPID;
		internal static readonly string sProcessInfo;
		[CodeDescription("This event fires when the user instructs OpenWeb to clear the cache or cookies.")]
		public static event System.EventHandler<CacheClearEventArgs> OnClearCache;
		public static event System.EventHandler<RawReadEventArgs> OnReadResponseBuffer;
		public static event System.EventHandler<RawReadEventArgs> OnReadRequestBuffer;
		public static event SessionStateHandler BeforeRequest;
		public static event SessionStateHandler BeforeResponse;
		public static event SessionStateHandler RequestHeadersAvailable;
		public static event SessionStateHandler ResponseHeadersAvailable;
		public static event SessionStateHandler BeforeReturningError;
		public static event System.EventHandler<WebSocketMessageEventArgs> OnWebSocketMessage;
		public static event SessionStateHandler AfterSessionComplete;
		[CodeDescription("This event fires when a user notification would be shown. See CONFIG.QuietMode property.")]
		public static event System.EventHandler<NotificationEventArgs> OnNotification;
		[CodeDescription("This event fires a HTTPS certificate is validated.")]
		public static event System.EventHandler<ValidateServerCertificateEventArgs> OnValidateServerCertificate;
		[CodeDescription("Sync this event to be notified when OpenWebCore has attached as the system proxy.")]
		public static event SimpleEventHandler OpenWebAttach;
		[CodeDescription("Sync this event to be notified when OpenWebCore has detached as the system proxy.")]
		public static event SimpleEventHandler OpenWebDetach;
		public static event System.EventHandler<ConnectionEventArgs> AfterSocketConnect;
		public static event System.EventHandler<ConnectionEventArgs> AfterSocketAccept;
		public static ISAZProvider oSAZProvider
		{
			get;
			set;
		}
		[CodeDescription("OpenWeb's Preferences collection. http://OpenWeb.wikidot.com/prefs")]
		public static IOpenWebPreferences Prefs
		{
			get
			{
				return OpenWebApplication._Prefs;
			}
		}
		public static string GetVersionString()
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string empty = string.Empty;
			string text = "OpenWebCore";
			return string.Format("{0}/{1}.{2}.{3}.{4}{5}", new object[]
			{
				text,
				versionInfo.FileMajorPart,
				versionInfo.FileMinorPart,
				versionInfo.FileBuildPart,
				versionInfo.FilePrivatePart,
				empty
			});
		}
		public static string GetDetailedInfo()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(512);
			stringBuilder.AppendFormat("\nRunning {0}on: {1}:{2}\n", string.Empty, CONFIG.sMachineName, OpenWebApplication.oProxy.ListenPort.ToString());
			if (CONFIG.bHookAllConnections)
			{
				stringBuilder.AppendLine("Listening to: All Adapters");
			}
			else
			{
				stringBuilder.AppendFormat("Listening to: {0}\n", CONFIG.sHookConnectionNamed ?? "Default LAN");
			}
			if (CONFIG.iReverseProxyForPort > 0)
			{
				stringBuilder.AppendFormat("Acting as reverse proxy for port #{0}\n", CONFIG.iReverseProxyForPort);
			}
			stringBuilder.Append(OpenWebApplication.oProxy.GetGatewayInformation());
			string empty = string.Empty;
			return string.Format("OpenWeb Web Debugger ({0})\n{8}\n{1}-bit {2}, VM: {3:N2}mb, WS: {4:N2}mb\n{5} {6}\n\n{7}\n", new object[]
			{
				CONFIG.bIsBeta ? string.Format("v{0} beta", Application.ProductVersion) : string.Format("v{0}", Application.ProductVersion),
				(8 == System.IntPtr.Size) ? "64" : "32",
				System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
				Process.GetCurrentProcess().PagedMemorySize64 / 1048576L,
				Process.GetCurrentProcess().WorkingSet64 / 1048576L,
				".NET " + System.Environment.Version,
				Utilities.GetOSVerString(),
				stringBuilder.ToString(),
				empty
			});
		}
		public static bool IsStarted()
		{
			return null != OpenWebApplication.oProxy;
		}
		public static bool IsSystemProxy()
		{
			return OpenWebApplication.oProxy != null && OpenWebApplication.oProxy.IsAttached;
		}
		public static void Startup(int iListenPort, OpenWebCoreStartupFlags oFlags)
		{
			if (OpenWebApplication.oProxy != null)
			{
				throw new System.InvalidOperationException("Calling startup twice without calling shutdown is not permitted.");
			}
			if (iListenPort < 0 || iListenPort > 65535)
			{
				throw new System.ArgumentOutOfRangeException("bListenPort", "Port must be between 0 and 65535.");
			}
			CONFIG.ListenPort = iListenPort;
			CONFIG.bAllowRemoteConnections = (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.AllowRemoteClients));
			CONFIG.bMITM_HTTPS = (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.DecryptSSL));
			CONFIG.bCaptureCONNECT = true;
			CONFIG.bCaptureFTP = (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.CaptureFTP));
			CONFIG.UpstreamGateway = ((OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.ChainToUpstreamGateway)) ? GatewayType.System : GatewayType.None);
			CONFIG.bHookAllConnections = (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.MonitorAllConnections));
			if (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.CaptureLocalhostTraffic))
			{
				CONFIG.sHostsThatBypassOpenWeb = CONFIG.sHostsThatBypassOpenWeb;
			}
			OpenWebApplication.oProxy = new Proxy(true);
			if (OpenWebApplication.oProxy.Start(CONFIG.ListenPort, CONFIG.bAllowRemoteConnections))
			{
				if (iListenPort == 0)
				{
					CONFIG.ListenPort = OpenWebApplication.oProxy.ListenPort;
				}
				if (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.RegisterAsSystemProxy))
				{
					OpenWebApplication.oProxy.Attach(true);
				}
				else
				{
					if (OpenWebCoreStartupFlags.None < (oFlags & OpenWebCoreStartupFlags.ChainToUpstreamGateway))
					{
						OpenWebApplication.oProxy.CollectConnectoidAndGatewayInfo();
					}
				}
			}
		}
		public static void Startup(int iListenPort, bool bRegisterAsSystemProxy, bool bDecryptSSL)
		{
			OpenWebCoreStartupFlags openWebCoreStartupFlags = OpenWebCoreStartupFlags.Default;
			if (bRegisterAsSystemProxy)
			{
				openWebCoreStartupFlags |= OpenWebCoreStartupFlags.RegisterAsSystemProxy;
			}
			else
			{
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.RegisterAsSystemProxy;
			}
			if (bDecryptSSL)
			{
				openWebCoreStartupFlags |= OpenWebCoreStartupFlags.DecryptSSL;
			}
			else
			{
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.DecryptSSL;
			}
			OpenWebApplication.Startup(iListenPort, openWebCoreStartupFlags);
		}
		public static void Startup(int iListenPort, bool bRegisterAsSystemProxy, bool bDecryptSSL, bool bAllowRemote)
		{
			OpenWebCoreStartupFlags openWebCoreStartupFlags = OpenWebCoreStartupFlags.Default;
			if (bRegisterAsSystemProxy)
			{
				openWebCoreStartupFlags |= OpenWebCoreStartupFlags.RegisterAsSystemProxy;
			}
			else
			{
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.RegisterAsSystemProxy;
			}
			if (bDecryptSSL)
			{
				openWebCoreStartupFlags |= OpenWebCoreStartupFlags.DecryptSSL;
			}
			else
			{
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.DecryptSSL;
			}
			if (bAllowRemote)
			{
				openWebCoreStartupFlags |= OpenWebCoreStartupFlags.AllowRemoteClients;
			}
			else
			{
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.AllowRemoteClients;
			}
			OpenWebApplication.Startup(iListenPort, openWebCoreStartupFlags);
		}
		public static Proxy CreateProxyEndpoint(int iPort, bool bAllowRemote, string sHTTPSHostname)
		{
			Proxy proxy = new Proxy(false);
			if (!string.IsNullOrEmpty(sHTTPSHostname))
			{
				proxy.ActAsHTTPSEndpointForHostname(sHTTPSHostname);
			}
			bool flag = proxy.Start(iPort, bAllowRemote);
			Proxy result;
			if (flag)
			{
				result = proxy;
			}
			else
			{
				proxy.Dispose();
				result = null;
			}
			return result;
		}
		public static Proxy CreateProxyEndpoint(int iPort, bool bAllowRemote, X509Certificate2 certHTTPS)
		{
			Proxy proxy = new Proxy(false);
			if (certHTTPS != null)
			{
				proxy.AssignEndpointCertificate(certHTTPS);
			}
			bool flag = proxy.Start(iPort, bAllowRemote);
			Proxy result;
			if (flag)
			{
				result = proxy;
			}
			else
			{
				proxy.Dispose();
				result = null;
			}
			return result;
		}
		public static void Shutdown()
		{
			if (OpenWebApplication.oProxy != null)
			{
				OpenWebApplication.oProxy.Detach();
				OpenWebApplication.oProxy.Dispose();
				OpenWebApplication.oProxy = null;
			}
		}
		internal static bool DoReadResponseBuffer(Session oS, byte[] arrBytes, int cBytes)
		{
			bool result;
			if (OpenWebApplication.OnReadResponseBuffer == null)
			{
				result = true;
			}
			else
			{
				if (oS.isFlagSet(SessionFlags.Ignored))
				{
					result = true;
				}
				else
				{
					RawReadEventArgs rawReadEventArgs = new RawReadEventArgs(oS, arrBytes, cBytes);
					OpenWebApplication.OnReadResponseBuffer(oS, rawReadEventArgs);
					result = !rawReadEventArgs.AbortReading;
				}
			}
			return result;
		}
		internal static bool DoReadRequestBuffer(Session oS, byte[] arrBytes, int cBytes)
		{
			bool result;
			if (OpenWebApplication.OnReadRequestBuffer == null)
			{
				result = true;
			}
			else
			{
				if (oS.isFlagSet(SessionFlags.Ignored))
				{
					result = true;
				}
				else
				{
					RawReadEventArgs rawReadEventArgs = new RawReadEventArgs(oS, arrBytes, cBytes);
					OpenWebApplication.OnReadRequestBuffer(oS, rawReadEventArgs);
					result = !rawReadEventArgs.AbortReading;
				}
			}
			return result;
		}
		internal static bool DoClearCache(bool bClearFiles, bool bClearCookies)
		{
			System.EventHandler<CacheClearEventArgs> onClearCache = OpenWebApplication.OnClearCache;
			bool result;
			if (onClearCache == null)
			{
				result = true;
			}
			else
			{
				CacheClearEventArgs cacheClearEventArgs = new CacheClearEventArgs(bClearFiles, bClearCookies);
				onClearCache(null, cacheClearEventArgs);
				result = !cacheClearEventArgs.Cancel;
			}
			return result;
		}
		internal static void CheckOverrideCertificatePolicy(Session oS, string sExpectedCN, System.Security.Cryptography.X509Certificates.X509Certificate ServerCertificate, X509Chain ServerCertificateChain, SslPolicyErrors sslPolicyErrors, ref CertificateValidity oValidity)
		{
			System.EventHandler<ValidateServerCertificateEventArgs> onValidateServerCertificate = OpenWebApplication.OnValidateServerCertificate;
			if (onValidateServerCertificate != null)
			{
				ValidateServerCertificateEventArgs validateServerCertificateEventArgs = new ValidateServerCertificateEventArgs(oS, sExpectedCN, ServerCertificate, ServerCertificateChain, sslPolicyErrors);
				onValidateServerCertificate(oS, validateServerCertificateEventArgs);
				oValidity = validateServerCertificateEventArgs.ValidityState;
			}
		}
		internal static void DoBeforeRequest(Session oSession)
		{
			if (!oSession.isFlagSet(SessionFlags.Ignored))
			{
				if (OpenWebApplication.BeforeRequest != null)
				{
					OpenWebApplication.BeforeRequest(oSession);
				}
			}
		}
		internal static void DoBeforeResponse(Session oSession)
		{
			if (!oSession.isFlagSet(SessionFlags.Ignored))
			{
				if (OpenWebApplication.BeforeResponse != null)
				{
					OpenWebApplication.BeforeResponse(oSession);
				}
			}
		}
		internal static void DoResponseHeadersAvailable(Session oSession)
		{
			if (!oSession.isFlagSet(SessionFlags.Ignored))
			{
				if (OpenWebApplication.ResponseHeadersAvailable != null)
				{
					OpenWebApplication.ResponseHeadersAvailable(oSession);
				}
			}
		}
		internal static void DoRequestHeadersAvailable(Session oSession)
		{
			if (!oSession.isFlagSet(SessionFlags.Ignored))
			{
				if (OpenWebApplication.RequestHeadersAvailable != null)
				{
					OpenWebApplication.RequestHeadersAvailable(oSession);
				}
			}
		}
		internal static void DoOnWebSocketMessage(Session oS, WebSocketMessage oWSM)
		{
			if (!oS.isFlagSet(SessionFlags.Ignored))
			{
				System.EventHandler<WebSocketMessageEventArgs> onWebSocketMessage = OpenWebApplication.OnWebSocketMessage;
				if (onWebSocketMessage != null)
				{
					onWebSocketMessage(oS, new WebSocketMessageEventArgs(oWSM));
				}
			}
		}
		internal static void DoBeforeReturningError(Session oSession)
		{
			if (!oSession.isFlagSet(SessionFlags.Ignored))
			{
				if (OpenWebApplication.BeforeReturningError != null)
				{
					OpenWebApplication.BeforeReturningError(oSession);
				}
			}
		}
		internal static void DoAfterSessionComplete(Session oSession)
		{
			if (!oSession.isFlagSet(SessionFlags.Ignored))
			{
				if (OpenWebApplication.AfterSessionComplete != null)
				{
					OpenWebApplication.AfterSessionComplete(oSession);
				}
			}
		}
		internal static void OnOpenWebAttach()
		{
			if (OpenWebApplication.OpenWebAttach != null)
			{
				OpenWebApplication.OpenWebAttach();
			}
		}
		internal static void OnOpenWebDetach()
		{
			if (OpenWebApplication.OpenWebDetach != null)
			{
				OpenWebApplication.OpenWebDetach();
			}
		}
		[CodeDescription("Reset the SessionID counter to 0. This method can lead to confusing UI, so call sparingly.")]
		public static void ResetSessionCounter()
		{
			Session.ResetSessionCounter();
		}
		internal static void DoNotifyUser(string sMessage, string sTitle)
		{
			OpenWebApplication.DoNotifyUser(sMessage, sTitle, MessageBoxIcon.None);
		}
		internal static void DoNotifyUser(string sMessage, string sTitle, MessageBoxIcon oIcon)
		{
			if (OpenWebApplication.OnNotification != null)
			{
				NotificationEventArgs e = new NotificationEventArgs(string.Format("{0} - {1}", sTitle, sMessage));
				OpenWebApplication.OnNotification(null, e);
			}
			if (!CONFIG.QuietMode)
			{
				MessageBox.Show(sMessage, sTitle, MessageBoxButtons.OK, oIcon);
			}
		}
		internal static void ReportException(System.Exception eX)
		{
			OpenWebApplication.ReportException(eX, "Sorry, you may have found a bug...", null);
		}
		public static void ReportException(System.Exception eX, string sTitle)
		{
			OpenWebApplication.ReportException(eX, sTitle, null);
		}
		public static void ReportException(System.Exception eX, string sTitle, string sCallerMessage)
		{
			Trace.WriteLine(string.Concat(new object[]
			{
				"*************************\n",
				eX.Message,
				"\n",
				eX.StackTrace,
				"\n",
				eX.InnerException
			}));
			if (!(eX is System.Threading.ThreadAbortException) || !OpenWebApplication.isClosing)
			{
				bool flag = 0 == 0;
				string text;
				if (eX is System.OutOfMemoryException)
				{
					sTitle = "Out of Memory Error";
					text = "An out-of-memory exception was encountered. To help avoid out-of-memory conditions, please see: " + CONFIG.GetRedirUrl("OpenWebOOM");
				}
				else
				{
					if (string.IsNullOrEmpty(sCallerMessage))
					{
						text = "OpenWeb has encountered an unexpected problem. If you believe this is a bug in OpenWeb, please copy this message by hitting CTRL+C, and submit a bug report using the Help | Send Feedback menu.";
					}
					else
					{
						text = sCallerMessage;
					}
				}
				OpenWebApplication.DoNotifyUser(string.Concat(new object[]
				{
					text,
					"\n\n",
					eX.Message,
					"\n\nType: ",
					eX.GetType().ToString(),
					"\nSource: ",
					eX.Source,
					"\n",
					eX.StackTrace,
					"\n\n",
					eX.InnerException,
					"\nOpenWeb v",
					Application.ProductVersion,
					(8 == System.IntPtr.Size) ? " (x64 " : " (x86 ",
					System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
					") [.NET ",
					System.Environment.Version,
					" on ",
					System.Environment.OSVersion.VersionString,
					"] "
				}), sTitle, MessageBoxIcon.Hand);
			}
		}
		internal static void HandleHTTPError(Session oSession, SessionFlags flagViolation, bool bPoisonClientConnection, bool bPoisonServerConnection, string sMessage)
		{
			if (bPoisonClientConnection)
			{
				oSession.PoisonClientPipe();
			}
			if (bPoisonServerConnection)
			{
				oSession.PoisonServerPipe();
			}
			oSession.SetBitFlag(flagViolation, true);
			oSession["ui-backcolor"] = "LightYellow";
			sMessage = "[ProtocolViolation] " + sMessage;
			if (oSession["x-HTTPProtocol-Violation"] == null || !oSession["x-HTTPProtocol-Violation"].Contains(sMessage))
			{
				oSession["x-HTTPProtocol-Violation"] = oSession["x-HTTPProtocol-Violation"] + sMessage;
			}
		}
		internal static void DebugSpew(string sMessage)
		{
			if (CONFIG.bDebugSpew)
			{
				Trace.WriteLine(sMessage);
			}
		}
		internal static void DebugSpew(string sMessage, params object[] args)
		{
			if (CONFIG.bDebugSpew)
			{
				Trace.WriteLine(string.Format(sMessage, args));
			}
		}
		private OpenWebApplication()
		{
		}
		static OpenWebApplication()
		{
			OpenWebApplication.Janitor = new PeriodicWorker();
			OpenWebApplication._Prefs = null;
			OpenWebApplication.oTranscoders = new OpenWebTranscoders();
			OpenWebApplication.slLeakedFiles = new System.Collections.Generic.List<string>();
			OpenWebApplication._SetXceedLicenseKeys();
			OpenWebApplication._Prefs = new PreferenceBag(null);
			try
			{
				Process currentProcess = Process.GetCurrentProcess();
				OpenWebApplication.iPID = currentProcess.Id;
				OpenWebApplication.sProcessInfo = string.Format("{0}:{1}", currentProcess.ProcessName.ToLower(), OpenWebApplication.iPID);
				currentProcess.Dispose();
			}
			catch (System.Exception)
			{
			}
		}
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		internal static void _SetXceedLicenseKeys()
		{
		}
		internal static void LogAddonException(System.Exception eX, string sTitle)
		{
			if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.debug.extensions.showerrors", false) || OpenWebApplication.Prefs.GetBoolPref("OpenWeb.debug.extensions.verbose", false))
			{
				OpenWebApplication.ReportException(eX, sTitle, "OpenWeb has encountered an unexpected problem with an extension.");
			}
		}
		public static void LogLeakedFile(string sTempFile)
		{
			lock (OpenWebApplication.slLeakedFiles)
			{
				OpenWebApplication.slLeakedFiles.Add(sTempFile);
			}
		}
		internal static void WipeLeakedFiles()
		{
			try
			{
				if (OpenWebApplication.slLeakedFiles.Count >= 1)
				{
					lock (OpenWebApplication.slLeakedFiles)
					{
						foreach (string current in OpenWebApplication.slLeakedFiles)
						{
							try
							{
								System.IO.File.Delete(current);
							}
							catch (System.Exception)
							{
							}
						}
						OpenWebApplication.slLeakedFiles.Clear();
					}
				}
			}
			catch (System.Exception ex)
			{
				Trace.WriteLine(string.Format("OpenWeb.WipeLeakedFiles failed! {0}", ex.Message));
			}
		}
		internal static void DoAfterSocketConnect(Session oSession, Socket sockServer)
		{
			System.EventHandler<ConnectionEventArgs> afterSocketConnect = OpenWebApplication.AfterSocketConnect;
			if (afterSocketConnect != null)
			{
				ConnectionEventArgs e = new ConnectionEventArgs(oSession, sockServer);
				afterSocketConnect(oSession, e);
			}
		}
		internal static void DoAfterSocketAccept(Session oSession, Socket sockClient)
		{
			System.EventHandler<ConnectionEventArgs> afterSocketAccept = OpenWebApplication.AfterSocketAccept;
			if (afterSocketAccept != null)
			{
				ConnectionEventArgs e = new ConnectionEventArgs(oSession, sockClient);
				afterSocketAccept(oSession, e);
			}
		}
	}
}
