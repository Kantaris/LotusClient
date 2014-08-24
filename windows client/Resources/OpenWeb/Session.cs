using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
namespace OpenWeb
{
	[System.Diagnostics.DebuggerDisplay("Session #{m_requestID}, {m_state}, {fullUrl}, [{BitFlags}]")]
	public class Session
	{
		private static bool bTrySPNTokenObject = true;
		private WebRequest __WebRequestForAuth;
		public ITunnel __oTunnel;
		private SessionFlags _bitFlags;
		private static int cRequests;
		private Session nextSession;
		public bool bBufferResponse;
		public SessionTimers Timers;
		private SessionStates m_state;
		private bool _bypassGateway;
		private int m_requestID;
		private int _LocalProcessID;
		public object ViewItem;
		private bool _bAllowClientPipeReuse;
		[CodeDescription("Object representing the HTTP Response.")]
		public ServerChatter oResponse;
		[CodeDescription("Object representing the HTTP Request.")]
		public ClientChatter oRequest;
		[CodeDescription("OpenWeb-internal flags set on the session.")]
		public readonly StringDictionary oFlags;
		[CodeDescription("Contains the bytes of the request body.")]
		public byte[] requestBodyBytes;
		[CodeDescription("Contains the bytes of the response body.")]
		public byte[] responseBodyBytes;
		[CodeDescription("IP Address of the client for this session.")]
		public string m_clientIP;
		[CodeDescription("Client port attached to OpenWeb.")]
		public int m_clientPort;
		[CodeDescription("IP Address of the server for this session.")]
		public string m_hostIP;
		private System.Threading.AutoResetEvent oSyncEvent;
		public event System.EventHandler<StateChangeEventArgs> OnStateChanged;
		public event System.EventHandler<ContinueTransactionEventArgs> OnContinueTransaction;
		public SessionFlags BitFlags
		{
			get
			{
				return this._bitFlags;
			}
			internal set
			{
				if (CONFIG.bDebugSpew && value != this._bitFlags)
				{
					OpenWebApplication.DebugSpew(string.Format("Session #{0} bitflags adjusted from {1} to {2} @ {3}", new object[]
					{
						this.id,
						this._bitFlags,
						value,
						System.Environment.StackTrace
					}));
				}
				this._bitFlags = value;
			}
		}
		public bool isTunnel
		{
			get;
			internal set;
		}
		public object Tag
		{
			get;
			set;
		}
		public bool TunnelIsOpen
		{
			get
			{
				return this.__oTunnel != null && this.__oTunnel.IsOpen;
			}
		}
		public long TunnelIngressByteCount
		{
			get
			{
				long result;
				if (this.__oTunnel != null)
				{
					result = this.__oTunnel.IngressByteCount;
				}
				else
				{
					result = 0L;
				}
				return result;
			}
		}
		public long TunnelEgressByteCount
		{
			get
			{
				long result;
				if (this.__oTunnel != null)
				{
					result = this.__oTunnel.EgressByteCount;
				}
				else
				{
					result = 0L;
				}
				return result;
			}
		}
		[CodeDescription("Gets or Sets the Request body bytes; Setter fixes up headers.")]
		public byte[] RequestBody
		{
			get
			{
				return this.requestBodyBytes ?? Utilities.emptyByteArray;
			}
			set
			{
				if (value == null)
				{
					value = Utilities.emptyByteArray;
				}
				this.oRequest.headers.Remove("Transfer-Encoding");
				this.oRequest.headers.Remove("Content-Encoding");
				this.requestBodyBytes = value;
				this.oRequest.headers["Content-Length"] = value.LongLength.ToString();
			}
		}
		[CodeDescription("Gets or Sets the request's Method (e.g. GET, POST, etc).")]
		public string RequestMethod
		{
			get
			{
				string result;
				if (!Utilities.HasHeaders(this.oRequest))
				{
					result = string.Empty;
				}
				else
				{
					result = this.oRequest.headers.HTTPMethod;
				}
				return result;
			}
			set
			{
				if (Utilities.HasHeaders(this.oRequest))
				{
					this.oRequest.headers.HTTPMethod = value;
				}
			}
		}
		[CodeDescription("Gets or Sets the Response body bytes; Setter fixes up headers.")]
		public byte[] ResponseBody
		{
			get
			{
				return this.responseBodyBytes ?? Utilities.emptyByteArray;
			}
			set
			{
				if (value == null)
				{
					value = Utilities.emptyByteArray;
				}
				this.oResponse.headers.Remove("Transfer-Encoding");
				this.oResponse.headers.Remove("Content-Encoding");
				this.responseBodyBytes = value;
				this.oResponse.headers["Content-Length"] = value.LongLength.ToString();
			}
		}
		[CodeDescription("When true, this session was conducted using the HTTPS protocol.")]
		public bool isHTTPS
		{
			get
			{
				return Utilities.HasHeaders(this.oRequest) && "HTTPS".OICEquals(this.oRequest.headers.UriScheme);
			}
		}
		[CodeDescription("When true, this session was conducted using the FTPS protocol.")]
		public bool isFTP
		{
			get
			{
				return Utilities.HasHeaders(this.oRequest) && "FTP".OICEquals(this.oRequest.headers.UriScheme);
			}
		}
		[CodeDescription("Get the process ID of the application which made this request, or 0 if it cannot be determined.")]
		public int LocalProcessID
		{
			get
			{
				return this._LocalProcessID;
			}
		}
		[CodeDescription("Gets a path-less filename suitable for saving the Response entity. Uses Content-Disposition if available.")]
		public string SuggestedFilename
		{
			get
			{
				string result;
				if (!Utilities.HasHeaders(this.oResponse))
				{
					result = this.id.ToString() + ".txt";
				}
				else
				{
					if (Utilities.IsNullOrEmpty(this.responseBodyBytes))
					{
						string format = "{0}_Status{1}.txt";
						result = string.Format(format, this.id.ToString(), this.responseCode.ToString());
					}
					else
					{
						string tokenValue = this.oResponse.headers.GetTokenValue("Content-Disposition", "filename*");
						if (tokenValue != null && tokenValue.Length > 7 && tokenValue.StartsWith("utf-8''", System.StringComparison.OrdinalIgnoreCase))
						{
							result = Utilities.UrlDecode(tokenValue.Substring(7));
						}
						else
						{
							tokenValue = this.oResponse.headers.GetTokenValue("Content-Disposition", "filename");
							if (tokenValue != null)
							{
								result = Session._MakeSafeFilename(tokenValue);
							}
							else
							{
								string text = Utilities.TrimBeforeLast(Utilities.TrimAfter(this.url, '?'), '/');
								if (text.Length > 0 && text.Length < 64 && text.Contains(".") && text.LastIndexOf('.') == text.IndexOf('.'))
								{
									string text2 = Session._MakeSafeFilename(text);
									string text3 = string.Empty;
									if (this.url.Contains("?") || text2.Length < 1 || text2.EndsWith(".php") || text2.EndsWith(".aspx") || text2.EndsWith(".asp") || text2.EndsWith(".asmx") || text2.EndsWith(".cgi"))
									{
										text3 = Utilities.FileExtensionForMIMEType(this.oResponse.MIMEType);
										if (text2.OICEndsWith(text3))
										{
											text3 = string.Empty;
										}
									}
									string format2 = OpenWebApplication.Prefs.GetBoolPref("OpenWeb.session.prependIDtosuggestedfilename", false) ? "{0}_{1}{2}" : "{1}{2}";
									result = string.Format(format2, this.id.ToString(), text2, text3);
								}
								else
								{
									System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
									stringBuilder.Append(this.id);
									stringBuilder.Append("_");
									string mIMEType = this.oResponse.MIMEType;
									stringBuilder.Append(Utilities.FileExtensionForMIMEType(mIMEType));
									result = stringBuilder.ToString();
								}
							}
						}
					}
				}
				return result;
			}
		}
		[CodeDescription("Set to true in OnBeforeRequest if this request should bypass the gateway")]
		public bool bypassGateway
		{
			get
			{
				return this._bypassGateway;
			}
			set
			{
				this._bypassGateway = value;
			}
		}
		[CodeDescription("Returns the port used by the client to communicate to OpenWeb.")]
		public int clientPort
		{
			get
			{
				return this.m_clientPort;
			}
		}
		[CodeDescription("Enumerated state of the current session.")]
		public SessionStates state
		{
			get
			{
				return this.m_state;
			}
			set
			{
				SessionStates state = this.m_state;
				this.m_state = value;
				if (!this.isFlagSet(SessionFlags.Ignored))
				{
					System.EventHandler<StateChangeEventArgs> onStateChanged = this.OnStateChanged;
					if (onStateChanged != null)
					{
						StateChangeEventArgs e = new StateChangeEventArgs(state, value);
						onStateChanged(this, e);
						if (this.m_state >= SessionStates.Done)
						{
							this.OnStateChanged = null;
						}
					}
				}
			}
		}
		[CodeDescription("Returns the path and query part of the URL. (For a CONNECT request, returns the host:port to be connected.)")]
		public string PathAndQuery
		{
			get
			{
				string result;
				if (this.oRequest.headers == null)
				{
					result = string.Empty;
				}
				else
				{
					result = this.oRequest.headers.RequestPath;
				}
				return result;
			}
			set
			{
				this.oRequest.headers.RequestPath = value;
			}
		}
		[CodeDescription("Retrieves the complete URI, including protocol/scheme, in the form http://www.host.com/filepath?query.")]
		public string fullUrl
		{
			get
			{
				string result;
				if (!Utilities.HasHeaders(this.oRequest))
				{
					result = string.Empty;
				}
				else
				{
					result = string.Format("{0}://{1}", this.oRequest.headers.UriScheme, this.url);
				}
				return result;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new System.ArgumentException("Must specify a complete URI");
				}
				string text = Utilities.TrimAfter(value, "://").ToLowerInvariant();
				string url = Utilities.TrimBefore(value, "://");
				if (text != "http" && text != "https" && text != "ftp")
				{
					throw new System.ArgumentException("URI scheme must be http, https, or ftp");
				}
				this.oRequest.headers.UriScheme = text;
				this.url = url;
			}
		}
		[CodeDescription("Gets or sets the URL (without protocol) being requested from the server, in the form www.host.com/filepath?query.")]
		public string url
		{
			get
			{
				string result;
				if (this.HTTPMethodIs("CONNECT"))
				{
					result = this.PathAndQuery;
				}
				else
				{
					result = this.host + this.PathAndQuery;
				}
				return result;
			}
			set
			{
				if (value.OICStartsWithAny(new string[]
				{
					"http://",
					"https://",
					"ftp://"
				}))
				{
					throw new System.ArgumentException("If you wish to specify a protocol, use the fullUrl property instead. Input was: " + value);
				}
				if (this.HTTPMethodIs("CONNECT"))
				{
					this.PathAndQuery = value;
					this.host = value;
				}
				else
				{
					int num = value.IndexOfAny(new char[]
					{
						'/',
						'?'
					});
					if (num > -1)
					{
						this.host = value.Substring(0, num);
						this.PathAndQuery = value.Substring(num);
					}
					else
					{
						this.host = value;
						this.PathAndQuery = "/";
					}
				}
			}
		}
		[CodeDescription("Gets/Sets the host to which this request is targeted. MAY include IPv6 literal brackets. MAY include a trailing port#.")]
		public string host
		{
			get
			{
				string result;
				if (this.oRequest == null)
				{
					result = string.Empty;
				}
				else
				{
					result = this.oRequest.host;
				}
				return result;
			}
			set
			{
				if (this.oRequest != null)
				{
					this.oRequest.host = value;
				}
			}
		}
		[CodeDescription("Gets/Sets the hostname to which this request is targeted; does NOT include any port# but will include IPv6-literal brackets for IPv6 literals.")]
		public string hostname
		{
			get
			{
				string host = this.oRequest.host;
				string result;
				if (host.Length < 1)
				{
					result = string.Empty;
				}
				else
				{
					int num = host.LastIndexOf(':');
					if (num > -1 && num > host.LastIndexOf(']'))
					{
						result = host.Substring(0, num);
					}
					else
					{
						result = this.oRequest.host;
					}
				}
				return result;
			}
			set
			{
				int num = value.LastIndexOf(':');
				if (num > -1 && num > value.LastIndexOf(']'))
				{
					throw new System.ArgumentException("Do not specify a port when setting hostname; use host property instead.");
				}
				string text = this.HTTPMethodIs("CONNECT") ? this.PathAndQuery : this.host;
				num = text.LastIndexOf(':');
				if (num > -1 && num > text.LastIndexOf(']'))
				{
					this.host = value + text.Substring(num);
				}
				else
				{
					this.host = value;
				}
			}
		}
		[CodeDescription("Returns the server port to which this request is targeted.")]
		public int port
		{
			get
			{
				string sHostPort = this.HTTPMethodIs("CONNECT") ? this.oRequest.headers.RequestPath : this.oRequest.host;
				int result = this.isHTTPS ? 443 : (this.isFTP ? 21 : 80);
				string text;
				Utilities.CrackHostAndPort(sHostPort, out text, ref result);
				return result;
			}
			set
			{
				if (value < 0 || value > 65535)
				{
					throw new System.ArgumentException("A valid target port value (0-65535) must be specified.");
				}
				this.host = string.Format("{0}:{1}", this.hostname, value);
			}
		}
		[CodeDescription("Returns the sequential number of this request.")]
		public int id
		{
			get
			{
				return this.m_requestID;
			}
		}
		[CodeDescription("Returns the Address used by the client to communicate to OpenWeb.")]
		public string clientIP
		{
			get
			{
				string result;
				if (this.m_clientIP != null)
				{
					result = this.m_clientIP;
				}
				else
				{
					result = "0.0.0.0";
				}
				return result;
			}
		}
		[CodeDescription("Gets or Sets the HTTP Status code of the server's response")]
		public int responseCode
		{
			get
			{
				int result;
				if (Utilities.HasHeaders(this.oResponse))
				{
					result = this.oResponse.headers.HTTPResponseCode;
				}
				else
				{
					result = 0;
				}
				return result;
			}
			set
			{
				if (Utilities.HasHeaders(this.oResponse))
				{
					this.oResponse.headers.SetStatus(value, "Fiddled");
				}
			}
		}
		public bool bHasWebSocketMessages
		{
			get
			{
				bool result;
				if (!this.isAnyFlagSet(SessionFlags.IsWebSocketTunnel) || this.HTTPMethodIs("CONNECT"))
				{
					result = false;
				}
				else
				{
					WebSocket webSocket = this.__oTunnel as WebSocket;
					result = (webSocket != null && webSocket.MessageCount > 0);
				}
				return result;
			}
		}
		[CodeDescription("Returns TRUE if this session state>ReadingResponse and oResponse not null.")]
		public bool bHasResponse
		{
			get
			{
				return this.state > SessionStates.ReadingResponse && this.oResponse != null && this.oResponse.headers != null && null != this.responseBodyBytes;
			}
		}
		[CodeDescription("Indexer property into SESSION flags, REQUEST headers, and RESPONSE headers. e.g. oSession[\"Request\", \"Host\"] returns string value for the Request host header. If null, returns String.Empty")]
		public string this[string sCollection, string sName]
		{
			get
			{
				string result;
				if ("SESSION".OICEquals(sCollection))
				{
					string text = this.oFlags[sName];
					result = (text ?? string.Empty);
				}
				else
				{
					if ("REQUEST".OICEquals(sCollection))
					{
						if (!Utilities.HasHeaders(this.oRequest))
						{
							result = string.Empty;
						}
						else
						{
							result = this.oRequest[sName];
						}
					}
					else
					{
						if (!"RESPONSE".OICEquals(sCollection))
						{
							result = "undefined";
						}
						else
						{
							if (!Utilities.HasHeaders(this.oResponse))
							{
								result = string.Empty;
							}
							else
							{
								result = this.oResponse[sName];
							}
						}
					}
				}
				return result;
			}
		}
		[CodeDescription("Indexer property into session flags collection. oSession[\"Flagname\"] returns string value (or null if missing!).")]
		public string this[string sFlag]
		{
			get
			{
				return this.oFlags[sFlag];
			}
			set
			{
				if (value == null)
				{
					this.oFlags.Remove(sFlag);
				}
				else
				{
					this.oFlags[sFlag] = value;
				}
			}
		}
		public void UNSTABLE_SetBitFlag(SessionFlags FlagsToSet, bool b)
		{
			this.SetBitFlag(FlagsToSet, b);
		}
		internal void SetBitFlag(SessionFlags FlagsToSet, bool b)
		{
			if (b)
			{
				this.BitFlags = (this._bitFlags | FlagsToSet);
			}
			else
			{
				this.BitFlags = (this._bitFlags & ~FlagsToSet);
			}
		}
		public bool isFlagSet(SessionFlags FlagsToTest)
		{
			return FlagsToTest == (this._bitFlags & FlagsToTest);
		}
		public bool isAnyFlagSet(SessionFlags FlagsToTest)
		{
			return SessionFlags.None != (this._bitFlags & FlagsToTest);
		}
		[CodeDescription("Returns TRUE if the Session's HTTP Method is available and matches the target method.")]
		public bool HTTPMethodIs(string sTestFor)
		{
			return Utilities.HasHeaders(this.oRequest) && string.Equals(this.oRequest.headers.HTTPMethod, sTestFor, System.StringComparison.OrdinalIgnoreCase);
		}
		[CodeDescription("Returns TRUE if the Session's target hostname (no port) matches sTestHost (case-insensitively).")]
		public bool HostnameIs(string sTestHost)
		{
			bool result;
			if (this.oRequest == null)
			{
				result = false;
			}
			else
			{
				int num = this.oRequest.host.LastIndexOf(':');
				if (num > -1 && num > this.oRequest.host.LastIndexOf(']'))
				{
					result = (0 == string.Compare(this.oRequest.host, 0, sTestHost, 0, num, System.StringComparison.OrdinalIgnoreCase));
				}
				else
				{
					result = string.Equals(this.oRequest.host, sTestHost, System.StringComparison.OrdinalIgnoreCase);
				}
			}
			return result;
		}
		private static string _MakeSafeFilename(string sFilename)
		{
			char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
			string result;
			if (sFilename.IndexOfAny(invalidFileNameChars) < 0)
			{
				result = Utilities.TrimTo(sFilename, 160);
			}
			else
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(sFilename);
				for (int i = 0; i < stringBuilder.Length; i++)
				{
					if (System.Array.IndexOf<char>(invalidFileNameChars, sFilename[i]) > -1)
					{
						stringBuilder[i] = '-';
					}
				}
				result = Utilities.TrimTo(stringBuilder.ToString(), 160);
			}
			return result;
		}
		private void FireContinueTransaction(Session oOrig, Session oNew, ContinueTransactionReason oReason)
		{
			System.EventHandler<ContinueTransactionEventArgs> onContinueTransaction = this.OnContinueTransaction;
			if (onContinueTransaction != null)
			{
				ContinueTransactionEventArgs e = new ContinueTransactionEventArgs(oOrig, oNew, oReason);
				onContinueTransaction(this, e);
			}
		}
		public string ToHTMLFragment(bool HeadersOnly)
		{
			string text2;
			if (!HeadersOnly)
			{
				string text = this.oRequest.headers.ToString(true, true);
				if (this.requestBodyBytes != null)
				{
					text += System.Text.Encoding.UTF8.GetString(this.requestBodyBytes);
				}
				text2 = "<SPAN CLASS='REQUEST'>" + Utilities.HtmlEncode(text).Replace("\r\n", "<BR>") + "</SPAN><BR>";
				if (this.oResponse != null && this.oResponse.headers != null)
				{
					string text3 = this.oResponse.headers.ToString(true, true);
					if (this.responseBodyBytes != null)
					{
						System.Text.Encoding responseBodyEncoding = Utilities.getResponseBodyEncoding(this);
						text3 += responseBodyEncoding.GetString(this.responseBodyBytes);
					}
					text2 = text2 + "<SPAN CLASS='RESPONSE'>" + Utilities.HtmlEncode(text3).Replace("\r\n", "<BR>") + "</SPAN>";
				}
			}
			else
			{
				text2 = "<SPAN CLASS='REQUEST'>" + Utilities.HtmlEncode(this.oRequest.headers.ToString()).Replace("\r\n", "<BR>") + "</SPAN><BR>";
				if (this.oResponse != null && this.oResponse.headers != null)
				{
					text2 = text2 + "<SPAN CLASS='RESPONSE'>" + Utilities.HtmlEncode(this.oResponse.headers.ToString()).Replace("\r\n", "<BR>") + "</SPAN>";
				}
			}
			return text2;
		}
		public string ToString(bool HeadersOnly)
		{
			string text;
			if (!HeadersOnly)
			{
				text = this.oRequest.headers.ToString(true, true);
				if (this.requestBodyBytes != null)
				{
					text += System.Text.Encoding.UTF8.GetString(this.requestBodyBytes);
				}
				if (this.oResponse != null && this.oResponse.headers != null)
				{
					text = text + "\r\n" + this.oResponse.headers.ToString(true, true);
					if (this.responseBodyBytes != null)
					{
						System.Text.Encoding responseBodyEncoding = Utilities.getResponseBodyEncoding(this);
						text += responseBodyEncoding.GetString(this.responseBodyBytes);
					}
				}
			}
			else
			{
				text = this.oRequest.headers.ToString();
				if (this.oResponse != null && this.oResponse.headers != null)
				{
					text = text + "\r\n" + this.oResponse.headers.ToString();
				}
			}
			return text;
		}
		public override string ToString()
		{
			return this.ToString(false);
		}
		private void ThreadPause()
		{
			if (this.oSyncEvent == null)
			{
				this.oSyncEvent = new System.Threading.AutoResetEvent(false);
			}
			this.oSyncEvent.WaitOne();
			this.oSyncEvent.Close();
			this.oSyncEvent = null;
		}
		public void ThreadResume()
		{
			if (this.oSyncEvent != null)
			{
				this.oSyncEvent.Set();
			}
		}
		[CodeDescription("Sets the SessionFlags.Ignore bit for this Session")]
		public void Ignore()
		{
			this.SetBitFlag(SessionFlags.Ignored, true);
			this.oFlags["log-drop-response-body"] = "IgnoreFlag";
			this.oFlags["log-drop-request-body"] = "IgnoreFlag";
			this.bBufferResponse = false;
		}
		internal static void CreateAndExecute(object oParams)
		{
			try
			{
				ProxyExecuteParams proxyExecuteParams = (ProxyExecuteParams)oParams;
				Socket oSocket = proxyExecuteParams.oSocket;
				ClientPipe clientPipe = new ClientPipe(oSocket, proxyExecuteParams.dtConnectionAccepted);
				Session session = new Session(clientPipe, null);
				OpenWebApplication.DoAfterSocketAccept(session, oSocket);
				if (proxyExecuteParams.oServerCert == null || session.AcceptHTTPSRequest(proxyExecuteParams.oServerCert))
				{
					session.Execute(null);
				}
			}
			catch (System.Exception eX)
			{
				OpenWebApplication.ReportException(eX);
			}
		}
		private bool AcceptHTTPSRequest(X509Certificate2 oCert)
		{
			bool result;
			try
			{
				if (CONFIG.bUseSNIForCN)
				{
					byte[] buffer = new byte[1024];
					int count = this.oRequest.pipeClient.GetRawSocket().Receive(buffer, SocketFlags.Peek);
					HTTPSClientHello hTTPSClientHello = new HTTPSClientHello();
					if (hTTPSClientHello.LoadFromStream(new System.IO.MemoryStream(buffer, 0, count, false)))
					{
						this.oFlags["https-Client-SessionID"] = hTTPSClientHello.SessionID;
						if (!string.IsNullOrEmpty(hTTPSClientHello.ServerNameIndicator))
						{
							this.oFlags["https-Client-SNIHostname"] = hTTPSClientHello.ServerNameIndicator;
						}
					}
				}
				if (!this.oRequest.pipeClient.SecureClientPipeDirect(oCert))
				{
					result = false;
					return result;
				}
			}
			catch (System.Exception var_3_B9)
			{
			}
			result = true;
			return result;
		}
		public bool COMETPeek()
		{
			bool result;
			if (this.state != SessionStates.ReadingResponse)
			{
				result = false;
			}
			else
			{
				this.responseBodyBytes = this.oResponse._PeekAtBody();
				result = true;
			}
			return result;
		}
		public void PoisonServerPipe()
		{
			if (this.oResponse != null)
			{
				this.oResponse._PoisonPipe();
			}
		}
		public void PoisonClientPipe()
		{
			this._bAllowClientPipeReuse = false;
		}
		private void CloseSessionPipes(bool bNullThemToo)
		{
			if (CONFIG.bDebugSpew)
			{
				OpenWebApplication.DebugSpew(string.Format("CloseSessionPipes() for Session #{0}", this.id));
			}
			if (this.oRequest != null && this.oRequest.pipeClient != null)
			{
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew("Closing client pipe...", new object[]
					{
						this.id
					});
				}
				this.oRequest.pipeClient.End();
				if (bNullThemToo)
				{
					this.oRequest.pipeClient = null;
				}
			}
			if (this.oResponse != null && this.oResponse.pipeServer != null)
			{
				OpenWebApplication.DebugSpew("Closing server pipe...", new object[]
				{
					this.id
				});
				this.oResponse.pipeServer.End();
				if (bNullThemToo)
				{
					this.oResponse.pipeServer = null;
				}
			}
		}
		public void Abort()
		{
			try
			{
				if (this.isAnyFlagSet(SessionFlags.IsBlindTunnel | SessionFlags.IsDecryptingTunnel | SessionFlags.IsWebSocketTunnel))
				{
					if (this.__oTunnel != null)
					{
						this.__oTunnel.CloseTunnel();
						this.oFlags["x-OpenWeb-Aborted"] = "true";
						this.state = SessionStates.Aborted;
					}
				}
				else
				{
					if (this.m_state < SessionStates.Done)
					{
						this.CloseSessionPipes(true);
						this.oFlags["x-OpenWeb-Aborted"] = "true";
						this.state = SessionStates.Aborted;
						this.ThreadResume();
					}
				}
			}
			catch (System.Exception)
			{
			}
		}
		[CodeDescription("Save HTTP response body to OpenWeb Captures folder.")]
		public bool SaveResponseBody()
		{
			string path = CONFIG.GetPath("Captures");
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			stringBuilder.Append(this.SuggestedFilename);
			while (System.IO.File.Exists(path + stringBuilder.ToString()))
			{
				stringBuilder.Insert(0, this.id.ToString() + "_");
			}
			stringBuilder.Insert(0, path);
			return this.SaveResponseBody(stringBuilder.ToString());
		}
		[CodeDescription("Save HTTP response body to specified location.")]
		public bool SaveResponseBody(string sFilename)
		{
			bool result;
			try
			{
				Utilities.WriteArrayToFile(sFilename, this.responseBodyBytes);
				result = true;
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser(ex.Message + "\n\n" + sFilename, "Save Failed");
				result = false;
			}
			return result;
		}
		[CodeDescription("Save HTTP request body to specified location.")]
		public bool SaveRequestBody(string sFilename)
		{
			bool result;
			try
			{
				Utilities.WriteArrayToFile(sFilename, this.requestBodyBytes);
				result = true;
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser(ex.Message + "\n\n" + sFilename, "Save Failed");
				result = false;
			}
			return result;
		}
		public void SaveSession(string sFilename, bool bHeadersOnly)
		{
			Utilities.EnsureOverwritable(sFilename);
			System.IO.FileStream fileStream = new System.IO.FileStream(sFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write);
			this.WriteToStream(fileStream, bHeadersOnly);
			fileStream.Close();
		}
		public void SaveRequest(string sFilename, bool bHeadersOnly)
		{
			this.SaveRequest(sFilename, bHeadersOnly, false);
		}
		public void SaveRequest(string sFilename, bool bHeadersOnly, bool bIncludeSchemeAndHostInPath)
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sFilename));
			System.IO.FileStream fileStream = new System.IO.FileStream(sFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write);
			if (this.oRequest.headers != null)
			{
				byte[] array = this.oRequest.headers.ToByteArray(true, true, bIncludeSchemeAndHostInPath, this.oFlags["X-OverrideHost"]);
				fileStream.Write(array, 0, array.Length);
				if (!bHeadersOnly && this.requestBodyBytes != null)
				{
					fileStream.Write(this.requestBodyBytes, 0, this.requestBodyBytes.Length);
				}
			}
			fileStream.Close();
		}
		public bool LoadMetadata(System.IO.Stream strmMetadata)
		{
			string a = XmlConvert.ToString(true);
			SessionFlags sessionFlags = SessionFlags.None;
			string text = null;
			bool result;
			try
			{
				XmlTextReader xmlTextReader = new XmlTextReader(strmMetadata);
				xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
				while (xmlTextReader.Read())
				{
					XmlNodeType nodeType = xmlTextReader.NodeType;
					string name;
					if (nodeType == XmlNodeType.Element && (name = xmlTextReader.Name) != null)
					{
						if (!(name == "Session"))
						{
							if (!(name == "SessionFlag"))
							{
								if (!(name == "SessionTimers"))
								{
									if (!(name == "TunnelInfo"))
									{
										if (name == "PipeInfo")
										{
											this.bBufferResponse = (a != xmlTextReader.GetAttribute("Streamed"));
											if (!this.bBufferResponse)
											{
												sessionFlags |= SessionFlags.ResponseStreamed;
											}
											if (a == xmlTextReader.GetAttribute("CltReuse"))
											{
												sessionFlags |= SessionFlags.ClientPipeReused;
											}
											if (a == xmlTextReader.GetAttribute("Reused"))
											{
												sessionFlags |= SessionFlags.ServerPipeReused;
											}
											if (this.oResponse != null)
											{
												this.oResponse._bWasForwarded = (a == xmlTextReader.GetAttribute("Forwarded"));
												if (this.oResponse._bWasForwarded)
												{
													sessionFlags |= SessionFlags.SentToGateway;
												}
											}
										}
									}
									else
									{
										long lngEgress = 0L;
										long lngIngress = 0L;
										if (long.TryParse(xmlTextReader.GetAttribute("BytesEgress"), out lngEgress) && long.TryParse(xmlTextReader.GetAttribute("BytesIngress"), out lngIngress))
										{
											this.__oTunnel = new MockTunnel(lngEgress, lngIngress);
										}
									}
								}
								else
								{
									this.Timers.ClientConnected = XmlConvert.ToDateTime(xmlTextReader.GetAttribute("ClientConnected"), XmlDateTimeSerializationMode.RoundtripKind);
									string attribute = xmlTextReader.GetAttribute("ClientBeginRequest");
									if (attribute != null)
									{
										this.Timers.ClientBeginRequest = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
									}
									attribute = xmlTextReader.GetAttribute("GotRequestHeaders");
									if (attribute != null)
									{
										this.Timers.OpenWebGotRequestHeaders = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
									}
									this.Timers.ClientDoneRequest = XmlConvert.ToDateTime(xmlTextReader.GetAttribute("ClientDoneRequest"), XmlDateTimeSerializationMode.RoundtripKind);
									attribute = xmlTextReader.GetAttribute("GatewayTime");
									if (attribute != null)
									{
										this.Timers.GatewayDeterminationTime = XmlConvert.ToInt32(attribute);
									}
									attribute = xmlTextReader.GetAttribute("DNSTime");
									if (attribute != null)
									{
										this.Timers.DNSTime = XmlConvert.ToInt32(attribute);
									}
									attribute = xmlTextReader.GetAttribute("TCPConnectTime");
									if (attribute != null)
									{
										this.Timers.TCPConnectTime = XmlConvert.ToInt32(attribute);
									}
									attribute = xmlTextReader.GetAttribute("HTTPSHandshakeTime");
									if (attribute != null)
									{
										this.Timers.HTTPSHandshakeTime = XmlConvert.ToInt32(attribute);
									}
									attribute = xmlTextReader.GetAttribute("ServerConnected");
									if (attribute != null)
									{
										this.Timers.ServerConnected = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
									}
									attribute = xmlTextReader.GetAttribute("OpenWebBeginRequest");
									if (attribute != null)
									{
										this.Timers.OpenWebBeginRequest = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
									}
									this.Timers.ServerGotRequest = XmlConvert.ToDateTime(xmlTextReader.GetAttribute("ServerGotRequest"), XmlDateTimeSerializationMode.RoundtripKind);
									attribute = xmlTextReader.GetAttribute("ServerBeginResponse");
									if (attribute != null)
									{
										this.Timers.ServerBeginResponse = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
									}
									attribute = xmlTextReader.GetAttribute("GotResponseHeaders");
									if (attribute != null)
									{
										this.Timers.OpenWebGotResponseHeaders = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
									}
									this.Timers.ServerDoneResponse = XmlConvert.ToDateTime(xmlTextReader.GetAttribute("ServerDoneResponse"), XmlDateTimeSerializationMode.RoundtripKind);
									this.Timers.ClientBeginResponse = XmlConvert.ToDateTime(xmlTextReader.GetAttribute("ClientBeginResponse"), XmlDateTimeSerializationMode.RoundtripKind);
									this.Timers.ClientDoneResponse = XmlConvert.ToDateTime(xmlTextReader.GetAttribute("ClientDoneResponse"), XmlDateTimeSerializationMode.RoundtripKind);
								}
							}
							else
							{
								this.oFlags.Add(xmlTextReader.GetAttribute("N"), xmlTextReader.GetAttribute("V"));
							}
						}
						else
						{
							if (xmlTextReader.GetAttribute("Aborted") != null)
							{
								this.m_state = SessionStates.Aborted;
							}
							if (xmlTextReader.GetAttribute("BitFlags") != null)
							{
								this.BitFlags = (SessionFlags)uint.Parse(xmlTextReader.GetAttribute("BitFlags"), System.Globalization.NumberStyles.HexNumber);
							}
							if (xmlTextReader.GetAttribute("SID") != null)
							{
								text = xmlTextReader.GetAttribute("SID");
							}
						}
					}
				}
				if (this.BitFlags == SessionFlags.None)
				{
					this.BitFlags = sessionFlags;
				}
				if (this.Timers.ClientBeginRequest.Ticks < 1L)
				{
					this.Timers.ClientBeginRequest = this.Timers.ClientConnected;
				}
				if (this.Timers.OpenWebBeginRequest.Ticks < 1L)
				{
					this.Timers.OpenWebBeginRequest = this.Timers.ServerGotRequest;
				}
				if (this.Timers.OpenWebGotRequestHeaders.Ticks < 1L)
				{
					this.Timers.OpenWebGotRequestHeaders = this.Timers.ClientBeginRequest;
				}
				if (this.Timers.OpenWebGotResponseHeaders.Ticks < 1L)
				{
					this.Timers.OpenWebGotResponseHeaders = this.Timers.ServerBeginResponse;
				}
				if (this.m_clientPort == 0 && this.oFlags.ContainsKey("X-ClientPort"))
				{
					int.TryParse(this.oFlags["X-ClientPort"], out this.m_clientPort);
				}
				if (text != null)
				{
					if (CONFIG.bReloadSessionIDAsFlag || this.oFlags.ContainsKey("ui-comments"))
					{
						this.oFlags["x-OriginalSessionID"] = text;
					}
					else
					{
						this.oFlags["ui-comments"] = string.Format("[#{0}]", text);
					}
				}
				xmlTextReader.Close();
				result = true;
			}
			catch (System.Exception eX)
			{
				OpenWebApplication.ReportException(eX);
				result = false;
			}
			return result;
		}
		public bool SaveMetadata(string sFilename)
		{
			bool result;
			try
			{
				System.IO.FileStream fileStream = new System.IO.FileStream(sFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write);
				this.WriteMetadataToStream(fileStream);
				fileStream.Close();
				result = true;
			}
			catch (System.Exception eX)
			{
				OpenWebApplication.ReportException(eX);
				result = false;
			}
			return result;
		}
		public void SaveResponse(string sFilename, bool bHeadersOnly)
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sFilename));
			System.IO.FileStream fileStream = new System.IO.FileStream(sFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write);
			if (this.oResponse.headers != null)
			{
				byte[] array = this.oResponse.headers.ToByteArray(true, true);
				fileStream.Write(array, 0, array.Length);
				if (!bHeadersOnly && this.responseBodyBytes != null)
				{
					fileStream.Write(this.responseBodyBytes, 0, this.responseBodyBytes.Length);
				}
			}
			fileStream.Close();
		}
		public void WriteMetadataToStream(System.IO.Stream strmMetadata)
		{
			XmlTextWriter xmlTextWriter = new XmlTextWriter(strmMetadata, System.Text.Encoding.UTF8);
			xmlTextWriter.Formatting = Formatting.Indented;
			xmlTextWriter.WriteStartDocument();
			xmlTextWriter.WriteStartElement("Session");
			xmlTextWriter.WriteAttributeString("SID", this.id.ToString());
			xmlTextWriter.WriteAttributeString("BitFlags", ((uint)this.BitFlags).ToString("x"));
			if (this.m_state == SessionStates.Aborted)
			{
				xmlTextWriter.WriteAttributeString("Aborted", XmlConvert.ToString(true));
			}
			xmlTextWriter.WriteStartElement("SessionTimers");
			xmlTextWriter.WriteAttributeString("ClientConnected", XmlConvert.ToString(this.Timers.ClientConnected, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ClientBeginRequest", XmlConvert.ToString(this.Timers.ClientBeginRequest, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("GotRequestHeaders", XmlConvert.ToString(this.Timers.OpenWebGotRequestHeaders, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ClientDoneRequest", XmlConvert.ToString(this.Timers.ClientDoneRequest, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("GatewayTime", XmlConvert.ToString(this.Timers.GatewayDeterminationTime));
			xmlTextWriter.WriteAttributeString("DNSTime", XmlConvert.ToString(this.Timers.DNSTime));
			xmlTextWriter.WriteAttributeString("TCPConnectTime", XmlConvert.ToString(this.Timers.TCPConnectTime));
			xmlTextWriter.WriteAttributeString("HTTPSHandshakeTime", XmlConvert.ToString(this.Timers.HTTPSHandshakeTime));
			xmlTextWriter.WriteAttributeString("ServerConnected", XmlConvert.ToString(this.Timers.ServerConnected, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("OpenWebBeginRequest", XmlConvert.ToString(this.Timers.OpenWebBeginRequest, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ServerGotRequest", XmlConvert.ToString(this.Timers.ServerGotRequest, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ServerBeginResponse", XmlConvert.ToString(this.Timers.ServerBeginResponse, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("GotResponseHeaders", XmlConvert.ToString(this.Timers.OpenWebGotResponseHeaders, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ServerDoneResponse", XmlConvert.ToString(this.Timers.ServerDoneResponse, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ClientBeginResponse", XmlConvert.ToString(this.Timers.ClientBeginResponse, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteAttributeString("ClientDoneResponse", XmlConvert.ToString(this.Timers.ClientDoneResponse, XmlDateTimeSerializationMode.RoundtripKind));
			xmlTextWriter.WriteEndElement();
			xmlTextWriter.WriteStartElement("PipeInfo");
			if (!this.bBufferResponse)
			{
				xmlTextWriter.WriteAttributeString("Streamed", XmlConvert.ToString(true));
			}
			if (this.oRequest != null && this.oRequest.bClientSocketReused)
			{
				xmlTextWriter.WriteAttributeString("CltReuse", XmlConvert.ToString(true));
			}
			if (this.oResponse != null)
			{
				if (this.oResponse.bServerSocketReused)
				{
					xmlTextWriter.WriteAttributeString("Reused", XmlConvert.ToString(true));
				}
				if (this.oResponse.bWasForwarded)
				{
					xmlTextWriter.WriteAttributeString("Forwarded", XmlConvert.ToString(true));
				}
			}
			xmlTextWriter.WriteEndElement();
			if (this.isTunnel && this.__oTunnel != null)
			{
				xmlTextWriter.WriteStartElement("TunnelInfo");
				xmlTextWriter.WriteAttributeString("BytesEgress", XmlConvert.ToString(this.__oTunnel.EgressByteCount));
				xmlTextWriter.WriteAttributeString("BytesIngress", XmlConvert.ToString(this.__oTunnel.IngressByteCount));
				xmlTextWriter.WriteEndElement();
			}
			xmlTextWriter.WriteStartElement("SessionFlags");
			foreach (string text in this.oFlags.Keys)
			{
				xmlTextWriter.WriteStartElement("SessionFlag");
				xmlTextWriter.WriteAttributeString("N", text);
				xmlTextWriter.WriteAttributeString("V", this.oFlags[text]);
				xmlTextWriter.WriteEndElement();
			}
			xmlTextWriter.WriteEndElement();
			xmlTextWriter.WriteEndElement();
			xmlTextWriter.WriteEndDocument();
			xmlTextWriter.Flush();
		}
		public bool WriteRequestToStream(bool bHeadersOnly, bool bIncludeProtocolAndHostWithPath, System.IO.Stream oFS)
		{
			return this.WriteRequestToStream(bHeadersOnly, bIncludeProtocolAndHostWithPath, false, oFS);
		}
		public bool WriteRequestToStream(bool bHeadersOnly, bool bIncludeProtocolAndHostWithPath, bool bEncodeIfBinary, System.IO.Stream oFS)
		{
			bool result;
			if (!Utilities.HasHeaders(this.oRequest))
			{
				result = false;
			}
			else
			{
				bool flag = bEncodeIfBinary && !bHeadersOnly && this.requestBodyBytes != null && Utilities.arrayContainsNonText(this.requestBodyBytes);
				HTTPRequestHeaders hTTPRequestHeaders = this.oRequest.headers;
				if (flag)
				{
					hTTPRequestHeaders = (HTTPRequestHeaders)hTTPRequestHeaders.Clone();
					hTTPRequestHeaders["OpenWeb-Encoding"] = "base64";
				}
				byte[] array = hTTPRequestHeaders.ToByteArray(true, true, bIncludeProtocolAndHostWithPath, this.oFlags["X-OverrideHost"]);
				oFS.Write(array, 0, array.Length);
				if (flag)
				{
					byte[] bytes = System.Text.Encoding.ASCII.GetBytes(System.Convert.ToBase64String(this.requestBodyBytes));
					oFS.Write(bytes, 0, bytes.Length);
					result = true;
				}
				else
				{
					if (!bHeadersOnly && !Utilities.IsNullOrEmpty(this.requestBodyBytes))
					{
						oFS.Write(this.requestBodyBytes, 0, this.requestBodyBytes.Length);
					}
					result = true;
				}
			}
			return result;
		}
		public bool WriteResponseToStream(System.IO.Stream oFS, bool bHeadersOnly)
		{
			bool result;
			if (!Utilities.HasHeaders(this.oResponse))
			{
				result = false;
			}
			else
			{
				byte[] array = this.oResponse.headers.ToByteArray(true, true);
				oFS.Write(array, 0, array.Length);
				if (!bHeadersOnly && !Utilities.IsNullOrEmpty(this.responseBodyBytes))
				{
					oFS.Write(this.responseBodyBytes, 0, this.responseBodyBytes.Length);
				}
				result = true;
			}
			return result;
		}
		internal bool WriteWebSocketMessagesToStream(System.IO.Stream oFS)
		{
			WebSocket webSocket = this.__oTunnel as WebSocket;
			return webSocket != null && webSocket.WriteWebSocketMessageListToStream(oFS);
		}
		[CodeDescription("Write the session (or session headers) to the specified stream")]
		public bool WriteToStream(System.IO.Stream oFS, bool bHeadersOnly)
		{
			bool result;
			try
			{
				this.WriteRequestToStream(bHeadersOnly, true, oFS);
				oFS.WriteByte(13);
				oFS.WriteByte(10);
				this.WriteResponseToStream(oFS, bHeadersOnly);
				result = true;
			}
			catch (System.Exception)
			{
				result = false;
			}
			return result;
		}
		[CodeDescription("Replace HTTP request headers and body using the specified file.")]
		public bool LoadRequestBodyFromFile(string sFilename)
		{
			bool result;
			if (!Utilities.HasHeaders(this.oRequest))
			{
				result = false;
			}
			else
			{
				sFilename = Utilities.EnsurePathIsAbsolute(CONFIG.GetPath("Requests"), sFilename);
				result = this.oRequest.ReadRequestBodyFromFile(sFilename);
			}
			return result;
		}
		private bool LoadResponse(System.IO.Stream strmResponse, string sResponseFile, string sOptionalContentTypeHint)
		{
			bool flag = string.IsNullOrEmpty(sResponseFile);
			this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
			this.responseBodyBytes = Utilities.emptyByteArray;
			this.bBufferResponse = true;
			this.BitFlags |= SessionFlags.ResponseGeneratedByOpenWeb;
			this.oFlags["x-OpenWeb-Generated"] = (flag ? "LoadResponseFromStream" : "LoadResponseFromFile");
			bool result;
			if (flag)
			{
				result = this.oResponse.ReadResponseFromStream(strmResponse, sOptionalContentTypeHint);
			}
			else
			{
				result = this.oResponse.ReadResponseFromFile(sResponseFile, sOptionalContentTypeHint);
			}
			if (this.HTTPMethodIs("HEAD"))
			{
				this.responseBodyBytes = Utilities.emptyByteArray;
			}
			if (this.m_state < SessionStates.AutoTamperResponseBefore)
			{
				this.state = SessionStates.AutoTamperResponseBefore;
			}
			return result;
		}
		public bool LoadResponseFromStream(System.IO.Stream strmResponse, string sOptionalContentTypeHint)
		{
			return this.LoadResponse(strmResponse, null, sOptionalContentTypeHint);
		}
		[CodeDescription("Replace HTTP response headers and body using the specified file.")]
		public bool LoadResponseFromFile(string sFilename)
		{
			sFilename = Utilities.GetFirstLocalResponse(sFilename);
			string sOptionalContentTypeHint = Utilities.ContentTypeForFilename(sFilename);
			return this.LoadResponse(null, sFilename, sOptionalContentTypeHint);
		}
		[CodeDescription("Return a string generated from the request body, decoding it and converting from a codepage if needed. Possibly expensive due to decompression and will throw on malformed content.")]
		public string GetRequestBodyAsString()
		{
			string result;
			if (!this._HasRequestBody() || !Utilities.HasHeaders(this.oRequest))
			{
				result = string.Empty;
			}
			else
			{
				byte[] array = Utilities.Dupe(this.requestBodyBytes);
				Utilities.utilDecodeHTTPBody(this.oRequest.headers, ref array);
				System.Text.Encoding entityBodyEncoding = Utilities.getEntityBodyEncoding(this.oRequest.headers, array);
				result = Utilities.GetStringFromArrayRemovingBOM(array, entityBodyEncoding);
			}
			return result;
		}
		[CodeDescription("Return a string generated from the response body, decoding it and converting from a codepage if needed. Possibly expensive due to decompression and will throw on malformed content.")]
		public string GetResponseBodyAsString()
		{
			string result;
			if (!this._HasResponseBody() || !Utilities.HasHeaders(this.oResponse))
			{
				result = string.Empty;
			}
			else
			{
				byte[] array = Utilities.Dupe(this.responseBodyBytes);
				Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref array);
				System.Text.Encoding entityBodyEncoding = Utilities.getEntityBodyEncoding(this.oResponse.headers, array);
				result = Utilities.GetStringFromArrayRemovingBOM(array, entityBodyEncoding);
			}
			return result;
		}
		[CodeDescription("Returns the Encoding of the requestBodyBytes")]
		public System.Text.Encoding GetRequestBodyEncoding()
		{
			return Utilities.getEntityBodyEncoding(this.oRequest.headers, this.requestBodyBytes);
		}
		[CodeDescription("Returns the Encoding of the responseBodyBytes")]
		public System.Text.Encoding GetResponseBodyEncoding()
		{
			return Utilities.getResponseBodyEncoding(this);
		}
		[CodeDescription("Returns true if request URI contains the specified string. Case-insensitive.")]
		public bool uriContains(string sLookfor)
		{
			return this.fullUrl.OICContains(sLookfor);
		}
		[CodeDescription("Removes chunking and HTTP Compression from the response. Adds or updates Content-Length header.")]
		public bool utilDecodeResponse()
		{
			return this.utilDecodeResponse(false);
		}
		public bool utilDecodeResponse(bool bSilent)
		{
			bool result;
			if (!Utilities.HasHeaders(this.oResponse) || (!this.oResponse.headers.Exists("Transfer-Encoding") && !this.oResponse.headers.Exists("Content-Encoding")))
			{
				result = false;
			}
			else
			{
				if (Utilities.isUnsupportedEncoding(this.oResponse.headers["Transfer-Encoding"], this.oResponse.headers["Content-Encoding"]))
				{
					result = false;
				}
				else
				{
					try
					{
						Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref this.responseBodyBytes);
						this.oResponse.headers.Remove("Transfer-Encoding");
						this.oResponse.headers.Remove("Content-Encoding");
						this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString());
					}
					catch (System.Exception eX)
					{
						if (CONFIG.bDebugSpew)
						{
							OpenWebApplication.DebugSpew("utilDecodeResponse failed. The HTTP response body was malformed. " + Utilities.DescribeException(eX));
						}
						if (!bSilent)
						{
							OpenWebApplication.ReportException(eX, "utilDecodeResponse failed for Session #" + this.id.ToString(), "The HTTP response body was malformed.");
						}
						this.oFlags["x-UtilDecodeResponse"] = Utilities.DescribeException(eX);
						this.oFlags["ui-backcolor"] = "LightYellow";
						result = false;
						return result;
					}
					result = true;
				}
			}
			return result;
		}
		[CodeDescription("Removes chunking and HTTP Compression from the Request. Adds or updates Content-Length header.")]
		public bool utilDecodeRequest()
		{
			return this.utilDecodeRequest(false);
		}
		public bool utilDecodeRequest(bool bSilent)
		{
			bool result;
			if (!Utilities.HasHeaders(this.oRequest) || (!this.oRequest.headers.Exists("Transfer-Encoding") && !this.oRequest.headers.Exists("Content-Encoding")))
			{
				result = false;
			}
			else
			{
				if (Utilities.isUnsupportedEncoding(this.oRequest.headers["Transfer-Encoding"], this.oRequest.headers["Content-Encoding"]))
				{
					result = false;
				}
				else
				{
					try
					{
						Utilities.utilDecodeHTTPBody(this.oRequest.headers, ref this.requestBodyBytes);
						this.oRequest.headers.Remove("Transfer-Encoding");
						this.oRequest.headers.Remove("Content-Encoding");
						this.oRequest.headers["Content-Length"] = ((this.requestBodyBytes == null) ? "0" : this.requestBodyBytes.LongLength.ToString());
					}
					catch (System.Exception eX)
					{
						if (!bSilent)
						{
							OpenWebApplication.ReportException(eX, "utilDecodeRequest failed for Session #" + this.id.ToString(), "The HTTP request body was malformed.");
						}
						this.oFlags["x-UtilDecodeRequest"] = Utilities.DescribeException(eX);
						this.oFlags["ui-backcolor"] = "LightYellow";
						result = false;
						return result;
					}
					result = true;
				}
			}
			return result;
		}
		[CodeDescription("Use GZIP to compress the request body. Throws exceptions to caller.")]
		public bool utilGZIPRequest()
		{
			bool result;
			if (!this._mayCompressRequest())
			{
				result = false;
			}
			else
			{
				this.requestBodyBytes = Utilities.GzipCompress(this.requestBodyBytes);
				this.oRequest.headers["Content-Encoding"] = "gzip";
				this.oRequest.headers["Content-Length"] = ((this.requestBodyBytes == null) ? "0" : this.requestBodyBytes.LongLength.ToString());
				result = true;
			}
			return result;
		}
		[CodeDescription("Use GZIP to compress the response body. Throws exceptions to caller.")]
		public bool utilGZIPResponse()
		{
			bool result;
			if (!this._mayCompressResponse())
			{
				result = false;
			}
			else
			{
				this.responseBodyBytes = Utilities.GzipCompress(this.responseBodyBytes);
				this.oResponse.headers["Content-Encoding"] = "gzip";
				this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString());
				result = true;
			}
			return result;
		}
		[CodeDescription("Use DEFLATE to compress the response body. Throws exceptions to caller.")]
		public bool utilDeflateResponse()
		{
			bool result;
			if (!this._mayCompressResponse())
			{
				result = false;
			}
			else
			{
				this.responseBodyBytes = Utilities.DeflaterCompress(this.responseBodyBytes);
				this.oResponse.headers["Content-Encoding"] = "deflate";
				this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString());
				result = true;
			}
			return result;
		}
		[CodeDescription("Use BZIP2 to compress the response body. Throws exceptions to caller.")]
		public bool utilBZIP2Response()
		{
			bool result;
			if (!this._mayCompressResponse())
			{
				result = false;
			}
			else
			{
				this.responseBodyBytes = Utilities.bzip2Compress(this.responseBodyBytes);
				this.oResponse.headers["Content-Encoding"] = "bzip2";
				this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString());
				result = true;
			}
			return result;
		}
		private bool _mayCompressRequest()
		{
			return this._HasRequestBody() && !this.oRequest.headers.Exists("Content-Encoding") && !this.oRequest.headers.Exists("Transfer-Encoding");
		}
		private bool _mayCompressResponse()
		{
			return this._HasResponseBody() && !this.oResponse.headers.Exists("Content-Encoding") && !this.oResponse.headers.Exists("Transfer-Encoding");
		}
		[CodeDescription("Apply Transfer-Encoding: chunked to the response, if possible.")]
		public bool utilChunkResponse(int iSuggestedChunkCount)
		{
			bool result;
			if (!Utilities.HasHeaders(this.oRequest) || !"HTTP/1.1".OICEquals(this.oRequest.headers.HTTPVersion) || this.HTTPMethodIs("HEAD") || this.HTTPMethodIs("CONNECT") || !Utilities.HasHeaders(this.oResponse) || !Utilities.HTTPStatusAllowsBody(this.oResponse.headers.HTTPResponseCode) || (this.responseBodyBytes != null && this.responseBodyBytes.LongLength > 2147483647L) || this.oResponse.headers.Exists("Transfer-Encoding"))
			{
				result = false;
			}
			else
			{
				this.responseBodyBytes = Utilities.doChunk(this.responseBodyBytes, iSuggestedChunkCount);
				this.oResponse.headers.Remove("Content-Length");
				this.oResponse.headers["Transfer-Encoding"] = "chunked";
				result = true;
			}
			return result;
		}
		[CodeDescription("Perform a case-sensitive string replacement on the request body (not URL!). Updates Content-Length header. Returns TRUE if replacements occur.")]
		public bool utilReplaceInRequest(string sSearchFor, string sReplaceWith)
		{
			bool result;
			if (!this._HasRequestBody() || !Utilities.HasHeaders(this.oRequest))
			{
				result = false;
			}
			else
			{
				string requestBodyAsString = this.GetRequestBodyAsString();
				string text = requestBodyAsString.Replace(sSearchFor, sReplaceWith);
				if (requestBodyAsString != text)
				{
					this.utilSetRequestBody(text);
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}
		[CodeDescription("Call inside OnBeforeRequest to create a Response object and bypass the server.")]
		public void utilCreateResponseAndBypassServer()
		{
			if (this.state > SessionStates.SendingRequest)
			{
				throw new System.InvalidOperationException("Too late, we're already talking to the server.");
			}
			this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
			this.responseBodyBytes = Utilities.emptyByteArray;
			this.oFlags["x-OpenWeb-Generated"] = "utilCreateResponseAndBypassServer";
			this.BitFlags |= SessionFlags.ResponseGeneratedByOpenWeb;
			this.bBufferResponse = true;
			this.state = SessionStates.AutoTamperResponseBefore;
		}
		[CodeDescription("Perform a regex-based replacement on the response body. Specify RegEx Options via leading Inline Flags, e.g. (?im) for case-Insensitive Multi-line. Updates Content-Length header. Note, you should call utilDecodeResponse first!  Returns TRUE if replacements occur.")]
		public bool utilReplaceRegexInResponse(string sSearchForRegEx, string sReplaceWithExpression)
		{
			bool result;
			if (!this._HasResponseBody())
			{
				result = false;
			}
			else
			{
				System.Text.Encoding responseBodyEncoding = Utilities.getResponseBodyEncoding(this);
				string @string = responseBodyEncoding.GetString(this.responseBodyBytes);
				string text = Regex.Replace(@string, sSearchForRegEx, sReplaceWithExpression, RegexOptions.ExplicitCapture | RegexOptions.Singleline);
				if (@string != text)
				{
					this.responseBodyBytes = responseBodyEncoding.GetBytes(text);
					this.oResponse["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}
		[CodeDescription("Perform a case-sensitive string replacement on the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!  Returns TRUE if replacements occur.")]
		public bool utilReplaceInResponse(string sSearchFor, string sReplaceWith)
		{
			return this._innerReplaceInResponse(sSearchFor, sReplaceWith, true, true);
		}
		[CodeDescription("Perform a single case-sensitive string replacement on the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first! Returns TRUE if replacements occur.")]
		public bool utilReplaceOnceInResponse(string sSearchFor, string sReplaceWith, bool bCaseSensitive)
		{
			return this._innerReplaceInResponse(sSearchFor, sReplaceWith, false, bCaseSensitive);
		}
		private bool _innerReplaceInResponse(string sSearchFor, string sReplaceWith, bool bReplaceAll, bool bCaseSensitive)
		{
			bool result;
			if (!this._HasResponseBody())
			{
				result = false;
			}
			else
			{
				System.Text.Encoding responseBodyEncoding = Utilities.getResponseBodyEncoding(this);
				string @string = responseBodyEncoding.GetString(this.responseBodyBytes);
				string text;
				if (bReplaceAll)
				{
					text = @string.Replace(sSearchFor, sReplaceWith);
				}
				else
				{
					int num = @string.IndexOf(sSearchFor, bCaseSensitive ? System.StringComparison.InvariantCulture : System.StringComparison.InvariantCultureIgnoreCase);
					if (num == 0)
					{
						text = sReplaceWith + @string.Substring(sSearchFor.Length);
					}
					else
					{
						if (num <= 0)
						{
							result = false;
							return result;
						}
						text = @string.Substring(0, num) + sReplaceWith + @string.Substring(num + sSearchFor.Length);
					}
				}
				if (@string != text)
				{
					this.responseBodyBytes = responseBodyEncoding.GetBytes(text);
					this.oResponse["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}
		[CodeDescription("Replaces the request body with sString as UTF8. Sets Content-Length header & removes Transfer-Encoding/Content-Encoding")]
		public void utilSetRequestBody(string sString)
		{
			if (sString == null)
			{
				sString = string.Empty;
			}
			this.oRequest.headers.Remove("Transfer-Encoding");
			this.oRequest.headers.Remove("Content-Encoding");
			this.requestBodyBytes = System.Text.Encoding.UTF8.GetBytes(sString);
			this.oRequest["Content-Length"] = this.requestBodyBytes.LongLength.ToString();
		}
		[CodeDescription("Replaces the response body with sString. Sets Content-Length header & removes Transfer-Encoding/Content-Encoding")]
		public void utilSetResponseBody(string sString)
		{
			if (sString == null)
			{
				sString = string.Empty;
			}
			this.oResponse.headers.Remove("Transfer-Encoding");
			this.oResponse.headers.Remove("Content-Encoding");
			System.Text.Encoding responseBodyEncoding = Utilities.getResponseBodyEncoding(this);
			this.responseBodyBytes = responseBodyEncoding.GetBytes(sString);
			this.oResponse["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
		}
		[CodeDescription("Prepend a string to the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!")]
		public void utilPrependToResponseBody(string sString)
		{
			if (this.responseBodyBytes == null)
			{
				this.responseBodyBytes = Utilities.emptyByteArray;
			}
			System.Text.Encoding responseBodyEncoding = Utilities.getResponseBodyEncoding(this);
			this.responseBodyBytes = Utilities.JoinByteArrays(responseBodyEncoding.GetBytes(sString), this.responseBodyBytes);
			this.oResponse.headers["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
		}
		[CodeDescription("Find a string in the request body. Return its index or -1.")]
		public int utilFindInRequest(string sSearchFor, bool bCaseSensitive)
		{
			int result;
			if (!this._HasRequestBody())
			{
				result = -1;
			}
			else
			{
				string @string = Utilities.getEntityBodyEncoding(this.oRequest.headers, this.requestBodyBytes).GetString(this.requestBodyBytes);
				result = @string.IndexOf(sSearchFor, bCaseSensitive ? System.StringComparison.InvariantCulture : System.StringComparison.InvariantCultureIgnoreCase);
			}
			return result;
		}
		private bool _HasRequestBody()
		{
			return !Utilities.IsNullOrEmpty(this.requestBodyBytes);
		}
		private bool _HasResponseBody()
		{
			return !Utilities.IsNullOrEmpty(this.responseBodyBytes);
		}
		[CodeDescription("Find a string in the response body. Return its index or -1. Note, you should call utilDecodeResponse first!")]
		public int utilFindInResponse(string sSearchFor, bool bCaseSensitive)
		{
			int result;
			if (!this._HasResponseBody())
			{
				result = -1;
			}
			else
			{
				string @string = Utilities.getResponseBodyEncoding(this).GetString(this.responseBodyBytes);
				result = @string.IndexOf(sSearchFor, bCaseSensitive ? System.StringComparison.InvariantCulture : System.StringComparison.InvariantCultureIgnoreCase);
			}
			return result;
		}
		[CodeDescription("Reset the SessionID counter to 0. This method can lead to confusing UI, so use sparingly.")]
		internal static void ResetSessionCounter()
		{
			System.Threading.Interlocked.Exchange(ref Session.cRequests, 0);
		}
		public Session(byte[] arrRequest, byte[] arrResponse) : this(arrRequest, arrResponse, SessionFlags.None)
		{
		}
		public Session(SessionData oSD) : this(oSD.arrRequest, oSD.arrResponse, SessionFlags.None)
		{
			this.LoadMetadata(new System.IO.MemoryStream(oSD.arrMetadata));
			if (oSD.arrWebSocketMessages != null && oSD.arrWebSocketMessages.Length > 0)
			{
				WebSocket.LoadWebSocketMessagesFromStream(this, new System.IO.MemoryStream(oSD.arrWebSocketMessages));
			}
		}
		public Session(byte[] arrRequest, byte[] arrResponse, SessionFlags oSF)
		{
			this.bBufferResponse = OpenWebApplication.Prefs.GetBoolPref("OpenWeb.ui.rules.bufferresponses", false);
			this.Timers = new SessionTimers();
			this._bAllowClientPipeReuse = true;
			this.oFlags = new StringDictionary();
			if (Utilities.IsNullOrEmpty(arrRequest))
			{
				throw new System.ArgumentException("Missing request data for session");
			}
			if (Utilities.IsNullOrEmpty(arrResponse))
			{
				arrResponse = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 0 OpenWeb GENERATED - RESPONSE DATA WAS MISSING\r\n\r\n");
			}
			this.state = SessionStates.Done;
			this.m_requestID = System.Threading.Interlocked.Increment(ref Session.cRequests);
			this.BitFlags = oSF;
			int count;
			int num;
			HTTPHeaderParseWarnings hTTPHeaderParseWarnings;
			if (!Parser.FindEntityBodyOffsetFromArray(arrRequest, out count, out num, out hTTPHeaderParseWarnings))
			{
				throw new InvalidDataException("Request corrupt, unable to find end of headers.");
			}
			int count2;
			int num2;
			if (!Parser.FindEntityBodyOffsetFromArray(arrResponse, out count2, out num2, out hTTPHeaderParseWarnings))
			{
				throw new InvalidDataException("Response corrupt, unable to find end of headers.");
			}
			this.requestBodyBytes = new byte[arrRequest.Length - num];
			this.responseBodyBytes = new byte[arrResponse.Length - num2];
			System.Buffer.BlockCopy(arrRequest, num, this.requestBodyBytes, 0, this.requestBodyBytes.Length);
			System.Buffer.BlockCopy(arrResponse, num2, this.responseBodyBytes, 0, this.responseBodyBytes.Length);
			string sData = CONFIG.oHeaderEncoding.GetString(arrRequest, 0, count) + "\r\n\r\n";
			string sHeaders = CONFIG.oHeaderEncoding.GetString(arrResponse, 0, count2) + "\r\n\r\n";
			this.oRequest = new ClientChatter(this, sData);
			this.oResponse = new ServerChatter(this, sHeaders);
		}
		internal Session(ClientPipe clientPipe, ServerPipe serverPipe)
		{
			this.bBufferResponse = OpenWebApplication.Prefs.GetBoolPref("OpenWeb.ui.rules.bufferresponses", false);
			this.Timers = new SessionTimers();
			this._bAllowClientPipeReuse = true;
			this.oFlags = new StringDictionary();
			if (CONFIG.bDebugSpew)
			{
				this.OnStateChanged += delegate(object s, StateChangeEventArgs ea)
				{
					OpenWebApplication.DebugSpew(string.Format("onstatechange>#{0} moving from state '{1}' to '{2}' {3}", new object[]
					{
						this.id.ToString(),
						ea.oldState,
						ea.newState,
						System.Environment.StackTrace
					}));
				};
			}
			if (clientPipe != null)
			{
				this.Timers.ClientConnected = clientPipe.dtAccepted;
				this.m_clientIP = ((clientPipe.Address == null) ? null : clientPipe.Address.ToString());
				this.m_clientPort = clientPipe.Port;
				this.oFlags["x-clientIP"] = this.m_clientIP;
				this.oFlags["x-clientport"] = this.m_clientPort.ToString();
				if (clientPipe.LocalProcessID != 0)
				{
					this._LocalProcessID = clientPipe.LocalProcessID;
					this.oFlags["x-ProcessInfo"] = string.Format("{0}:{1}", clientPipe.LocalProcessName, this._LocalProcessID);
				}
			}
			else
			{
				this.Timers.ClientConnected = System.DateTime.Now;
			}
			this.oResponse = new ServerChatter(this);
			this.oRequest = new ClientChatter(this);
			this.oRequest.pipeClient = clientPipe;
			this.oResponse.pipeServer = serverPipe;
		}
		public Session(HTTPRequestHeaders oRequestHeaders, byte[] arrRequestBody)
		{
			this.bBufferResponse = OpenWebApplication.Prefs.GetBoolPref("OpenWeb.ui.rules.bufferresponses", false);
			this.Timers = new SessionTimers();
			this._bAllowClientPipeReuse = true;
			this.oFlags = new StringDictionary();
			if (oRequestHeaders == null)
			{
				throw new System.ArgumentNullException("oRequestHeaders", "oRequestHeaders must not be null when creating a new Session.");
			}
			if (arrRequestBody == null)
			{
				arrRequestBody = Utilities.emptyByteArray;
			}
			if (CONFIG.bDebugSpew)
			{
				this.OnStateChanged += delegate(object s, StateChangeEventArgs ea)
				{
					OpenWebApplication.DebugSpew(string.Format("onstatechange>#{0} moving from state '{1}' to '{2}' {3}", new object[]
					{
						this.id.ToString(),
						ea.oldState,
						ea.newState,
						System.Environment.StackTrace
					}));
				};
			}
			this.Timers.ClientConnected = (this.Timers.ClientBeginRequest = (this.Timers.OpenWebGotRequestHeaders = System.DateTime.Now));
			this.m_clientIP = null;
			this.m_clientPort = 0;
			this.oFlags["x-clientIP"] = this.m_clientIP;
			this.oFlags["x-clientport"] = this.m_clientPort.ToString();
			this.oResponse = new ServerChatter(this);
			this.oRequest = new ClientChatter(this);
			this.oRequest.pipeClient = null;
			this.oResponse.pipeServer = null;
			this.oRequest.headers = oRequestHeaders;
			this.requestBodyBytes = arrRequestBody;
			this.m_state = SessionStates.AutoTamperRequestBefore;
		}
		public static Session BuildFromData(bool bClone, HTTPRequestHeaders headersRequest, byte[] arrRequestBody, HTTPResponseHeaders headersResponse, byte[] arrResponseBody, SessionFlags oSF)
		{
			if (headersRequest == null)
			{
				headersRequest = new HTTPRequestHeaders();
				headersRequest.HTTPMethod = "GET";
				headersRequest.HTTPVersion = "HTTP/1.1";
				headersRequest.UriScheme = "http";
				headersRequest.Add("Host", "localhost");
				headersRequest.RequestPath = "/" + System.DateTime.Now.Ticks.ToString();
			}
			else
			{
				if (bClone)
				{
					headersRequest = (HTTPRequestHeaders)headersRequest.Clone();
				}
			}
			if (headersResponse == null)
			{
				headersResponse = new HTTPResponseHeaders();
				headersResponse.SetStatus(200, "OK");
				headersResponse.HTTPVersion = "HTTP/1.1";
				headersResponse.Add("Connection", "close");
			}
			else
			{
				if (bClone)
				{
					headersResponse = (HTTPResponseHeaders)headersResponse.Clone();
				}
			}
			if (arrRequestBody == null)
			{
				arrRequestBody = Utilities.emptyByteArray;
			}
			else
			{
				if (bClone)
				{
					arrRequestBody = (byte[])arrRequestBody.Clone();
				}
			}
			if (arrResponseBody == null)
			{
				arrResponseBody = Utilities.emptyByteArray;
			}
			else
			{
				if (bClone)
				{
					arrResponseBody = (byte[])arrResponseBody.Clone();
				}
			}
			Session session = new Session(headersRequest, arrRequestBody);
			session._AssignID();
			session.SetBitFlag(oSF, true);
			session.oResponse.headers = headersResponse;
			session.responseBodyBytes = arrResponseBody;
			session.state = SessionStates.Done;
			return session;
		}
		internal void ExecuteUponAsyncRequest()
		{
			System.Threading.ThreadPool.UnsafeQueueUserWorkItem(new System.Threading.WaitCallback(this.Execute), null);
		}
		internal void Execute(object objThreadState)
		{
			try
			{
				this.InnerExecute();
				if (this.nextSession != null)
				{
					this.nextSession.ExecuteUponAsyncRequest();
					this.nextSession = null;
				}
			}
			catch (System.Exception eX)
			{
				OpenWebApplication.ReportException(eX, "Uncaught Exception in Session #" + this.id.ToString());
			}
		}
		private void InnerExecute()
		{
			if (this.oRequest != null && this.oResponse != null)
			{
				if (this._executeObtainRequest())
				{
					if (this.HTTPMethodIs("CONNECT"))
					{
						this.isTunnel = true;
						if (this.oFlags.ContainsKey("x-replywithtunnel"))
						{
							this._ReturnSelfGeneratedCONNECTTunnel(this.hostname);
							return;
						}
					}
					if (this.m_state >= SessionStates.ReadingResponse)
					{
						if (this.isAnyFlagSet(SessionFlags.ResponseGeneratedByOpenWeb))
						{
							OpenWebApplication.DoResponseHeadersAvailable(this);
						}
					}
					else
					{
						if (!this.oFlags.ContainsKey("x-replywithfile"))
						{
							if (this.port < 0 || this.port > 65535)
							{
								OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "HTTP Request specified an invalid port number.");
							}
							if (this._isDirectRequestToOpenWeb())
							{
								if (this.oRequest.headers.RequestPath.OICEndsWith(".pac"))
								{
									if (this.oRequest.headers.RequestPath.OICEndsWith("/proxy.pac"))
									{
										this._returnPACFileResponse();
										return;
									}
									if (this.oRequest.headers.RequestPath.OICEndsWith("/UpstreamProxy.pac"))
									{
										this._returnUpstreamPACFileResponse();
										return;
									}
								}
								if (this.oRequest.headers.RequestPath.OICEndsWith("/OpenWebroot.cer"))
								{
									Session._returnRootCert(this);
									return;
								}
								if (CONFIG.iReverseProxyForPort == 0)
								{
									this._returnEchoServiceResponse();
									return;
								}
								this.oFlags.Add("X-ReverseProxy", "1");
								this.host = string.Format("{0}:{1}", CONFIG.sReverseProxyHostname, CONFIG.iReverseProxyForPort);
							}
							while (true)
							{
								this.state = SessionStates.SendingRequest;
								if (!this.oResponse.ResendRequest())
								{
									break;
								}
								if (this.isAnyFlagSet(SessionFlags.RequestStreamed))
								{
									bool flag = false;
									GenericTunnel.CreateTunnel(this, flag);
									if (flag)
									{
										goto Block_21;
									}
								}
								this.Timers.ServerGotRequest = System.DateTime.Now;
								this.state = SessionStates.ReadingResponse;
								if (this.HTTPMethodIs("CONNECT") && !this.oResponse._bWasForwarded)
								{
									goto Block_23;
								}
								if (this.oResponse.ReadResponse())
								{
									goto Block_24;
								}
								if (!this._MayRetryWhenReceiveFailed())
								{
									goto Block_25;
								}
								OpenWebApplication.DebugSpew("[" + this.id.ToString() + "] ServerSocket Reuse failed. Restarting fresh.");
								StringDictionary stringDictionary;
								(stringDictionary = this.oFlags)["x-RetryOnFailedReceive"] = stringDictionary["x-RetryOnFailedReceive"] + "*";
								this.oResponse.Initialize(true);
							}
							this.CloseSessionPipes(true);
							this.state = SessionStates.Aborted;
							Block_21:
							return;
							Block_23:
							this.SetBitFlag(SessionFlags.ResponseGeneratedByOpenWeb, true);
							this.oResponse.headers = new HTTPResponseHeaders();
							this.oResponse.headers.HTTPVersion = this.oRequest.headers.HTTPVersion;
							this.oResponse.headers.SetStatus(200, "Connection Established");
							this.oResponse.headers.Add("OpenWebGateway", "Direct");
							this.oResponse.headers.Add("StartTime", System.DateTime.Now.ToString("HH:mm:ss.fff"));
							this.oResponse.headers.Add("Connection", "close");
							this.responseBodyBytes = Utilities.emptyByteArray;
							goto IL_6BC;
							Block_24:
							if (200 == this.responseCode && this.isAnyFlagSet(SessionFlags.RequestStreamed))
							{
								this.responseBodyBytes = this.oResponse.TakeEntity();
								try
								{
									this.oRequest.pipeClient.Send(this.oResponse.headers.ToByteArray(true, true));
									this.oRequest.pipeClient.Send(this.responseBodyBytes);
									(this.__oTunnel as GenericTunnel).BeginResponseStreaming();
								}
								catch (System.Exception var_3_5D9)
								{
								}
								return;
							}
							if (this.isAnyFlagSet(SessionFlags.ResponseBodyDropped))
							{
								this.responseBodyBytes = Utilities.emptyByteArray;
								this.oResponse.FreeResponseDataBuffer();
							}
							else
							{
								this.responseBodyBytes = this.oResponse.TakeEntity();
								long num;
								if (this.oResponse.headers.Exists("Content-Length") && !this.HTTPMethodIs("HEAD") && long.TryParse(this.oResponse.headers["Content-Length"], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out num) && num != this.responseBodyBytes.LongLength)
								{
									OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, true, true, string.Format("Content-Length mismatch: Response Header indicated {0:N0} bytes, but server sent {1:N0} bytes.", num, this.responseBodyBytes.LongLength));
								}
							}
							goto IL_6BB;
							Block_25:
							OpenWebApplication.DebugSpew("Failed to read server response and retry is forbidden. Aborting Session #{0}", new object[]
							{
								this.id
							});
							if (this.state != SessionStates.Aborted)
							{
								string text = string.Empty;
								if (!Utilities.IsNullOrEmpty(this.responseBodyBytes))
								{
									text = System.Text.Encoding.UTF8.GetString(this.responseBodyBytes);
								}
								text = string.Format("Server returned {0} bytes. {1}", this.oResponse.m_responseTotalDataCount, text);
								this.oRequest.FailSession(504, "OpenWeb - Receive Failure", string.Format("[OpenWeb] ReadResponse() failed: The server did not return a response for this request. {0}", text));
							}
							this.CloseSessionPipes(true);
							this.state = SessionStates.Aborted;
							return;
						}
						this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nServer: OpenWeb\r\n\r\n");
						if (this.LoadResponseFromFile(this.oFlags["x-replywithfile"]) && this.isAnyFlagSet(SessionFlags.ResponseGeneratedByOpenWeb))
						{
							OpenWebApplication.DoResponseHeadersAvailable(this);
						}
						this.oFlags["x-repliedwithfile"] = this.oFlags["x-replywithfile"];
						this.oFlags.Remove("x-replywithfile");
						IL_6BB:;
					}
					IL_6BC:
					this.oFlags["x-ResponseBodyTransferLength"] = ((this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString());
					this.state = SessionStates.AutoTamperResponseBefore;
					OpenWebApplication.DoBeforeResponse(this);
					if (!this._handledAsAutomaticRedirect())
					{
						if (!this._handledAsAutomaticAuth())
						{
							bool flag2 = false;
							if (this.m_state >= SessionStates.Done || this.isFlagSet(SessionFlags.ResponseStreamed))
							{
								this.FinishUISession(this.isFlagSet(SessionFlags.ResponseStreamed));
								if (this.isFlagSet(SessionFlags.ResponseStreamed) && this.oFlags.ContainsKey("log-drop-response-body"))
								{
									this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
									this.responseBodyBytes = Utilities.emptyByteArray;
								}
								flag2 = true;
							}
							if (flag2)
							{
								if (this.m_state < SessionStates.Done)
								{
									this.m_state = SessionStates.Done;
								}
								OpenWebApplication.DoAfterSessionComplete(this);
							}
							else
							{
								if (this.oFlags.ContainsKey("x-replywithfile"))
								{
									this.LoadResponseFromFile(this.oFlags["x-replywithfile"]);
									this.oFlags["x-replacedwithfile"] = this.oFlags["x-replywithfile"];
									this.oFlags.Remove("x-replywithfile");
								}
								this.state = SessionStates.AutoTamperResponseAfter;
							}
							bool flag3 = false;
							if (this._isResponseMultiStageAuthChallenge())
							{
								flag3 = this._isNTLMType2();
							}
							if (this.m_state >= SessionStates.Done)
							{
								this.FinishUISession();
								flag2 = true;
							}
							if (!flag2)
							{
								this.ReturnResponse(flag3);
							}
							if (flag2 && this.oRequest.pipeClient != null)
							{
								bool flag4 = flag3 || this._MayReuseMyClientPipe();
								if (flag4)
								{
									this._createNextSession(flag3);
								}
								else
								{
									this.oRequest.pipeClient.End();
								}
								this.oRequest.pipeClient = null;
							}
							this.oResponse.releaseServerPipe();
						}
					}
				}
			}
		}
		private bool _MayRetryWhenReceiveFailed()
		{
			bool result;
			if (!this.oResponse.bServerSocketReused || this.state == SessionStates.Aborted)
			{
				result = false;
			}
			else
			{
				switch (CONFIG.RetryOnReceiveFailure)
				{
				case RetryMode.Never:
					result = false;
					break;
				case RetryMode.IdempotentOnly:
					result = Utilities.HTTPMethodIsIdempotent(this.oRequest.headers.HTTPMethod);
					break;
				default:
					result = true;
					break;
				}
			}
			return result;
		}
		private bool _handledAsAutomaticAuth()
		{
			bool result;
			if (!this._isResponseAuthChallenge() || !this.oFlags.ContainsKey("x-AutoAuth") || this.oFlags.ContainsKey("x-AutoAuth-Failed"))
			{
				this.__WebRequestForAuth = null;
				result = false;
			}
			else
			{
				bool flag;
				try
				{
					flag = this._PerformInnerAuth();
				}
				catch (System.TypeLoadException var_1_4C)
				{
					flag = false;
				}
				result = flag;
			}
			return result;
		}
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private bool _PerformInnerAuth()
		{
			bool flag = 407 == this.oResponse.headers.HTTPResponseCode;
			bool result;
			try
			{
				Uri uri = new Uri(this.fullUrl);
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew("Performing automatic authentication to {0} in response to {1}", new object[]
					{
						uri,
						this.oResponse.headers.HTTPResponseCode
					});
				}
				if (this.__WebRequestForAuth == null)
				{
					this.__WebRequestForAuth = WebRequest.Create(uri);
				}
				System.Type type = this.__WebRequestForAuth.GetType();
				type.InvokeMember("Async", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty, null, this.__WebRequestForAuth, new object[]
				{
					false
				});
				object obj = type.InvokeMember("ServerAuthenticationState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetProperty, null, this.__WebRequestForAuth, new object[0]);
				if (obj == null)
				{
					throw new System.ApplicationException("Auth state is null");
				}
				System.Type type2 = obj.GetType();
				type2.InvokeMember("ChallengedUri", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, obj, new object[]
				{
					uri
				});
				string text = this.oFlags["X-AutoAuth-SPN"];
				if (text == null && !flag)
				{
					text = Session._GetSPNForUri(uri);
				}
				if (text != null)
				{
					if (CONFIG.bDebugSpew)
					{
						OpenWebApplication.DebugSpew("Authenticating to '{0}' with ChallengedSpn='{1}'", new object[]
						{
							uri,
							text
						});
					}
					bool flag2 = false;
					if (Session.bTrySPNTokenObject)
					{
						try
						{
							System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(typeof(AuthenticationManager));
							System.Type type3 = assembly.GetType("System.Net.SpnToken", true);
							object obj2 = System.Activator.CreateInstance(type3, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.CreateInstance, null, new string[]
							{
								text
							}, System.Globalization.CultureInfo.InvariantCulture);
							type2.InvokeMember("ChallengedSpn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, obj, new object[]
							{
								obj2
							});
							flag2 = true;
						}
						catch (System.Exception eX)
						{
							OpenWebApplication.DebugSpew(Utilities.DescribeException(eX));
							Session.bTrySPNTokenObject = false;
						}
					}
					if (!flag2)
					{
						type2.InvokeMember("ChallengedSpn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, obj, new object[]
						{
							text
						});
					}
				}
				try
				{
					if (this.oResponse.pipeServer.bIsSecured)
					{
						TransportContext transportContext = this.oResponse.pipeServer._GetTransportContext();
						if (transportContext != null)
						{
							type2.InvokeMember("_TransportContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetField, null, obj, new object[]
							{
								transportContext
							});
						}
					}
				}
				catch (System.Exception var_13_2C3)
				{
				}
				string challenge = flag ? this.oResponse["Proxy-Authenticate"] : this.oResponse["WWW-Authenticate"];
				ICredentials credentials;
				if (this.oFlags["x-AutoAuth"].Contains(":"))
				{
					string text2 = Utilities.TrimAfter(this.oFlags["x-AutoAuth"], ':');
					if (text2.Contains("\\"))
					{
						string domain = Utilities.TrimAfter(text2, '\\');
						text2 = Utilities.TrimBefore(text2, '\\');
						credentials = new NetworkCredential(text2, Utilities.TrimBefore(this.oFlags["x-AutoAuth"], ':'), domain);
					}
					else
					{
						credentials = new NetworkCredential(text2, Utilities.TrimBefore(this.oFlags["x-AutoAuth"], ':'));
					}
				}
				else
				{
					credentials = CredentialCache.DefaultCredentials;
				}
				this.__WebRequestForAuth.Method = this.RequestMethod;
				Authorization authorization = AuthenticationManager.Authenticate(challenge, this.__WebRequestForAuth, credentials);
				if (authorization == null)
				{
					throw new System.Exception("AuthenticationManager.Authenticate returned null.");
				}
				string message = authorization.Message;
				this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
				this.nextSession.propagateProcessInfo(this);
				this.FireContinueTransaction(this, this.nextSession, ContinueTransactionReason.Authenticate);
				if (!authorization.Complete)
				{
					this.nextSession.__WebRequestForAuth = this.__WebRequestForAuth;
				}
				this.__WebRequestForAuth = null;
				this.nextSession.requestBodyBytes = this.requestBodyBytes;
				this.nextSession.oRequest.headers = (HTTPRequestHeaders)this.oRequest.headers.Clone();
				this.nextSession.oRequest.headers[flag ? "Proxy-Authorization" : "Authorization"] = message;
				this.nextSession.SetBitFlag(SessionFlags.RequestGeneratedByOpenWeb, true);
				if (this.oFlags.ContainsKey("x-From-Builder"))
				{
					this.nextSession.oFlags["x-From-Builder"] = this.oFlags["x-From-Builder"] + " > +Auth";
				}
				int num;
				if (int.TryParse(this.oFlags["x-AutoAuth-Retries"], out num))
				{
					num--;
					if (num > 0)
					{
						this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
						this.nextSession.oFlags["x-AutoAuth-Retries"] = num.ToString();
					}
					else
					{
						this.nextSession.oFlags["x-AutoAuth-Failed"] = "true";
					}
				}
				else
				{
					this.nextSession.oFlags["x-AutoAuth-Retries"] = "5";
					this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
				}
				if (this.oFlags.ContainsKey("x-Builder-Inspect"))
				{
					this.nextSession.oFlags["x-Builder-Inspect"] = this.oFlags["x-Builder-Inspect"];
				}
				if (this.oFlags.ContainsKey("x-Builder-MaxRedir"))
				{
					this.nextSession.oFlags["x-Builder-MaxRedir"] = this.oFlags["x-Builder-MaxRedir"];
				}
				this.state = SessionStates.Done;
				this.nextSession.state = SessionStates.AutoTamperRequestBefore;
				this.FinishUISession(!this.bBufferResponse);
				result = true;
			}
			catch (System.Exception var_21_6A1)
			{
				this.__WebRequestForAuth = null;
				result = false;
			}
			return result;
		}
		internal void propagateProcessInfo(Session sessionFrom)
		{
			if (this._LocalProcessID == 0)
			{
				if (sessionFrom == null)
				{
					this._LocalProcessID = OpenWebApplication.iPID;
					this.oFlags["x-ProcessInfo"] = OpenWebApplication.sProcessInfo;
				}
				else
				{
					this._LocalProcessID = sessionFrom._LocalProcessID;
					if (sessionFrom.oFlags.ContainsKey("x-ProcessInfo"))
					{
						this.oFlags["x-ProcessInfo"] = sessionFrom.oFlags["x-ProcessInfo"];
					}
				}
			}
		}
		private static string _GetSPNForUri(Uri uriTarget)
		{
			int int32Pref = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.auth.SPNMode", 3);
			string result;
			string text;
			switch (int32Pref)
			{
			case 0:
				result = null;
				return result;
			case 1:
				text = uriTarget.DnsSafeHost;
				break;
			default:
				text = uriTarget.DnsSafeHost;
				if (int32Pref == 3 || (uriTarget.HostNameType != UriHostNameType.IPv6 && uriTarget.HostNameType != UriHostNameType.IPv4 && text.IndexOf('.') == -1))
				{
					string canonicalName = DNSResolver.GetCanonicalName(uriTarget.DnsSafeHost);
					if (!string.IsNullOrEmpty(canonicalName))
					{
						text = canonicalName;
					}
				}
				break;
			}
			text = "HTTP/" + text;
			if (uriTarget.Port != 80 && uriTarget.Port != 443 && OpenWebApplication.Prefs.GetBoolPref("OpenWeb.auth.SPNIncludesPort", false))
			{
				text = text + ":" + uriTarget.Port.ToString();
			}
			result = text;
			return result;
		}
		public string GetRedirectTargetURL()
		{
			string result;
			if (!Utilities.IsRedirectStatus(this.responseCode) || !Utilities.HasHeaders(this.oResponse))
			{
				result = null;
			}
			else
			{
				result = Session.GetRedirectTargetURL(this.fullUrl, this.oResponse["Location"]);
			}
			return result;
		}
		public static string GetRedirectTargetURL(string sBase, string sLocation)
		{
			int num = sLocation.IndexOf(":");
			if (num >= 0)
			{
				if (sLocation.IndexOfAny(new char[]
				{
					'/',
					'?',
					'#'
				}) >= num)
				{
					goto IL_61;
				}
			}
			string result;
			try
			{
				Uri baseUri = new Uri(sBase);
				Uri uri = new Uri(baseUri, sLocation);
				string text = uri.ToString();
				result = text;
				return result;
			}
			catch (UriFormatException)
			{
				string text = null;
				result = text;
				return result;
			}
			IL_61:
			result = sLocation;
			return result;
		}
		private static bool isRedirectableURI(string sBase, string sLocation, out string sTarget)
		{
			sTarget = Session.GetRedirectTargetURL(sBase, sLocation);
			return sTarget != null && sTarget.OICStartsWithAny(new string[]
			{
				"http://",
				"https://",
				"ftp://"
			});
		}
		private bool _handledAsAutomaticRedirect()
		{
			bool result;
			if (this.oResponse.headers.HTTPResponseCode < 300 || this.oResponse.headers.HTTPResponseCode > 308 || !this.oFlags.ContainsKey("x-Builder-MaxRedir") || !this.oResponse.headers.Exists("Location"))
			{
				result = false;
			}
			else
			{
				string text;
				if (!Session.isRedirectableURI(this.fullUrl, this.oResponse["Location"], out text))
				{
					result = false;
				}
				else
				{
					this.nextSession = new Session(this.oRequest.pipeClient, null);
					this.nextSession.propagateProcessInfo(this);
					this.FireContinueTransaction(this, this.nextSession, ContinueTransactionReason.Redirect);
					this.nextSession.oRequest.headers = (HTTPRequestHeaders)this.oRequest.headers.Clone();
					this.oResponse.releaseServerPipe();
					text = Utilities.TrimAfter(text, '#');
					try
					{
						this.nextSession.fullUrl = new Uri(text).AbsoluteUri;
					}
					catch (UriFormatException arg)
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("Redirect's Location header was malformed.\nLocation: {0}\n\n{1}", text, arg));
						this.nextSession.fullUrl = text;
					}
					if (this.oResponse.headers.HTTPResponseCode == 307 || this.oResponse.headers.HTTPResponseCode == 308)
					{
						this.nextSession.requestBodyBytes = Utilities.Dupe(this.requestBodyBytes);
					}
					else
					{
						if (!this.nextSession.HTTPMethodIs("HEAD"))
						{
							this.nextSession.RequestMethod = "GET";
						}
						this.nextSession.oRequest.headers.Remove("Content-Length");
						this.nextSession.oRequest.headers.Remove("Transfer-Encoding");
						this.nextSession.requestBodyBytes = Utilities.emptyByteArray;
					}
					this.nextSession.SetBitFlag(SessionFlags.RequestGeneratedByOpenWeb, true);
					if (this.oFlags.ContainsKey("x-From-Builder"))
					{
						this.nextSession.oFlags["x-From-Builder"] = this.oFlags["x-From-Builder"] + " > +Redir";
					}
					if (this.oFlags.ContainsKey("x-AutoAuth"))
					{
						this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
					}
					if (this.oFlags.ContainsKey("x-Builder-Inspect"))
					{
						this.nextSession.oFlags["x-Builder-Inspect"] = this.oFlags["x-Builder-Inspect"];
					}
					int num;
					if (int.TryParse(this.oFlags["x-Builder-MaxRedir"], out num))
					{
						num--;
						if (num > 0)
						{
							this.nextSession.oFlags["x-Builder-MaxRedir"] = num.ToString();
						}
					}
					this.nextSession.state = SessionStates.AutoTamperRequestBefore;
					this.state = SessionStates.Done;
					this.FinishUISession(!this.bBufferResponse);
					result = true;
				}
			}
			return result;
		}
		private void ExecuteHTTPLintOnRequest()
		{
			if (this.fullUrl.Length > 2083)
			{
				OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("[HTTPLint Warning] Request URL was {0} characters. WinINET-based clients encounter problems when dealing with URLs longer than 2083 characters.", this.fullUrl.Length));
			}
			if (this.oRequest.headers != null)
			{
				if (this.oRequest.headers.ByteCount() > 16000)
				{
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("[HTTPLint Warning] Request headers were {0} bytes long. Many servers will reject requests this large.", this.oRequest.headers.ByteCount()));
				}
			}
		}
		private void ExecuteHTTPLintOnResponse()
		{
			if (this.responseBodyBytes != null && this.oResponse.headers != null)
			{
				if (this.oResponse.headers.Exists("Content-Encoding"))
				{
					if (this.oResponse.headers.ExistsAndContains("Content-Encoding", ","))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint Warning] Response appears to specify multiple encodings: '{0}'. This will prevent decoding in Internet Explorer.", this.oResponse.headers["Content-Encoding"]));
					}
					if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "gzip") && this.oRequest != null && this.oRequest.headers != null && !this.oRequest.headers.ExistsAndContains("Accept-Encoding", "gzip"))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] Illegal response. Response specified Content-Encoding: gzip, but request did not specify GZIP in Accept-Encoding.");
					}
					if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "deflate") && this.oRequest != null && this.oRequest.headers != null && !this.oRequest.headers.ExistsAndContains("Accept-Encoding", "deflate"))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] Illegal response. Response specified Content-Encoding: Deflate, but request did not specify Deflate in Accept-Encoding.");
					}
					if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "chunked"))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] Response specified Content-Encoding: chunked, but Chunked is a Transfer-Encoding.");
					}
				}
				if (this.oResponse.headers.ExistsAndContains("Transfer-Encoding", "chunked"))
				{
					if (Utilities.HasHeaders(this.oRequest) && "HTTP/1.0".OICEquals(this.oRequest.headers.HTTPVersion))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] Invalid response. Responses to HTTP/1.0 clients MUST NOT specify a Transfer-Encoding.");
					}
					if (this.oResponse.headers.Exists("Content-Length"))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] Invalid response headers. Messages MUST NOT include both a Content-Length header field and a non-identity transfer-coding.");
					}
					long num = 0L;
					long num2 = (long)this.responseBodyBytes.Length;
					if (!Utilities.IsChunkedBodyComplete(this, this.responseBodyBytes, 0L, (long)this.responseBodyBytes.Length, out num, out num2))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] The HTTP Chunked response body was incomplete; most likely lacking the final 0-size chunk.");
					}
				}
				System.Collections.Generic.List<HTTPHeaderItem> list = this.oResponse.headers.FindAll("ETAG");
				if (list.Count > 1)
				{
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint] Response contained {0} ETag headers", list.Count));
				}
				if (list.Count > 0)
				{
					string value = list[0].Value;
					if (!value.EndsWith("\"") || (!value.StartsWith("\"") && !value.StartsWith("W/\"")))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint] ETag values must be a quoted string. Response ETag: {0}", value));
					}
				}
				if (!this.oResponse.headers.Exists("Date") && this.responseCode != 100 && this.responseCode != 101 && this.responseCode < 500 && !this.HTTPMethodIs("CONNECT"))
				{
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] With rare exceptions, servers MUST include a DATE response header. RFC2616 Section 14.18");
				}
				if (this.responseCode != 304 && this.responseCode > 299 && this.responseCode < 399)
				{
					if (this.oResponse.headers.Exists("Location"))
					{
						if (this.oResponse["Location"].StartsWith("/"))
						{
							OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint] HTTP Location header must specify a fully-qualified URL. Location: {0}", this.oResponse["Location"]));
						}
					}
					else
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] HTTP/3xx redirect response headers lacked a Location header.");
					}
				}
				if (this.oResponse.headers.ExistsAndContains("Content-Type", "utf8"))
				{
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint] Content-Type header specified UTF-8 incorrectly. CharSet=UTF-8 is valid, CharSet=UTF8 is not.");
				}
				System.Collections.Generic.List<HTTPHeaderItem> list2 = this.oResponse.headers.FindAll("Set-Cookie");
				if (list2.Count > 0)
				{
					if (this.hostname.Contains("_"))
					{
						OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint Warning] Response sets a cookie, and server's hostname contains '_'. Internet Explorer does not permit cookies to be set on hostnames containing underscores. See http://support.microsoft.com/kb/316112");
					}
					foreach (HTTPHeaderItem current in list2)
					{
						string text = Utilities.GetCommaTokenValue(current.Value, "domain");
						if (!string.IsNullOrEmpty(text))
						{
							if (text.StartsWith("."))
							{
								text = text.Substring(1);
							}
							if (!this.hostname.EndsWith(text))
							{
								OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint] Illegal DOMAIN in Set-Cookie. Cookie from {0} specified 'domain={1}'", this.hostname, text));
							}
						}
						string text2 = Utilities.TrimAfter(current.Value, ';');
						string text3 = text2;
						for (int i = 0; i < text3.Length; i++)
						{
							char c = text3[i];
							if (c == ',')
							{
								OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint] Illegal comma in cookie. Set-Cookie: {0}.", text2));
							}
							else
							{
								if (c >= '\u0080')
								{
									OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint] Non-ASCII character found in Set-Cookie: {0}. Some browsers (Safari) may corrupt this cookie.", text2));
								}
							}
						}
					}
				}
			}
		}
		internal void _AssignID()
		{
			this.m_requestID = System.Threading.Interlocked.Increment(ref Session.cRequests);
		}
		private bool _executeObtainRequest()
		{
			bool result;
			if (this.state > SessionStates.ReadingRequest)
			{
				this.Timers.ClientBeginRequest = (this.Timers.OpenWebGotRequestHeaders = (this.Timers.ClientDoneRequest = System.DateTime.Now));
				this._AssignID();
			}
			else
			{
				this.state = SessionStates.ReadingRequest;
				if (!this.oRequest.ReadRequest())
				{
					if (this.oResponse != null)
					{
						this.oResponse._detachServerPipe();
					}
					if (this.oRequest.headers == null)
					{
						this.oFlags["ui-hide"] = "stealth-NewOrReusedClosedWithoutRequest";
					}
					this.CloseSessionPipes(true);
					this.state = SessionStates.Aborted;
					result = false;
					return result;
				}
				this.Timers.ClientDoneRequest = System.DateTime.Now;
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew(string.Format("Session ID #{0} for request read from {1}.", this.m_requestID, this.oRequest.pipeClient));
				}
				try
				{
					this.requestBodyBytes = this.oRequest.TakeEntity();
				}
				catch (System.Exception eX)
				{
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "Failed to obtain request body. " + Utilities.DescribeException(eX));
					this.CloseSessionPipes(true);
					this.state = SessionStates.Aborted;
					result = false;
					return result;
				}
			}
			this._replaceVirtualHostnames();
			if (Utilities.IsNullOrEmpty(this.requestBodyBytes) && Utilities.HTTPMethodRequiresBody(this.RequestMethod))
			{
				OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "This HTTP method requires a request body.");
			}
			string text = this.oFlags["X-Original-Host"];
			if (text != null)
			{
				if (string.Empty == text)
				{
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, "HTTP/1.1 Request was missing the required HOST header.");
				}
				else
				{
					if (!OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.SetHostHeaderFromURL", true))
					{
						this.oFlags["X-OverrideHost"] = this.oFlags["X-URI-Host"];
					}
					OpenWebApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("The Request's Host header did not match the URL's host component.\n\nURL Host:\t{0}\nHeader Host:\t{1}", this.oFlags["X-URI-Host"], this.oFlags["X-Original-Host"]));
				}
			}
			if (this.isHTTPS)
			{
				this.SetBitFlag(SessionFlags.IsHTTPS, true);
				this.SetBitFlag(SessionFlags.IsFTP, false);
			}
			else
			{
				if (this.isFTP)
				{
					this.SetBitFlag(SessionFlags.IsFTP, true);
					this.SetBitFlag(SessionFlags.IsHTTPS, false);
				}
			}
			this.state = SessionStates.AutoTamperRequestBefore;
			OpenWebApplication.DoBeforeRequest(this);
			if (this.m_state >= SessionStates.Done)
			{
				this.FinishUISession();
				result = false;
			}
			else
			{
				if (this.m_state < SessionStates.AutoTamperRequestAfter)
				{
					this.state = SessionStates.AutoTamperRequestAfter;
				}
				result = (this.m_state < SessionStates.Done);
			}
			return result;
		}
		private bool _isResponseMultiStageAuthChallenge()
		{
			return (401 == this.oResponse.headers.HTTPResponseCode && this.oResponse.headers["WWW-Authenticate"].OICStartsWith("N")) || (407 == this.oResponse.headers.HTTPResponseCode && this.oResponse.headers["Proxy-Authenticate"].OICStartsWith("N"));
		}
		private bool _isResponseAuthChallenge()
		{
			bool result;
			if (401 == this.oResponse.headers.HTTPResponseCode)
			{
				result = (this.oResponse.headers.ExistsAndContains("WWW-Authenticate", "NTLM") || this.oResponse.headers.ExistsAndContains("WWW-Authenticate", "Negotiate") || this.oResponse.headers.ExistsAndContains("WWW-Authenticate", "Digest"));
			}
			else
			{
				result = (407 == this.oResponse.headers.HTTPResponseCode && (this.oResponse.headers.ExistsAndContains("Proxy-Authenticate", "NTLM") || this.oResponse.headers.ExistsAndContains("Proxy-Authenticate", "Negotiate") || this.oResponse.headers.ExistsAndContains("Proxy-Authenticate", "Digest")));
			}
			return result;
		}
		private void _replaceVirtualHostnames()
		{
			if (this.hostname.OICEndsWith(".OpenWeb"))
			{
				string text = this.hostname.ToLowerInvariant();
				string a;
				if ((a = text) != null)
				{
					if (!(a == "ipv4.OpenWeb"))
					{
						if (!(a == "localhost.OpenWeb"))
						{
							if (!(a == "ipv6.OpenWeb"))
							{
								return;
							}
							this.hostname = "[::1]";
						}
						else
						{
							this.hostname = "localhost";
						}
					}
					else
					{
						this.hostname = "127.0.0.1";
					}
					this.oFlags["x-UsedVirtualHost"] = text;
					this.bypassGateway = true;
					if (this.HTTPMethodIs("CONNECT"))
					{
						this.oFlags["x-OverrideCertCN"] = text;
					}
				}
			}
		}
		private bool _isDirectRequestToOpenWeb()
		{
			bool result;
			if (this.port != CONFIG.ListenPort)
			{
				result = false;
			}
			else
			{
				if (string.Equals(CONFIG.sOpenWebListenHostPort, this.host, System.StringComparison.OrdinalIgnoreCase))
				{
					result = true;
				}
				else
				{
					string text = this.hostname.ToLowerInvariant();
					if (text == "localhost" || text == "localhost." || text == CONFIG.sAlternateHostname)
					{
						result = true;
					}
					else
					{
						if (text.StartsWith("[") && text.EndsWith("]"))
						{
							text = text.Substring(1, text.Length - 2);
						}
						IPAddress iPAddress = Utilities.IPFromString(text);
						if (iPAddress != null)
						{
							try
							{
								if (IPAddress.IsLoopback(iPAddress))
								{
									bool flag = true;
									result = flag;
									return result;
								}
								IPAddress[] hostAddresses = Dns.GetHostAddresses(string.Empty);
								IPAddress[] array = hostAddresses;
								for (int i = 0; i < array.Length; i++)
								{
									IPAddress iPAddress2 = array[i];
									if (iPAddress2.Equals(iPAddress))
									{
										bool flag = true;
										result = flag;
										return result;
									}
								}
							}
							catch (System.Exception)
							{
							}
							result = false;
						}
						else
						{
							result = (text.StartsWith(CONFIG.sMachineName) && (text.Length == CONFIG.sMachineName.Length || text == CONFIG.sMachineName + "." + CONFIG.sMachineDomain));
						}
					}
				}
			}
			return result;
		}
		private void _returnEchoServiceResponse()
		{
			if (!OpenWebApplication.Prefs.GetBoolPref("OpenWeb.echoservice.enabled", true))
			{
				if (this.oRequest != null && this.oRequest.pipeClient != null)
				{
					this.oRequest.pipeClient.EndWithRST();
				}
				this.state = SessionStates.Aborted;
			}
			else
			{
				if (this.HTTPMethodIs("CONNECT"))
				{
					this.oRequest.FailSession(405, "Method Not Allowed", "This endpoint does not support HTTP CONNECTs. Try GET or POST instead.");
				}
				else
				{
					int num = 200;
					System.Action<Session> delLastChance = null;
					if (this.PathAndQuery.Length == 4 && Regex.IsMatch(this.PathAndQuery, "/\\d{3}"))
					{
						num = int.Parse(this.PathAndQuery.Substring(1));
						if (Utilities.IsRedirectStatus(num))
						{
							delLastChance = delegate(Session s)
							{
								s.oResponse["Location"] = "/200";
							};
						}
					}
					System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
					stringBuilder.AppendFormat("<!doctype html>\n<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><title>", new object[0]);
					if (num != 200)
					{
						stringBuilder.AppendFormat("[{0}] - ", num);
					}
					stringBuilder.Append("OpenWeb Echo Service</title></head><body style=\"font-family: arial,sans-serif;\"><h1>OpenWeb Echo Service</h1><br /><pre>");
					stringBuilder.Append(Utilities.HtmlEncode(this.oRequest.headers.ToString(true, true)));
					if (this.requestBodyBytes != null && this.requestBodyBytes.LongLength > 0L)
					{
						stringBuilder.Append(Utilities.HtmlEncode(System.Text.Encoding.UTF8.GetString(this.requestBodyBytes)));
					}
					stringBuilder.Append("</pre>");
					stringBuilder.AppendFormat("This page returned a <b>HTTP/{0}</b> response <br />", num);
					if (this.oFlags.ContainsKey("X-ProcessInfo"))
					{
						stringBuilder.AppendFormat("Originating Process Information: <code>{0}</code><br />", this.oFlags["X-ProcessInfo"]);
					}
					stringBuilder.Append("<hr /><ul><li>To configure OpenWeb as a reverse proxy instead of seeing this page, see <a href='" + CONFIG.GetRedirUrl("REVERSEPROXY") + "'>Reverse Proxy Setup</a><li>You can download the <a href=\"OpenWebRoot.cer\">OpenWebRoot certificate</a></ul></body></html>");
					this.oRequest.BuildAndReturnResponse(num, "OpenWeb Generated", stringBuilder.ToString(), delLastChance);
					this.state = SessionStates.Aborted;
				}
			}
		}
		private void _returnPACFileResponse()
		{
			this.utilCreateResponseAndBypassServer();
			this.oResponse.headers["Content-Type"] = "application/x-ns-proxy-autoconfig";
			this.oResponse.headers["Cache-Control"] = "max-age=60";
			this.oResponse.headers["Connection"] = "close";
			this.utilSetResponseBody(OpenWebApplication.oProxy._GetPACScriptText(OpenWebApplication.oProxy.IsAttached));
			this.state = SessionStates.Aborted;
			OpenWebApplication.DoResponseHeadersAvailable(this);
			this.ReturnResponse(false);
		}
		private void _returnUpstreamPACFileResponse()
		{
			this.utilCreateResponseAndBypassServer();
			this.oResponse.headers["Content-Type"] = "application/x-ns-proxy-autoconfig";
			this.oResponse.headers["Connection"] = "close";
			this.oResponse.headers["Cache-Control"] = "max-age=300";
			string text = OpenWebApplication.oProxy._GetUpstreamPACScriptText();
			if (string.IsNullOrEmpty(text))
			{
				this.responseCode = 404;
			}
			this.utilSetResponseBody(text);
			this.state = SessionStates.Aborted;
			this.ReturnResponse(false);
		}
		private static void _returnRootCert(Session oS)
		{
			oS.utilCreateResponseAndBypassServer();
			oS.oResponse.headers["Connection"] = "close";
			oS.oResponse.headers["Cache-Control"] = "max-age=0";
			byte[] array = null;
			if (array != null)
			{
				oS.oResponse.headers["Content-Type"] = "application/x-x509-ca-cert";
				oS.responseBodyBytes = array;
				oS.oResponse.headers["Content-Length"] = oS.responseBodyBytes.Length.ToString();
			}
			else
			{
				oS.responseCode = 404;
				oS.oResponse.headers["Content-Type"] = "text/html; charset=UTF-8";
				oS.utilSetResponseBody("No root certificate was found. Have you enabled HTTPS traffic decryption in OpenWeb yet?".PadRight(512, ' '));
			}
			OpenWebApplication.DoResponseHeadersAvailable(oS);
			oS.ReturnResponse(false);
		}
		private void _ReturnSelfGeneratedCONNECTTunnel(string sHostname)
		{
			this.SetBitFlag(SessionFlags.ResponseGeneratedByOpenWeb | SessionFlags.IsDecryptingTunnel, true);
			this.oResponse.headers = new HTTPResponseHeaders();
			this.oResponse.headers.SetStatus(200, "DecryptEndpoint Created");
			this.oResponse.headers.Add("Timestamp", System.DateTime.Now.ToString("HH:mm:ss.fff"));
			this.oResponse.headers.Add("OpenWebGateway", "AutoResponder");
			this.oResponse.headers.Add("Connection", "close");
			this.responseBodyBytes = System.Text.Encoding.UTF8.GetBytes("This is a OpenWeb-generated response to the client's request for a CONNECT tunnel.\n\n");
			this.oFlags["ui-backcolor"] = "Lavender";
			OpenWebApplication.DoBeforeResponse(this);
			this.state = SessionStates.Done;
			OpenWebApplication.DoAfterSessionComplete(this);
			if (CONFIG.bUseSNIForCN && !this.oFlags.ContainsKey("x-OverrideCertCN"))
			{
				string text = this.oFlags["https-Client-SNIHostname"];
				if (!string.IsNullOrEmpty(text) && text != sHostname)
				{
					this.oFlags["x-OverrideCertCN"] = this.oFlags["https-Client-SNIHostname"];
				}
			}
			string sHostname2 = this.oFlags["x-OverrideCertCN"] ?? sHostname;
			if (this.oRequest.pipeClient == null || !this.oRequest.pipeClient.SecureClientPipe(sHostname2, this.oResponse.headers))
			{
				this.CloseSessionPipes(false);
			}
			else
			{
				Session session = new Session(this.oRequest.pipeClient, null);
				this.oRequest.pipeClient = null;
				session.oFlags["x-serversocket"] = "AUTO-RESPONDER-GENERATED";
				session.Execute(null);
			}
		}
		private bool _isNTLMType2()
		{
			if (!this.oFlags.ContainsKey("x-SuppressProxySupportHeader"))
			{
				this.oResponse.headers["Proxy-Support"] = "Session-Based-Authentication";
			}
			bool result;
			if (407 == this.oResponse.headers.HTTPResponseCode)
			{
				if (this.oRequest.headers["Proxy-Authorization"].Length < 1)
				{
					result = false;
					return result;
				}
				if (!this.oResponse.headers.Exists("Proxy-Authenticate") || this.oResponse.headers["Proxy-Authenticate"].Length < 6)
				{
					result = false;
					return result;
				}
			}
			else
			{
				if (string.IsNullOrEmpty(this.oRequest.headers["Authorization"]))
				{
					result = false;
					return result;
				}
				if (!this.oResponse.headers.Exists("WWW-Authenticate") || this.oResponse.headers["WWW-Authenticate"].Length < 6)
				{
					result = false;
					return result;
				}
			}
			result = true;
			return result;
		}
		private bool _MayReuseMyClientPipe()
		{
			return CONFIG.bReuseClientSockets && this._bAllowClientPipeReuse && !this.oResponse.headers.ExistsAndEquals("Connection", "close") && !this.oRequest.headers.ExistsAndEquals("Connection", "close") && !this.oResponse.headers.ExistsAndEquals("Proxy-Connection", "close") && !this.oRequest.headers.ExistsAndEquals("Proxy-Connection", "close") && (this.oResponse.headers.HTTPVersion == "HTTP/1.1" || this.oResponse.headers.ExistsAndContains("Connection", "Keep-Alive"));
		}
		internal bool ReturnResponse(bool bForceClientServerPipeAffinity)
		{
			this.state = SessionStates.SendingResponse;
			bool flag = false;
			this.Timers.ClientBeginResponse = (this.Timers.ClientDoneResponse = System.DateTime.Now);
			bool result;
			try
			{
				if (this.oRequest.pipeClient != null && this.oRequest.pipeClient.Connected)
				{
					if (this.oFlags.ContainsKey("response-trickle-delay"))
					{
						int transmitDelay = int.Parse(this.oFlags["response-trickle-delay"]);
						this.oRequest.pipeClient.TransmitDelay = transmitDelay;
					}
					this.oRequest.pipeClient.Send(this.oResponse.headers.ToByteArray(true, true));
					this.oRequest.pipeClient.Send(this.responseBodyBytes);
					this.Timers.ClientDoneResponse = System.DateTime.Now;
					if (this.responseCode == 101 && this.oRequest != null && this.oResponse != null && this.oRequest.headers != null && this.oRequest.headers.ExistsAndContains("Upgrade", "WebSocket") && this.oResponse.headers != null && this.oResponse.headers.ExistsAndContains("Upgrade", "WebSocket"))
					{
						WebSocket.CreateTunnel(this);
						this.state = SessionStates.Done;
						this.FinishUISession(false);
						result = true;
						return result;
					}
					if (this.responseCode == 200 && this.HTTPMethodIs("CONNECT") && this.oRequest.pipeClient != null)
					{
						bForceClientServerPipeAffinity = true;
						Socket rawSocket = this.oRequest.pipeClient.GetRawSocket();
						if (rawSocket != null)
						{
							byte[] array = new byte[1024];
							int num = rawSocket.Receive(array, SocketFlags.Peek);
							if (num == 0)
							{
								this.oFlags["x-CONNECT-Peek"] = "After the client received notice of the established CONNECT, it failed to send any data.";
								this.requestBodyBytes = System.Text.Encoding.UTF8.GetBytes("After the client received notice of the established CONNECT, it failed to send any data.\n");
								if (this.isFlagSet(SessionFlags.SentToGateway))
								{
									this.PoisonServerPipe();
								}
								this.PoisonClientPipe();
								this.oRequest.pipeClient.End();
								flag = true;
								goto IL_480;
							}
							if (CONFIG.bDebugSpew)
							{
							}
							bool flag2 = array[0] == 22 || array[0] == 128;
							if (flag2)
							{
								try
								{
									HTTPSClientHello hTTPSClientHello = new HTTPSClientHello();
									if (hTTPSClientHello.LoadFromStream(new System.IO.MemoryStream(array, 0, num, false)))
									{
										this.requestBodyBytes = System.Text.Encoding.UTF8.GetBytes(hTTPSClientHello.ToString() + "\n");
										this["https-Client-SessionID"] = hTTPSClientHello.SessionID;
										if (!string.IsNullOrEmpty(hTTPSClientHello.ServerNameIndicator))
										{
											this["https-Client-SNIHostname"] = hTTPSClientHello.ServerNameIndicator;
										}
									}
								}
								catch (System.Exception)
								{
								}
								CONNECTTunnel.CreateTunnel(this);
								flag = true;
								goto IL_480;
							}
							if (num <= 4 || array[0] != 71 || array[1] != 69 || array[2] != 84 || array[3] != 32)
							{
								this.oFlags["x-CONNECT-Peek"] = System.BitConverter.ToString(array, 0, System.Math.Min(num, 16));
								this.oFlags["x-no-decrypt"] = "PeekYieldedUnknownProtocol";
								CONNECTTunnel.CreateTunnel(this);
								flag = true;
								goto IL_480;
							}
							this.SetBitFlag(SessionFlags.IsWebSocketTunnel, true);
						}
					}
					bool flag3 = bForceClientServerPipeAffinity || this._MayReuseMyClientPipe();
					if (flag3)
					{
						OpenWebApplication.DebugSpew("Creating next session with pipes from {0}.", new object[]
						{
							this.id
						});
						this._createNextSession(bForceClientServerPipeAffinity);
						flag = true;
					}
					else
					{
						if (CONFIG.bDebugSpew)
						{
							OpenWebApplication.DebugSpew(string.Format("OpenWeb.network.clientpipereuse> Closing client socket since bReuseClientSocket was false after returning [{0}]", this.url));
						}
						this.oRequest.pipeClient.End();
						flag = true;
					}
				}
				else
				{
					flag = true;
				}
			}
			catch (System.Exception ex)
			{
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew(string.Format("Write to client failed for Session #{0}; exception was {1}", this.id, ex.ToString()));
				}
				this.state = SessionStates.Aborted;
			}
			IL_480:
			this.oRequest.pipeClient = null;
			if (flag)
			{
				this.state = SessionStates.Done;
				try
				{
					this.FinishUISession(false);
				}
				catch (System.Exception)
				{
				}
			}
			OpenWebApplication.DoAfterSessionComplete(this);
			if (this.oFlags.ContainsKey("log-drop-response-body"))
			{
				this.oFlags["x-ResponseBodyFinalLength"] = ((this.responseBodyBytes != null) ? this.responseBodyBytes.LongLength.ToString() : "0");
				this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
				this.responseBodyBytes = Utilities.emptyByteArray;
			}
			result = flag;
			return result;
		}
		private void _createNextSession(bool bForceClientServerPipeAffinity)
		{
			if (this.oResponse != null && this.oResponse.pipeServer != null && (bForceClientServerPipeAffinity || this.oResponse.pipeServer.ReusePolicy == PipeReusePolicy.MarriedToClientPipe || this.oFlags.ContainsKey("X-ClientServerPipeAffinity")))
			{
				this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
				this.oResponse.pipeServer = null;
			}
			else
			{
				this.nextSession = new Session(this.oRequest.pipeClient, null);
			}
		}
		internal void FinishUISession()
		{
			this.FinishUISession(false);
		}
		internal void FinishUISession(bool bSynchronous)
		{
		}
	}
}
