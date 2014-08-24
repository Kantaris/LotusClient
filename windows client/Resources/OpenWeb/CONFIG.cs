using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Windows.Forms;
namespace OpenWeb
{
	public static class CONFIG
	{
		internal const int I_MAX_CONNECTION_QUEUE = 50;
		internal static string sDefaultBrowserExe;
		internal static string sDefaultBrowserParams;
		internal static bool bRunningOnCLRv4;
		private static bool bQuietMode;
		private static ProcessFilterCategories _pfcDecyptFilter;
		private static string sLVColInfo;
		internal static bool bReloadSessionIDAsFlag;
		internal static bool bIsViewOnly;
		internal static bool bUseXceedDecompressForGZIP;
		internal static bool bUseXceedDecompressForDeflate;
		public static bool bMapSocketToProcess;
		public static bool bMITM_HTTPS;
		public static bool bUseSNIForCN;
		private static bool bIgnoreServerCertErrors;
		public static bool bStreamAudioVideo;
		internal static bool bCheckCompressionIntegrity;
		internal static bool bShowDefaultClientCertificateNeededPrompt;
		internal static string sOpenWebListenHostPort;
		internal static string sMakeCertParamsRoot;
		internal static string sMakeCertParamsEE;
		internal static string sMakeCertRootCN;
		internal static string sMakeCertSubjectO;
		private static string sRootUrl;
		private static string sSecureRootUrl;
		internal static string sRootKey;
		private static string sUserPath;
		private static string sScriptPath;
		public static bool bUseAESForSAZ;
		public static SslProtocols oAcceptedClientHTTPSProtocols;
		public static SslProtocols oAcceptedServerHTTPSProtocols;
		public static System.Version OpenWebVersionInfo;
		internal static bool bIsBeta;
		[System.Obsolete]
		public static bool bForwardToGateway;
		private static GatewayType _UpstreamGateway;
		public static bool bDebugSpew;
		public static System.Text.Encoding oHeaderEncoding;
		public static System.Text.Encoding oBodyEncoding;
		public static bool bReuseServerSockets;
		public static bool bReuseClientSockets;
		public static bool bCaptureCONNECT;
		public static bool bCaptureFTP;
		public static bool bUseEventLogForExceptions;
		public static bool bAutoProxyLogon;
		public static bool bEnableIPv6;
		public static string sHookConnectionNamed;
		internal static bool bHookAllConnections;
		internal static bool bHookWithPAC;
		private static string m_sHostsThatBypassOpenWeb;
		private static string m_JSEditor;
		private static bool m_bAllowRemoteConnections;
		public static string sGatewayUsername;
		public static string sGatewayPassword;
		private static bool m_bCheckForISA;
		private static int m_ListenPort;
		internal static bool bUsingPortOverride;
		private static bool m_bForceExclusivePort;
		public static bool bDebugCertificateGeneration;
		private static int _iReverseProxyForPort;
		public static string sAlternateHostname;
		internal static string sReverseProxyHostname;
		internal static string sMachineName;
		internal static string sMachineDomain;
		internal static HostList oHLSkipDecryption;
		private static bool fNeedToMaximizeOnload;
		public static RetryMode RetryOnReceiveFailure;
		public static ProcessFilterCategories DecryptWhichProcesses
		{
			get
			{
				return CONFIG._pfcDecyptFilter;
			}
			set
			{
				CONFIG._pfcDecyptFilter = value;
			}
		}
		public static GatewayType UpstreamGateway
		{
			get
			{
				return CONFIG._UpstreamGateway;
			}
			internal set
			{
				if (value < GatewayType.None || value > GatewayType.WPAD)
				{
					value = GatewayType.System;
				}
				CONFIG._UpstreamGateway = value;
				CONFIG.bForwardToGateway = (value != GatewayType.None);
			}
		}
		public static int iReverseProxyForPort
		{
			get
			{
				return CONFIG._iReverseProxyForPort;
			}
			set
			{
				if (value > -1 && value <= 65535 && value != CONFIG.m_ListenPort)
				{
					CONFIG._iReverseProxyForPort = value;
				}
			}
		}
		public static string sHostsThatBypassOpenWeb
		{
			get
			{
				return CONFIG.m_sHostsThatBypassOpenWeb;
			}
			set
			{
				string text = value;
				if (text == null)
				{
					text = string.Empty;
				}
				if (!text.OICContains("<-loopback>") && !text.OICContains("<loopback>"))
				{
					text = "<-loopback>;" + text;
				}
				CONFIG.m_sHostsThatBypassOpenWeb = text;
			}
		}
		public static bool ForceExclusivePort
		{
			get
			{
				return CONFIG.m_bForceExclusivePort;
			}
			internal set
			{
				CONFIG.m_bForceExclusivePort = value;
			}
		}
		public static bool IgnoreServerCertErrors
		{
			get
			{
				return CONFIG.bIgnoreServerCertErrors;
			}
			set
			{
				CONFIG.bIgnoreServerCertErrors = value;
			}
		}
		public static bool QuietMode
		{
			get
			{
				return CONFIG.bQuietMode;
			}
			set
			{
				CONFIG.bQuietMode = value;
			}
		}
		public static int ListenPort
		{
			get
			{
				return CONFIG.m_ListenPort;
			}
			internal set
			{
				if (value >= 0 && value < 65536)
				{
					CONFIG.m_ListenPort = value;
					CONFIG.sOpenWebListenHostPort = Utilities.TrimAfter(CONFIG.sOpenWebListenHostPort, ':') + ":" + CONFIG.m_ListenPort.ToString();
				}
			}
		}
		[CodeDescription("Return path to user's OpenWebScript editor.")]
		public static string JSEditor
		{
			get
			{
				if (string.IsNullOrEmpty(CONFIG.m_JSEditor))
				{
					CONFIG.m_JSEditor = CONFIG.GetPath("TextEditor");
				}
				return CONFIG.m_JSEditor;
			}
			set
			{
				CONFIG.m_JSEditor = value;
			}
		}
		[CodeDescription("Returns true if OpenWeb is configured to accept remote clients.")]
		public static bool bAllowRemoteConnections
		{
			get
			{
				return CONFIG.m_bAllowRemoteConnections;
			}
			internal set
			{
				CONFIG.m_bAllowRemoteConnections = value;
			}
		}
		public static void SetNoDecryptList(string sNewList)
		{
			if (string.IsNullOrEmpty(sNewList))
			{
				CONFIG.oHLSkipDecryption = null;
			}
			else
			{
				CONFIG.oHLSkipDecryption = new HostList();
				CONFIG.oHLSkipDecryption.AssignFromString(sNewList);
			}
		}
		[CodeDescription("Return a special Url.")]
		public static string GetUrl(string sWhatUrl)
		{
			string result;
			switch (sWhatUrl)
			{
			case "AutoResponderHelp":
				result = CONFIG.sRootUrl + "help/AutoResponder.asp";
				return result;
			case "ChangeList":
				result = "http://www.telerik.com/support/whats-new/OpenWeb/release-history/OpenWeb-v2.x";
				return result;
			case "FiltersHelp":
				result = CONFIG.sRootUrl + "help/Filters.asp";
				return result;
			case "HelpContents":
				result = CONFIG.sRootUrl + "help/?ver=";
				return result;
			case "REDIR":
				result = "http://OpenWeb2.com/r/?";
				return result;
			case "VerCheck":
				result = "http://www.telerik.com/UpdateCheck.aspx?isBeta=";
				return result;
			case "InstallLatest":
				if (!CONFIG.bIsBeta)
				{
					result = CONFIG.sSecureRootUrl + "r/?GetOpenWeb4";
					return result;
				}
				result = CONFIG.sSecureRootUrl + "r/?GetOpenWeb4Beta";
				return result;
			case "ShopAmazon":
				result = "http://www.OpenWebbook.com/r/?shop";
				return result;
			}
			result = CONFIG.sRootUrl;
			return result;
		}
		public static string GetRedirUrl(string sKeyword)
		{
			return string.Format("{0}{1}", CONFIG.GetUrl("REDIR"), sKeyword);
		}
		public static string GetRegPath(string sWhatPath)
		{
			string result;
			if (sWhatPath != null)
			{
				if (sWhatPath == "Root")
				{
					result = CONFIG.sRootKey;
					return result;
				}
				if (sWhatPath == "LMIsBeta")
				{
					result = CONFIG.sRootKey;
					return result;
				}
				if (sWhatPath == "MenuExt")
				{
					result = CONFIG.sRootKey + "MenuExt\\";
					return result;
				}
				if (sWhatPath == "UI")
				{
					result = CONFIG.sRootKey + "UI\\";
					return result;
				}
				if (sWhatPath == "Dynamic")
				{
					result = CONFIG.sRootKey + "Dynamic\\";
					return result;
				}
				if (sWhatPath == "Prefs")
				{
					result = CONFIG.sRootKey + "Prefs\\";
					return result;
				}
			}
			result = CONFIG.sRootKey;
			return result;
		}
		[CodeDescription("Return a filesystem path.")]
		public static string GetPath(string sWhatPath)
		{
			string result;
			switch (sWhatPath)
			{
			case "App":
				result = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + System.IO.Path.DirectorySeparatorChar;
				return result;
			case "AutoOpenWebs_Machine":
				result = string.Concat(new object[]
				{
					System.IO.Path.GetDirectoryName(Application.ExecutablePath),
					System.IO.Path.DirectorySeparatorChar,
					"Scripts",
					System.IO.Path.DirectorySeparatorChar
				});
				return result;
			case "AutoOpenWebs_User":
				result = CONFIG.sUserPath + "Scripts" + System.IO.Path.DirectorySeparatorChar;
				return result;
			case "AutoResponderDefaultRules":
				result = CONFIG.sUserPath + "AutoResponder.xml";
				return result;
			case "Captures":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.captures", CONFIG.sUserPath + "Captures" + System.IO.Path.DirectorySeparatorChar);
				return result;
			case "CustomRules":
				result = CONFIG.sScriptPath;
				return result;
			case "DefaultClientCertificate":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.defaultclientcert", CONFIG.sUserPath + "ClientCertificate.cer");
				return result;
			case "OpenWebRootCert":
				result = CONFIG.sUserPath + "DO_NOT_TRUST_OpenWebRoot.cer";
				return result;
			case "Filters":
				result = CONFIG.sUserPath + "Filters" + System.IO.Path.DirectorySeparatorChar;
				return result;
			case "Inspectors":
				result = string.Concat(new object[]
				{
					System.IO.Path.GetDirectoryName(Application.ExecutablePath),
					System.IO.Path.DirectorySeparatorChar,
					"Inspectors",
					System.IO.Path.DirectorySeparatorChar
				});
				return result;
			case "Inspectors_User":
				result = CONFIG.sUserPath + "Inspectors" + System.IO.Path.DirectorySeparatorChar;
				return result;
			case "PerUser-ISA-Config":
			{
				string text = "C:\\";
				try
				{
					text = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
				}
				catch (System.Exception)
				{
				}
				result = text + "\\microsoft\\firewall client 2004\\management.ini";
				return result;
			}
			case "PerMachine-ISA-Config":
			{
				string text = "C:\\";
				try
				{
					text = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
				}
				catch (System.Exception)
				{
				}
				result = text + "\\microsoft\\firewall client 2004\\management.ini";
				return result;
			}
			case "MakeCert":
			{
				string text = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.makecert", System.IO.Path.GetDirectoryName(Application.ExecutablePath) + System.IO.Path.DirectorySeparatorChar + "MakeCert.exe");
				if (!System.IO.File.Exists(text))
				{
					text = "MakeCert.exe";
				}
				result = text;
				return result;
			}
			case "MyDocs":
			{
				string text = "C:\\";
				try
				{
					text = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
				}
				catch (System.Exception ex)
				{
					OpenWebApplication.DoNotifyUser("Initialization Error", "Failed to retrieve path to your My Documents folder.\nThis generally means you have a relative environment variable.\nDefaulting to C:\\\n\n" + ex.Message);
				}
				result = text;
				return result;
			}
			case "Pac":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.pac", string.Concat(new object[]
				{
					CONFIG.sUserPath,
					"Scripts",
					System.IO.Path.DirectorySeparatorChar,
					"BrowserPAC.js"
				}));
				return result;
			case "Requests":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.requests", string.Concat(new object[]
				{
					CONFIG.sUserPath,
					"Captures",
					System.IO.Path.DirectorySeparatorChar,
					"Requests",
					System.IO.Path.DirectorySeparatorChar
				}));
				return result;
			case "Responses":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.responses", string.Concat(new object[]
				{
					CONFIG.sUserPath,
					"Captures",
					System.IO.Path.DirectorySeparatorChar,
					"Responses",
					System.IO.Path.DirectorySeparatorChar
				}));
				return result;
			case "Root":
				result = CONFIG.sUserPath;
				return result;
			case "SafeTemp":
			{
				string text = "C:\\";
				try
				{
					text = System.Environment.GetFolderPath(System.Environment.SpecialFolder.InternetCache) + System.IO.Path.DirectorySeparatorChar;
				}
				catch (System.Exception ex2)
				{
					OpenWebApplication.DoNotifyUser("Failed to retrieve path to your Internet Cache folder.\nThis generally means you have a relative environment variable.\nDefaulting to C:\\\n\n" + ex2.Message, "GetPath(SafeTemp) Failed");
				}
				result = text;
				return result;
			}
			case "SampleRules":
				result = string.Concat(new object[]
				{
					System.IO.Path.GetDirectoryName(Application.ExecutablePath),
					System.IO.Path.DirectorySeparatorChar,
					"Scripts",
					System.IO.Path.DirectorySeparatorChar,
					"SampleRules.js"
				});
				return result;
			case "Scripts":
				result = CONFIG.sUserPath + "Scripts" + System.IO.Path.DirectorySeparatorChar;
				return result;
			case "TemplateResponses":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.templateresponses", string.Concat(new object[]
				{
					System.IO.Path.GetDirectoryName(Application.ExecutablePath),
					System.IO.Path.DirectorySeparatorChar,
					"ResponseTemplates",
					System.IO.Path.DirectorySeparatorChar
				}));
				return result;
			case "Tools":
				result = OpenWebApplication.Prefs.GetStringPref("OpenWeb.config.path.Tools", string.Concat(new object[]
				{
					System.IO.Path.GetDirectoryName(Application.ExecutablePath),
					System.IO.Path.DirectorySeparatorChar,
					"Tools",
					System.IO.Path.DirectorySeparatorChar
				}));
				return result;
			case "Transcoders_Machine":
				result = string.Concat(new object[]
				{
					System.IO.Path.GetDirectoryName(Application.ExecutablePath),
					System.IO.Path.DirectorySeparatorChar,
					"ImportExport",
					System.IO.Path.DirectorySeparatorChar
				});
				return result;
			case "Transcoders_User":
				result = CONFIG.sUserPath + "ImportExport" + System.IO.Path.DirectorySeparatorChar;
				return result;
			}
			result = "C:\\";
			return result;
		}
		internal static void EnsureFoldersExist()
		{
			try
			{
				if (!System.IO.Directory.Exists(CONFIG.GetPath("Captures")))
				{
					System.IO.Directory.CreateDirectory(CONFIG.GetPath("Captures"));
				}
				if (!System.IO.Directory.Exists(CONFIG.GetPath("Requests")))
				{
					System.IO.Directory.CreateDirectory(CONFIG.GetPath("Requests"));
				}
				if (!System.IO.Directory.Exists(CONFIG.GetPath("Responses")))
				{
					System.IO.Directory.CreateDirectory(CONFIG.GetPath("Responses"));
				}
				if (!System.IO.Directory.Exists(CONFIG.GetPath("Scripts")))
				{
					System.IO.Directory.CreateDirectory(CONFIG.GetPath("Scripts"));
				}
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser(ex.ToString(), "Folder Creation Failed");
			}
			try
			{
				if (!OpenWebApplication.Prefs.GetBoolPref("OpenWeb.script.delaycreate", true) && !System.IO.File.Exists(CONFIG.GetPath("CustomRules")) && System.IO.File.Exists(CONFIG.GetPath("SampleRules")))
				{
					System.IO.File.Copy(CONFIG.GetPath("SampleRules"), CONFIG.GetPath("CustomRules"));
				}
			}
			catch (System.Exception ex2)
			{
				OpenWebApplication.DoNotifyUser(ex2.ToString(), "Initial file copies failed");
			}
		}
		static CONFIG()
		{
			CONFIG.sDefaultBrowserExe = "iexplore.exe";
			CONFIG.sDefaultBrowserParams = string.Empty;
			CONFIG.bRunningOnCLRv4 = true;
			CONFIG.bQuietMode = !System.Environment.UserInteractive;
			CONFIG._pfcDecyptFilter = ProcessFilterCategories.All;
			CONFIG.sLVColInfo = null;
			CONFIG.bReloadSessionIDAsFlag = false;
			CONFIG.bIsViewOnly = false;
			CONFIG.bUseXceedDecompressForGZIP = false;
			CONFIG.bUseXceedDecompressForDeflate = false;
			CONFIG.bMapSocketToProcess = true;
			CONFIG.bMITM_HTTPS = false;
			CONFIG.bUseSNIForCN = false;
			CONFIG.bIgnoreServerCertErrors = false;
			CONFIG.bStreamAudioVideo = false;
			CONFIG.bCheckCompressionIntegrity = false;
			CONFIG.bShowDefaultClientCertificateNeededPrompt = true;
			CONFIG.sOpenWebListenHostPort = "127.0.0.1:8888";
			CONFIG.sMakeCertParamsRoot = "-r -ss my -n \"CN={0}{1}\" -sky signature -eku 1.3.6.1.5.5.7.3.1 -h 1 -cy authority -a sha1 -m 132 -b {3}{4}";
			CONFIG.sMakeCertParamsEE = "-pe -ss my -n \"CN={0}{1}\" -sky exchange -in {2} -is my -eku 1.3.6.1.5.5.7.3.1 -cy end -a sha1 -m 132 -b {3}{4}";
			CONFIG.sMakeCertRootCN = "DO_NOT_TRUST_OpenWebRoot";
			CONFIG.sMakeCertSubjectO = ", O=DO_NOT_TRUST, OU=Created by http://www.OpenWeb2.com";
			CONFIG.sRootUrl = "http://OpenWeb2.com/OpenWebcore/";
			CONFIG.sSecureRootUrl = "https://OpenWeb2.com/";
			CONFIG.sRootKey = "SOFTWARE\\Microsoft\\OpenWebCore\\";
			CONFIG.sUserPath = string.Concat(new object[]
			{
				CONFIG.GetPath("MyDocs"),
				System.IO.Path.DirectorySeparatorChar,
				"OpenWebCore",
				System.IO.Path.DirectorySeparatorChar
			});
			CONFIG.sScriptPath = string.Concat(new object[]
			{
				CONFIG.sUserPath,
				"Scripts",
				System.IO.Path.DirectorySeparatorChar,
				"CustomRules.js"
			});
			CONFIG.bUseAESForSAZ = true;
			CONFIG.oAcceptedClientHTTPSProtocols = (SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls);
			CONFIG.oAcceptedServerHTTPSProtocols = SslProtocols.Default;
			CONFIG.OpenWebVersionInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			CONFIG.bIsBeta = false;
			CONFIG.bForwardToGateway = true;
			CONFIG._UpstreamGateway = GatewayType.System;
			CONFIG.bDebugSpew = false;
			CONFIG.oHeaderEncoding = System.Text.Encoding.UTF8;
			CONFIG.oBodyEncoding = System.Text.Encoding.UTF8;
			CONFIG.bReuseServerSockets = true;
			CONFIG.bReuseClientSockets = true;
			CONFIG.bCaptureCONNECT = true;
			CONFIG.bCaptureFTP = false;
			CONFIG.bUseEventLogForExceptions = false;
			CONFIG.bAutoProxyLogon = false;
			CONFIG.bEnableIPv6 = (System.Environment.OSVersion.Version.Major > 5);
			CONFIG.sHookConnectionNamed = "DefaultLAN";
			CONFIG.bHookAllConnections = true;
			CONFIG.bHookWithPAC = false;
			CONFIG.m_bCheckForISA = true;
			CONFIG.m_ListenPort = 8888;
			CONFIG.bUsingPortOverride = false;
			CONFIG.bDebugCertificateGeneration = true;
			CONFIG.sAlternateHostname = "?";
			CONFIG.sReverseProxyHostname = "localhost";
			CONFIG.sMachineName = string.Empty;
			CONFIG.sMachineDomain = string.Empty;
			CONFIG.oHLSkipDecryption = null;
			CONFIG.RetryOnReceiveFailure = RetryMode.Always;
			try
			{
				IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
				CONFIG.sMachineDomain = iPGlobalProperties.DomainName.ToLowerInvariant();
				CONFIG.sMachineName = iPGlobalProperties.HostName.ToLowerInvariant();
			}
			catch (System.Exception)
			{
			}
			CONFIG.bQuietMode = true;
			CONFIG.bDebugSpew = false;
			CONFIG.m_ListenPort = 8866;
			if (System.Environment.OSVersion.Version.Major < 6 && System.Environment.OSVersion.Version.Minor < 1)
			{
				CONFIG.bMapSocketToProcess = false;
			}
		}
	}
}
