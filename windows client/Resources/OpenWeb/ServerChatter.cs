using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
namespace OpenWeb
{
	public class ServerChatter
	{
		internal static int _cbServerReadBuffer = 32768;
		public ServerPipe pipeServer;
		private Session m_session;
		private HTTPResponseHeaders m_inHeaders;
		internal bool _bWasForwarded;
		private PipeReadBuffer m_responseData;
		internal long m_responseTotalDataCount;
		private int iEntityBodyOffset;
		private int _iBodySeekProgress;
		private bool _bLeakedHeaders;
		private long _lngLeakedOffset;
		private long _lngLastChunkInfoOffset = -1L;
		public string MIMEType
		{
			get
			{
				string result;
				if (this.headers == null)
				{
					result = string.Empty;
				}
				else
				{
					string text = this.headers["Content-Type"];
					if (text.Length > 0)
					{
						text = Utilities.TrimAfter(text, ';').Trim();
					}
					result = text;
				}
				return result;
			}
		}
		internal long _PeekDownloadProgress
		{
			get
			{
				long result;
				if (this.m_responseData != null)
				{
					result = this.m_responseTotalDataCount;
				}
				else
				{
					result = -1L;
				}
				return result;
			}
		}
		public int iTTFB
		{
			get
			{
				int num = (int)(this.m_session.Timers.ServerBeginResponse - this.m_session.Timers.OpenWebBeginRequest).TotalMilliseconds;
				int result;
				if (num <= 0)
				{
					result = 0;
				}
				else
				{
					result = num;
				}
				return result;
			}
		}
		public int iTTLB
		{
			get
			{
				int num = (int)(this.m_session.Timers.ServerDoneResponse - this.m_session.Timers.OpenWebBeginRequest).TotalMilliseconds;
				int result;
				if (num <= 0)
				{
					result = 0;
				}
				else
				{
					result = num;
				}
				return result;
			}
		}
		public bool bWasForwarded
		{
			get
			{
				return this._bWasForwarded;
			}
		}
		public bool bServerSocketReused
		{
			get
			{
				return this.m_session.isFlagSet(SessionFlags.ServerPipeReused);
			}
		}
		public HTTPResponseHeaders headers
		{
			get
			{
				return this.m_inHeaders;
			}
			set
			{
				if (value != null)
				{
					this.m_inHeaders = value;
				}
			}
		}
		public string this[string sHeader]
		{
			get
			{
				string result;
				if (this.m_inHeaders != null)
				{
					result = this.m_inHeaders[sHeader];
				}
				else
				{
					result = string.Empty;
				}
				return result;
			}
			set
			{
				if (this.m_inHeaders != null)
				{
					this.m_inHeaders[sHeader] = value;
					return;
				}
				throw new InvalidDataException("Response Headers object does not exist");
			}
		}
		internal byte[] _PeekAtBody()
		{
			byte[] result;
			if (this.iEntityBodyOffset < 1 || this.m_responseData == null || this.m_responseData.Length < 1L)
			{
				result = Utilities.emptyByteArray;
			}
			else
			{
				int num = (int)this.m_responseData.Length - this.iEntityBodyOffset;
				if (num < 1)
				{
					result = Utilities.emptyByteArray;
				}
				else
				{
					byte[] array = new byte[num];
					System.Buffer.BlockCopy(this.m_responseData.GetBuffer(), this.iEntityBodyOffset, array, 0, num);
					result = array;
				}
			}
			return result;
		}
		internal ServerChatter(Session oSession)
		{
			this.m_session = oSession;
			this.m_responseData = new PipeReadBuffer(false);
		}
		internal ServerChatter(Session oSession, string sHeaders)
		{
			this.m_session = oSession;
			this.m_inHeaders = Parser.ParseResponse(sHeaders);
		}
		internal void Initialize(bool bAllocatePipeReadBuffer)
		{
			if (bAllocatePipeReadBuffer)
			{
				this.m_responseData = new PipeReadBuffer(false);
			}
			else
			{
				this.m_responseData = null;
			}
			this._lngLeakedOffset = (long)(this._iBodySeekProgress = (this.iEntityBodyOffset = 0));
			this._lngLastChunkInfoOffset = -1L;
			this.m_inHeaders = null;
			this._bLeakedHeaders = false;
			this.pipeServer = null;
			this._bWasForwarded = false;
			this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, false);
		}
		internal byte[] TakeEntity()
		{
			byte[] array;
			try
			{
				array = new byte[this.m_responseData.Length - (long)this.iEntityBodyOffset];
				this.m_responseData.Position = (long)this.iEntityBodyOffset;
				this.m_responseData.Read(array, 0, array.Length);
			}
			catch (System.OutOfMemoryException eX)
			{
				OpenWebApplication.ReportException(eX, "HTTP Response Too Large");
				array = System.Text.Encoding.ASCII.GetBytes("OpenWeb: Out of memory");
				this.m_session.PoisonServerPipe();
			}
			this.FreeResponseDataBuffer();
			return array;
		}
		internal void FreeResponseDataBuffer()
		{
			this.m_responseData.Dispose();
			this.m_responseData = null;
		}
		private bool HeadersAvailable()
		{
			bool result;
			if (this.iEntityBodyOffset > 0)
			{
				result = true;
			}
			else
			{
				if (this.m_responseData == null)
				{
					result = false;
				}
				else
				{
					byte[] buffer = this.m_responseData.GetBuffer();
					HTTPHeaderParseWarnings hTTPHeaderParseWarnings;
					bool flag = Parser.FindEndOfHeaders(buffer, ref this._iBodySeekProgress, this.m_responseData.Length, out hTTPHeaderParseWarnings);
					if (flag)
					{
						this.iEntityBodyOffset = this._iBodySeekProgress + 1;
						if (hTTPHeaderParseWarnings == HTTPHeaderParseWarnings.EndedWithLFLF)
						{
							OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The Server did not return properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFLF.");
						}
						if (hTTPHeaderParseWarnings == HTTPHeaderParseWarnings.EndedWithLFCRLF)
						{
							OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The Server did not return properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFCRLF.");
						}
						result = true;
					}
					else
					{
						result = false;
					}
				}
			}
			return result;
		}
		private bool ParseResponseForHeaders()
		{
			bool result;
			if (this.m_responseData == null || this.iEntityBodyOffset < 4)
			{
				result = false;
			}
			else
			{
				this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
				byte[] buffer = this.m_responseData.GetBuffer();
				string text = CONFIG.oHeaderEncoding.GetString(buffer, 0, this.iEntityBodyOffset).Trim();
				if (text == null || text.Length < 1)
				{
					this.m_inHeaders = null;
					result = false;
				}
				else
				{
					string[] array = text.Replace("\r\n", "\n").Split(new char[]
					{
						'\n'
					});
					if (array.Length < 1)
					{
						result = false;
					}
					else
					{
						int num = array[0].IndexOf(' ');
						if (num <= 0)
						{
							OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Cannot parse HTTP response; Status line contains no spaces. Data:\n\n\t" + array[0]);
							result = false;
						}
						else
						{
							this.m_inHeaders.HTTPVersion = array[0].Substring(0, num).ToUpperInvariant();
							array[0] = array[0].Substring(num + 1).Trim();
							if (!this.m_inHeaders.HTTPVersion.OICStartsWith("HTTP/"))
							{
								if (!this.m_inHeaders.HTTPVersion.OICStartsWith("ICY"))
								{
									OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Response does not start with HTTP. Data:\n\n\t" + array[0]);
									result = false;
									return result;
								}
								this.m_session.bBufferResponse = false;
								this.m_session.oFlags["log-drop-response-body"] = "ICY";
							}
							this.m_inHeaders.HTTPResponseStatus = array[0];
							num = array[0].IndexOf(' ');
							bool flag;
							if (num > 0)
							{
								flag = int.TryParse(array[0].Substring(0, num).Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
							}
							else
							{
								string text2 = array[0].Trim();
								flag = int.TryParse(text2, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
								if (!flag)
								{
									int i = 0;
									while (i < text2.Length)
									{
										if (!char.IsDigit(text2[i]))
										{
											flag = int.TryParse(text2.Substring(0, i), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
											if (flag)
											{
												OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, false, "The response's status line was missing a space between ResponseCode and ResponseStatus. Data:\n\n\t" + text2);
												break;
											}
											break;
										}
										else
										{
											i++;
										}
									}
								}
							}
							if (!flag)
							{
								OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The response's status line did not contain a ResponseCode. Data:\n\n\t" + array[0]);
								result = false;
							}
							else
							{
								string empty = string.Empty;
								if (!Parser.ParseNVPHeaders(this.m_inHeaders, array, 1, ref empty))
								{
									OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Incorrectly formed response headers.\n" + empty);
								}
								if (this.m_inHeaders.Exists("Content-Length") && this.m_inHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
								{
									OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, "Content-Length response header MUST NOT be present when Transfer-Encoding is used (RFC2616 Section 4.4)");
								}
								result = true;
							}
						}
					}
				}
			}
			return result;
		}
		private bool GetHeaders()
		{
			bool result;
			if (!this.HeadersAvailable())
			{
				result = false;
			}
			else
			{
				if (!this.ParseResponseForHeaders())
				{
					this.m_session.SetBitFlag(SessionFlags.ProtocolViolationInResponse, true);
					this._PoisonPipe();
					string arg;
					if (this.m_responseData != null)
					{
						arg = "<plaintext>\n" + Utilities.ByteArrayToHexView(this.m_responseData.GetBuffer(), 24, (int)System.Math.Min(this.m_responseData.Length, 2048L));
					}
					else
					{
						arg = "{OpenWeb:no data}";
					}
					this.m_session.oRequest.FailSession(500, "OpenWeb - Bad Response", string.Format("[OpenWeb] Response Header parsing failed.\n{0}Response Data:\n{1}", this.m_session.isFlagSet(SessionFlags.ServerPipeReused) ? "This can be caused by an illegal HTTP response earlier on this reused server socket-- for instance, a HTTP/304 response which illegally contains a body.\n" : string.Empty, arg));
					result = true;
				}
				else
				{
					if (this.m_inHeaders.HTTPResponseCode > 99 && this.m_inHeaders.HTTPResponseCode < 200)
					{
						if (this.m_inHeaders.Exists("Content-Length") && "0" != this.m_inHeaders["Content-Length"].Trim())
						{
							OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "HTTP/1xx responses MUST NOT contain a body, but a non-zero content-length was returned.");
						}
						if (this.m_inHeaders.HTTPResponseCode != 101 || !this.m_inHeaders.ExistsAndContains("Upgrade", "WebSocket"))
						{
							if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.leakhttp1xx", true) && this.m_session.oRequest.pipeClient != null)
							{
								try
								{
									this.m_session.oRequest.pipeClient.Send(this.m_inHeaders.ToByteArray(true, true));
									StringDictionary oFlags;
									(oFlags = this.m_session.oFlags)["x-OpenWeb-Stream1xx"] = oFlags["x-OpenWeb-Stream1xx"] + "Returned a HTTP/" + this.m_inHeaders.HTTPResponseCode.ToString() + " message from the server.";
									goto IL_296;
								}
								catch (System.Exception innerException)
								{
									if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.streaming.abortifclientaborts", false))
									{
										throw new System.Exception("Leaking HTTP/1xx response to client failed", innerException);
									}
									goto IL_296;
								}
							}
							StringDictionary oFlags2;
							(oFlags2 = this.m_session.oFlags)["x-OpenWeb-streaming"] = oFlags2["x-OpenWeb-streaming"] + "Eating a HTTP/" + this.m_inHeaders.HTTPResponseCode.ToString() + " message from the stream.";
							IL_296:
							this._deleteInformationalMessage();
							result = this.GetHeaders();
							return result;
						}
					}
					result = true;
				}
			}
			return result;
		}
		private bool isResponseBodyComplete()
		{
			bool result;
			if (this.m_session.HTTPMethodIs("HEAD"))
			{
				result = true;
			}
			else
			{
				if (this.m_session.HTTPMethodIs("CONNECT") && this.m_inHeaders.HTTPResponseCode == 200)
				{
					result = true;
				}
				else
				{
					if (this.m_inHeaders.HTTPResponseCode == 200 && this.m_session.isFlagSet(SessionFlags.RequestStreamed))
					{
						this.m_session.bBufferResponse = true;
						result = true;
					}
					else
					{
						if (this.m_inHeaders.HTTPResponseCode == 204 || this.m_inHeaders.HTTPResponseCode == 205 || this.m_inHeaders.HTTPResponseCode == 304 || (this.m_inHeaders.HTTPResponseCode > 99 && this.m_inHeaders.HTTPResponseCode < 200))
						{
							if (this.m_inHeaders.Exists("Content-Length") && "0" != this.m_inHeaders["Content-Length"].Trim())
							{
								OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "This type of HTTP response MUST NOT contain a body, but a non-zero content-length was returned.");
								result = true;
							}
							else
							{
								result = true;
							}
						}
						else
						{
							if (this.m_inHeaders.ExistsAndEquals("Transfer-Encoding", "chunked"))
							{
								if (this._lngLastChunkInfoOffset < (long)this.iEntityBodyOffset)
								{
									this._lngLastChunkInfoOffset = (long)this.iEntityBodyOffset;
								}
								long num;
								result = Utilities.IsChunkedBodyComplete(this.m_session, this.m_responseData, this._lngLastChunkInfoOffset, out this._lngLastChunkInfoOffset, out num);
							}
							else
							{
								if (this.m_inHeaders.Exists("Content-Length"))
								{
									long num2;
									if (!long.TryParse(this.m_inHeaders["Content-Length"], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out num2) || num2 < 0L)
									{
										OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "Content-Length response header is not a valid unsigned integer.\nContent-Length: " + this.m_inHeaders["Content-Length"]);
										result = true;
									}
									else
									{
										result = (this.m_responseTotalDataCount >= (long)this.iEntityBodyOffset + num2);
									}
								}
								else
								{
									if (this.m_inHeaders.ExistsAndEquals("Connection", "close") || this.m_inHeaders.ExistsAndEquals("Proxy-Connection", "close") || (this.m_inHeaders.HTTPVersion != "HTTP/1.1" && !this.m_inHeaders.ExistsAndContains("Connection", "Keep-Alive")))
									{
										result = false;
									}
									else
									{
										OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "No Connection: close, no Content-Length. No way to tell if the response is complete.");
										result = false;
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
		private void _deleteInformationalMessage()
		{
			this.m_inHeaders = null;
			int num = (int)this.m_responseData.Length - this.iEntityBodyOffset;
			PipeReadBuffer pipeReadBuffer = new PipeReadBuffer(num);
			pipeReadBuffer.Write(this.m_responseData.GetBuffer(), this.iEntityBodyOffset, num);
			this.m_responseData = pipeReadBuffer;
			this.m_responseTotalDataCount = this.m_responseData.Length;
			this.iEntityBodyOffset = (this._iBodySeekProgress = 0);
		}
		internal void releaseServerPipe()
		{
			if (this.pipeServer != null)
			{
				if (this.headers.ExistsAndEquals("Connection", "close") || this.headers.ExistsAndEquals("Proxy-Connection", "close") || (this.headers.HTTPVersion != "HTTP/1.1" && !this.headers.ExistsAndContains("Connection", "Keep-Alive")) || !this.pipeServer.Connected)
				{
					this.pipeServer.ReusePolicy = PipeReusePolicy.NoReuse;
				}
				this._detachServerPipe();
			}
		}
		internal void _detachServerPipe()
		{
			if (this.pipeServer != null)
			{
				if (this.pipeServer.ReusePolicy != PipeReusePolicy.NoReuse && this.pipeServer.ReusePolicy != PipeReusePolicy.MarriedToClientPipe && this.pipeServer.isClientCertAttached && !this.pipeServer.isAuthenticated)
				{
					this.pipeServer.MarkAsAuthenticated(this.m_session.LocalProcessID);
				}
				Proxy.htServerPipePool.PoolOrClosePipe(this.pipeServer);
				this.pipeServer = null;
			}
		}
		private static bool SIDsMatch(int iPID, string sIDSession, string sIDPipe)
		{
			return string.Equals(sIDSession, sIDPipe, System.StringComparison.Ordinal) || (iPID != 0 && string.Equals(string.Format("PID{0}*{1}", iPID, sIDSession), sIDPipe, System.StringComparison.Ordinal));
		}
		private bool ConnectToHost()
		{
			string text = this.m_session.oFlags["x-overrideHostName"];
			if (text != null)
			{
				this.m_session.oFlags["x-overrideHost"] = string.Format("{0}:{1}", text, this.m_session.port);
			}
			text = this.m_session.oFlags["x-overrideHost"];
			if (text == null)
			{
				if (this.m_session.HTTPMethodIs("CONNECT"))
				{
					text = this.m_session.PathAndQuery;
				}
				else
				{
					text = this.m_session.host;
				}
			}
			bool flag = false;
			IPEndPoint[] array = null;
			bool result;
			if (this.m_session.oFlags["x-overrideGateway"] != null)
			{
				if ("DIRECT".OICEquals(this.m_session.oFlags["x-overrideGateway"]))
				{
					this.m_session.bypassGateway = true;
				}
				else
				{
					string text2 = this.m_session.oFlags["x-overrideGateway"];
					if (text2.OICStartsWith("socks="))
					{
						flag = true;
						text2 = text2.Substring(6);
					}
					array = Utilities.IPEndPointListFromHostPortString(text2);
					if (flag && array == null)
					{
						this.m_session.oRequest.FailSession(502, "OpenWeb - SOCKS Proxy DNS Lookup Failed", string.Format("[OpenWeb] SOCKS DNS Lookup for \"{0}\" failed. {1}", Utilities.HtmlEncode(text2), NetworkInterface.GetIsNetworkAvailable() ? string.Empty : "The system reports that no network connection is available. \n"));
						result = false;
						return result;
					}
				}
			}
			else
			{
				if (!this.m_session.bypassGateway)
				{
					int tickCount = System.Environment.TickCount;
					string text3 = this.m_session.oRequest.headers.UriScheme;
					if (text3 == "http" && this.m_session.HTTPMethodIs("CONNECT"))
					{
						text3 = "https";
					}
					IPEndPoint iPEndPoint = OpenWebApplication.oProxy.FindGatewayForOrigin(text3, text);
					if (iPEndPoint != null)
					{
						array = new IPEndPoint[]
						{
							iPEndPoint
						};
					}
					this.m_session.Timers.GatewayDeterminationTime = System.Environment.TickCount - tickCount;
				}
			}
			if (array != null)
			{
				this._bWasForwarded = true;
			}
			else
			{
				if (this.m_session.isFTP)
				{
					result = true;
					return result;
				}
			}
			int num = this.m_session.isHTTPS ? 443 : (this.m_session.isFTP ? 21 : 80);
			string text4;
			Utilities.CrackHostAndPort(text, out text4, ref num);
			string text5;
			if (array != null)
			{
				if (this.m_session.isHTTPS || flag)
				{
					text5 = string.Format("{0}:{1}->{2}/{3}:{4}", new object[]
					{
						flag ? "SOCKS" : "GW",
						array[0],
						this.m_session.isHTTPS ? "https" : "http",
						text4,
						num
					});
				}
				else
				{
					text5 = string.Format("GW:{0}->*", array[0]);
				}
			}
			else
			{
				text5 = string.Format("DIRECT->{0}{1}:{2}", this.m_session.isHTTPS ? "https/" : "http/", text4, num);
			}
			if (this.pipeServer != null && !this.m_session.oFlags.ContainsKey("X-ServerPipe-Marriage-Trumps-All") && !ServerChatter.SIDsMatch(this.m_session.LocalProcessID, text5, this.pipeServer.sPoolKey))
			{
				this.m_session.oFlags["X-Divorced-ServerPipe"] = string.Format("Had: '{0}' but needs: '{1}'", this.pipeServer.sPoolKey, text5);
				this._detachServerPipe();
			}
			if (this.pipeServer == null && !this.m_session.oFlags.ContainsKey("X-Bypass-ServerPipe-Reuse-Pool"))
			{
				this.pipeServer = Proxy.htServerPipePool.TakePipe(text5, this.m_session.LocalProcessID, this.m_session.id);
			}
			if (this.pipeServer != null)
			{
				this.m_session.Timers.ServerConnected = this.pipeServer.dtConnected;
				StringDictionary oFlags;
				(oFlags = this.m_session.oFlags)["x-serversocket"] = oFlags["x-serversocket"] + "REUSE " + this.pipeServer._sPipeName;
				if (this.pipeServer.Address != null && !this.pipeServer.isConnectedToGateway)
				{
					this.m_session.m_hostIP = this.pipeServer.Address.ToString();
					this.m_session.oFlags["x-hostIP"] = this.m_session.m_hostIP;
				}
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew(string.Format("Session #{0} ({1} {2}): Reusing {3}\r\n", new object[]
					{
						this.m_session.id,
						this.m_session.RequestMethod,
						this.m_session.fullUrl,
						this.pipeServer.ToString()
					}));
				}
				result = true;
			}
			else
			{
				if (this.m_session.oFlags.ContainsKey("x-serversocket"))
				{
					StringDictionary oFlags2;
					(oFlags2 = this.m_session.oFlags)["x-serversocket"] = oFlags2["x-serversocket"] + "*NEW*";
				}
				IPEndPoint[] arrDest;
				bool flag2;
				if (array == null)
				{
					if (num < 0 || num > 65535)
					{
						this.m_session.oRequest.FailSession(400, "OpenWeb - Bad Request", "[OpenWeb] HTTP Request specified an invalid port number.");
						result = false;
						return result;
					}
					try
					{
						IPAddress[] iPAddressList = DNSResolver.GetIPAddressList(text4, true, this.m_session.Timers);
						System.Collections.Generic.List<IPEndPoint> list = new System.Collections.Generic.List<IPEndPoint>();
						IPAddress[] array2 = iPAddressList;
						for (int i = 0; i < array2.Length; i++)
						{
							IPAddress address = array2[i];
							list.Add(new IPEndPoint(address, num));
						}
						arrDest = list.ToArray();
						flag2 = this.ilMethod(flag, num, text4, text5, arrDest);
						result = flag2;
						return result;
					}
					catch (System.Exception eX)
					{
						this.m_session.oRequest.FailSession(502, "OpenWeb - DNS Lookup Failed", string.Format("[OpenWeb] DNS Lookup for \"{0}\" failed. {1}{2}", Utilities.HtmlEncode(text4), NetworkInterface.GetIsNetworkAvailable() ? string.Empty : "The system reports that no network connection is available. \n", Utilities.DescribeException(eX)));
						flag2 = false;
						result = flag2;
						return result;
					}
				}
				arrDest = array;
				flag2 = this.ilMethod(flag, num, text4, text5, arrDest);
				result = flag2;
			}
			return result;
		}
		private bool ilMethod(bool flag, int num, string text4, string text5, IPEndPoint[] arrDest)
		{
			bool result;
			try
			{
				Socket socket = ServerChatter.CreateConnectedSocket(arrDest, this.m_session);
				if (flag)
				{
					socket = this._SOCKSifyConnection(text4, num, socket);
				}
				if (this.m_session.isHTTPS && this._bWasForwarded)
				{
					if (!ServerChatter.SendHTTPCONNECTToGateway(socket, text4, num, this.m_session.oRequest.headers))
					{
						throw new System.Exception("Upstream Gateway refused requested CONNECT.");
					}
					this.m_session.oFlags["x-CreatedTunnel"] = "OpenWeb-Created-A-CONNECT-Tunnel";
				}
				this.pipeServer = new ServerPipe(socket, "ServerPipe#" + this.m_session.id.ToString(), this._bWasForwarded, text5);
				if (flag)
				{
					this.pipeServer.isConnectedViaSOCKS = true;
				}
				if (this.m_session.isHTTPS && !this.pipeServer.SecureExistingConnection(this.m_session, text4, this.m_session.oFlags["https-Client-Certificate"], ref this.m_session.Timers.HTTPSHandshakeTime))
				{
					string text6 = "Failed to negotiate HTTPS connection with server.";
					if (!Utilities.IsNullOrEmpty(this.m_session.responseBodyBytes))
					{
						text6 += System.Text.Encoding.UTF8.GetString(this.m_session.responseBodyBytes);
					}
					throw new System.Security.SecurityException(text6);
				}
				result = true;
			}
			catch (System.Exception ex)
			{
				string text7 = string.Empty;
				bool flag2 = true;
				if (ex is System.Security.SecurityException)
				{
					flag2 = false;
				}
				SocketException ex2 = ex as SocketException;
				if (ex2 != null)
				{
					if (ex2.ErrorCode == 10013 || ex2.ErrorCode == 10050)
					{
						text7 = string.Format("A Firewall may be blocking OpenWeb's traffic. <br />ErrorCode: {0}.", ex2.ErrorCode);
						flag2 = false;
					}
					else
					{
						text7 = string.Format("<br />ErrorCode: {0}.", ex2.ErrorCode);
					}
				}
				string sErrorStatusText;
				string arg;
				if (this._bWasForwarded)
				{
					sErrorStatusText = "OpenWeb - Gateway Connection Failed";
					arg = "[OpenWeb] The socket connection to the upstream proxy/gateway failed.";
					if (flag2)
					{
						text7 = string.Format("Closing OpenWeb, changing your system proxy settings, and restarting OpenWeb may help. {0}", text7);
					}
				}
				else
				{
					sErrorStatusText = "OpenWeb - Connection Failed";
					arg = string.Format("[OpenWeb] The socket connection to {0} failed.", Utilities.HtmlEncode(text4));
				}
				this.m_session.oRequest.FailSession(502, sErrorStatusText, string.Format("{0} {1} <br />{2}", arg, text7, Utilities.HtmlEncode(Utilities.DescribeException(ex))));
				result = false;
			}
			return result;
		}
		private Socket _SOCKSifyConnection(string sServerHostname, int iServerPort, Socket newSocket)
		{
			this._bWasForwarded = false;
			OpenWebApplication.DebugSpew("Creating SOCKS connection for {0}:{1}.", new object[]
			{
				sServerHostname,
				iServerPort
			});
			byte[] buffer = ServerChatter._BuildSOCKS5Greeting();
			newSocket.Send(buffer);
			byte[] buffer2 = new byte[64];
			int num = newSocket.Receive(buffer2);
			byte[] buffer3 = ServerChatter._BuildSOCKS5ConnectHandshakeForTarget(sServerHostname, iServerPort);
			newSocket.Send(buffer3);
			byte[] array = new byte[64];
			int num2 = newSocket.Receive(array);
			if (num2 > 2 && array[0] == 5)
			{
				return newSocket;
			}
			try
			{
				newSocket.Close();
			}
			catch
			{
			}
			string text = string.Empty;
			if (num2 > 1 && array[0] == 0)
			{
				int num3 = (int)array[1];
				text = string.Format("Gateway returned error 0x{0:x}", num3);
				switch (num3)
				{
				case 91:
					text += "-'request rejected or failed'";
					break;
				case 92:
					text += "-'request failed because client is not running identd (or not reachable from the server)'";
					break;
				case 93:
					text += "-'request failed because client's identd could not confirm the user ID string in the request'";
					break;
				default:
					text += "-'unknown'";
					break;
				}
			}
			else
			{
				if (num2 > 0)
				{
					text = "Gateway returned a malformed response:\n" + Utilities.ByteArrayToHexView(array, 8, num2);
				}
				else
				{
					text = "Gateway returned no data.";
				}
			}
			throw new InvalidDataException(string.Format("SOCKS gateway failed: {0}", text));
		}
		private static byte[] _BuildSOCKS5ConnectHandshakeForTarget(string sTargetHost, int iPort)
		{
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(sTargetHost);
			byte[] array = new byte[7 + bytes.Length];
			array[0] = 5;
			array[1] = 1;
			array[2] = 0;
			array[3] = 3;
			array[4] = (byte)bytes.Length;
			System.Buffer.BlockCopy(bytes, 0, array, 5, bytes.Length);
			array[bytes.Length + 5] = (byte)(iPort >> 8);
			array[bytes.Length + 6] = (byte)(iPort & 255);
			return array;
		}
		private static byte[] _BuildSOCKS5Greeting()
		{
			return new byte[]
			{
				5,
				1,
				0
			};
		}
		private static bool SendHTTPCONNECTToGateway(Socket oGWSocket, string sHost, int iPort, HTTPRequestHeaders oRH)
		{
			string text = oRH["User-Agent"];
			string text2 = OpenWebApplication.Prefs.GetStringPref("OpenWeb.composer.HTTPSProxyBasicCreds", null);
			if (!string.IsNullOrEmpty(text2))
			{
				text2 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text2));
			}
			string s = string.Format("CONNECT {0}:{1} HTTP/1.1\r\n{2}{3}Connection: close\r\n\r\n", new object[]
			{
				sHost,
				iPort,
				string.IsNullOrEmpty(text) ? string.Empty : string.Format("User-Agent: {0}\r\n", text),
				string.IsNullOrEmpty(text2) ? string.Empty : string.Format("Proxy-Authorization: Basic {0}\r\n", text2)
			});
			oGWSocket.Send(System.Text.Encoding.ASCII.GetBytes(s));
			byte[] array = new byte[8192];
			int num = oGWSocket.Receive(array);
			return (num > 12 && Utilities.isHTTP200Array(array)) || (num > 12 && Utilities.isHTTP407Array(array) && false);
		}
		private static Socket CreateConnectedSocket(IPEndPoint[] arrDest, Session _oSession)
		{
			Socket socket = null;
			bool flag = false;
			Stopwatch stopwatch = Stopwatch.StartNew();
			System.Exception ex = null;
			for (int i = 0; i < arrDest.Length; i++)
			{
				IPEndPoint iPEndPoint = arrDest[i];
				try
				{
					socket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					socket.NoDelay = true;
					if (OpenWebApplication.oProxy._DefaultEgressEndPoint != null)
					{
						socket.Bind(OpenWebApplication.oProxy._DefaultEgressEndPoint);
					}
					socket.Connect(iPEndPoint);
					_oSession.m_hostIP = iPEndPoint.Address.ToString();
					_oSession.oFlags["x-hostIP"] = _oSession.m_hostIP;
					OpenWebApplication.DoAfterSocketConnect(_oSession, socket);
					flag = true;
					break;
				}
				catch (System.Exception ex2)
				{
					ex = ex2;
					if (!OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.dns.fallback", true))
					{
						break;
					}
					_oSession.oFlags["x-DNS-Failover"] = _oSession.oFlags["x-DNS-Failover"] + "+1";
				}
			}
			_oSession.Timers.ServerConnected = System.DateTime.Now;
			_oSession.Timers.TCPConnectTime = (int)stopwatch.ElapsedMilliseconds;
			if (!flag)
			{
				throw ex;
			}
			return socket;
		}
		internal bool ResendRequest()
		{
			bool b = this.pipeServer != null;
			bool result;
			if (!this.ConnectToHost())
			{
				OpenWebApplication.DebugSpew("Session #{0} ConnectToHost returned null. Bailing...", new object[]
				{
					this.m_session.id
				});
				this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, b);
				result = false;
			}
			else
			{
				try
				{
					if (this.m_session.isFTP && !this.m_session.isFlagSet(SessionFlags.SentToGateway))
					{
						bool flag = true;
						result = flag;
						return result;
					}
					this.pipeServer.IncrementUse(this.m_session.id);
					this.pipeServer.setTimeouts();
					this.m_session.Timers.ServerConnected = this.pipeServer.dtConnected;
					this._bWasForwarded = this.pipeServer.isConnectedToGateway;
					this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, this.pipeServer.iUseCount > 1u);
					this.m_session.SetBitFlag(SessionFlags.SentToGateway, this._bWasForwarded);
					if (this.pipeServer.isConnectedViaSOCKS)
					{
						this.m_session.SetBitFlag(SessionFlags.SentToSOCKSGateway, true);
					}
					if (!this._bWasForwarded && !this.m_session.isHTTPS)
					{
						this.m_session.oRequest.headers.RenameHeaderItems("Proxy-Connection", "Connection");
					}
					if (!this.pipeServer.isAuthenticated)
					{
						string text = this.m_session.oRequest.headers["Authorization"];
						if (text != null && text.OICStartsWith("N"))
						{
							this.pipeServer.MarkAsAuthenticated(this.m_session.LocalProcessID);
						}
					}
					this.m_session.Timers.OpenWebBeginRequest = System.DateTime.Now;
					if (this.m_session.oFlags.ContainsKey("request-trickle-delay"))
					{
						int transmitDelay = int.Parse(this.m_session.oFlags["request-trickle-delay"]);
						this.pipeServer.TransmitDelay = transmitDelay;
					}
					if (this._bWasForwarded || !this.m_session.HTTPMethodIs("CONNECT"))
					{
						bool includeProtocolInPath = this._bWasForwarded && !this.m_session.isHTTPS;
						byte[] oBytes = this.m_session.oRequest.headers.ToByteArray(true, true, includeProtocolInPath, this.m_session.oFlags["X-OverrideHost"]);
						this.pipeServer.Send(oBytes);
						if (this.m_session.requestBodyBytes != null && this.m_session.requestBodyBytes.Length > 0)
						{
							if (this.m_session.oFlags.ContainsKey("request-body-delay"))
							{
								int millisecondsTimeout = int.Parse(this.m_session.oFlags["request-body-delay"]);
								System.Threading.Thread.Sleep(millisecondsTimeout);
							}
							this.pipeServer.Send(this.m_session.requestBodyBytes);
						}
					}
				}
				catch (System.Exception eX)
				{
					bool flag;
					if (this._MayRetryWhenSendFailed())
					{
						this.pipeServer = null;
						StringDictionary oFlags;
						(oFlags = this.m_session.oFlags)["x-RetryOnFailedSend"] = oFlags["x-RetryOnFailedSend"] + "*";
						OpenWebApplication.DebugSpew("[{0}] ServerSocket Reuse failed during ResendRequest(). Restarting fresh.", new object[]
						{
							this.m_session.id
						});
						flag = this.ResendRequest();
						result = flag;
						return result;
					}
					OpenWebApplication.DebugSpew("ResendRequest() failed: " + Utilities.DescribeException(eX));
					this.m_session.oRequest.FailSession(504, "OpenWeb - Send Failure", "[OpenWeb] ResendRequest() failed: " + Utilities.DescribeException(eX));
					flag = false;
					result = flag;
					return result;
				}
				this.m_session.oFlags["x-EgressPort"] = this.pipeServer.LocalPort.ToString();
				if (this.m_session.oFlags.ContainsKey("log-drop-request-body"))
				{
					this.m_session.oFlags["x-RequestBodyLength"] = ((this.m_session.requestBodyBytes != null) ? this.m_session.requestBodyBytes.Length.ToString() : "0");
					this.m_session.requestBodyBytes = Utilities.emptyByteArray;
				}
				result = true;
			}
			return result;
		}
		private bool _MayRetryWhenSendFailed()
		{
			return this.bServerSocketReused && this.m_session.state != SessionStates.Aborted;
		}
		private void _ReturnFileReadError(string sRemoteError, string sTrustedError)
		{
			this.Initialize(false);
			string text;
			if (this.m_session.LocalProcessID > 0 || this.m_session.isFlagSet(SessionFlags.RequestGeneratedByOpenWeb))
			{
				text = sTrustedError;
			}
			else
			{
				text = sRemoteError;
			}
			text = text.PadRight(512, ' ');
			this.m_session.responseBodyBytes = System.Text.Encoding.UTF8.GetBytes(text);
			this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			this.m_inHeaders.SetStatus(404, "Not Found");
			this.m_inHeaders.Add("Content-Length", this.m_session.responseBodyBytes.Length.ToString());
			this.m_inHeaders.Add("Cache-Control", "max-age=0, must-revalidate");
		}
		internal bool ReadResponseFromArray(byte[] arrResponse, bool bAllowBOM, string sContentTypeHint)
		{
			this.Initialize(true);
			int num = arrResponse.Length;
			int num2 = 0;
			bool flag = false;
			if (bAllowBOM)
			{
				flag = (arrResponse.Length > 3 && arrResponse[0] == 239 && arrResponse[1] == 187 && arrResponse[2] == 191);
				if (flag)
				{
					num2 = 3;
					num -= 3;
				}
			}
			bool flag2 = arrResponse.Length > 5 + num2 && arrResponse[num2] == 72 && arrResponse[num2 + 1] == 84 && arrResponse[num2 + 2] == 84 && arrResponse[num2 + 3] == 80 && arrResponse[num2 + 4] == 47;
			if (flag && !flag2)
			{
				num += 3;
				num2 = 0;
			}
			this.m_responseData.Capacity = num;
			this.m_responseData.Write(arrResponse, num2, num);
			if (flag2 && this.HeadersAvailable() && this.ParseResponseForHeaders())
			{
				this.m_session.responseBodyBytes = this.TakeEntity();
			}
			else
			{
				this.Initialize(false);
				this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
				this.m_inHeaders.SetStatus(200, "OK with automatic headers");
				this.m_inHeaders["Date"] = System.DateTime.Now.ToUniversalTime().ToString("r");
				this.m_inHeaders["Content-Length"] = arrResponse.LongLength.ToString();
				this.m_inHeaders["Cache-Control"] = "max-age=0, must-revalidate";
				if (sContentTypeHint != null)
				{
					this.m_inHeaders["Content-Type"] = sContentTypeHint;
				}
				this.m_session.responseBodyBytes = arrResponse;
			}
			return true;
		}
		internal bool ReadResponseFromFile(string sFilename, string sOptionalContentTypeHint)
		{
			bool result;
			if (!System.IO.File.Exists(sFilename))
			{
				this._ReturnFileReadError("OpenWeb - The requested file was not found.", "OpenWeb - The file '" + sFilename + "' was not found.");
				result = false;
			}
			else
			{
				byte[] arrResponse;
				try
				{
					arrResponse = System.IO.File.ReadAllBytes(sFilename);
				}
				catch (System.Exception eX)
				{
					this._ReturnFileReadError("OpenWeb - The requested file could not be read.", "OpenWeb - The requested file could not be read. " + Utilities.DescribeException(eX));
					result = false;
					return result;
				}
				result = this.ReadResponseFromArray(arrResponse, true, sOptionalContentTypeHint);
			}
			return result;
		}
		internal bool ReadResponseFromStream(System.IO.Stream oResponse, string sContentTypeHint)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			byte[] array = new byte[32768];
			int count;
			while ((count = oResponse.Read(array, 0, array.Length)) > 0)
			{
				memoryStream.Write(array, 0, count);
			}
			byte[] array2 = new byte[memoryStream.Length];
			memoryStream.Position = 0L;
			memoryStream.Read(array2, 0, array2.Length);
			return this.ReadResponseFromArray(array2, false, sContentTypeHint);
		}
		internal bool ReadResponse()
		{
			bool result;
			if (this.pipeServer == null)
			{
				result = this.IsWorkableFTPRequest();
			}
			else
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				byte[] array = new byte[ServerChatter._cbServerReadBuffer];
				do
				{
					try
					{
						int num = this.pipeServer.Receive(array);
						if (0L == this.m_session.Timers.ServerBeginResponse.Ticks)
						{
							this.m_session.Timers.ServerBeginResponse = System.DateTime.Now;
						}
						if (num <= 0)
						{
							flag = true;
							OpenWebApplication.DoReadResponseBuffer(this.m_session, array, 0);
							if (CONFIG.bDebugSpew)
							{
								OpenWebApplication.DebugSpew(string.Format("READ FROM {0}: returned {1} signaling end-of-stream", this.pipeServer, num));
							}
						}
						else
						{
							if (CONFIG.bDebugSpew)
							{
								OpenWebApplication.DebugSpew(string.Format("READ FROM {0}:\n{1}", this.pipeServer, Utilities.ByteArrayToHexView(array, 32, num)));
							}
							if (!OpenWebApplication.DoReadResponseBuffer(this.m_session, array, num))
							{
								flag2 = true;
							}
							this.m_responseData.Write(array, 0, num);
							this.m_responseTotalDataCount += (long)num;
							if (this.m_inHeaders == null && this.GetHeaders())
							{
								this.m_session.Timers.OpenWebGotResponseHeaders = System.DateTime.Now;
								if (this.m_session.state == SessionStates.Aborted && this.m_session.isAnyFlagSet(SessionFlags.ProtocolViolationInResponse))
								{
									bool flag5 = false;
									result = flag5;
									return result;
								}
								OpenWebApplication.DoResponseHeadersAvailable(this.m_session);
								string inStr = this.m_inHeaders["Content-Type"];
								if (inStr.OICStartsWithAny(new string[]
								{
									"text/event-stream",
									"multipart/x-mixed-replace"
								}) && OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.streaming.AutoStreamByMIME", true))
								{
									this.m_session.bBufferResponse = false;
								}
								else
								{
									if (CONFIG.bStreamAudioVideo && inStr.OICStartsWithAny(new string[]
									{
										"video/",
										"audio/",
										"application/x-mms-framed"
									}))
									{
										this.m_session.bBufferResponse = false;
									}
								}
								if (!this.m_session.bBufferResponse && this.m_session.HTTPMethodIs("CONNECT"))
								{
									this.m_session.bBufferResponse = true;
								}
								if (!this.m_session.bBufferResponse && 101 == this.m_inHeaders.HTTPResponseCode)
								{
									this.m_session.bBufferResponse = true;
								}
								if (!this.m_session.bBufferResponse && this.m_session.oRequest.pipeClient == null)
								{
									this.m_session.bBufferResponse = true;
								}
								if (!this.m_session.bBufferResponse && (401 == this.m_inHeaders.HTTPResponseCode || 407 == this.m_inHeaders.HTTPResponseCode) && this.m_session.oFlags.ContainsKey("x-AutoAuth"))
								{
									this.m_session.bBufferResponse = true;
								}
								this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, !this.m_session.bBufferResponse);
								if (!this.m_session.bBufferResponse)
								{
									if (this.m_session.oFlags.ContainsKey("response-trickle-delay"))
									{
										int transmitDelay = int.Parse(this.m_session.oFlags["response-trickle-delay"]);
										this.m_session.oRequest.pipeClient.TransmitDelay = transmitDelay;
									}
									if (this.m_session.oFlags.ContainsKey("log-drop-response-body") || OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.streaming.ForgetStreamedData", false))
									{
										flag3 = true;
									}
								}
							}
							if (this.m_inHeaders != null && this.m_session.isFlagSet(SessionFlags.ResponseStreamed))
							{
								if (!flag4 && !this.LeakResponseBytes())
								{
									flag4 = true;
								}
								if (flag3)
								{
									this.m_session.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
									if (this._lngLastChunkInfoOffset > -1L)
									{
										this.ReleaseStreamedChunkedData();
									}
									else
									{
										if (this.m_inHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
										{
											this.ReleaseStreamedChunkedData();
										}
										else
										{
											this.ReleaseStreamedData();
										}
									}
								}
							}
						}
					}
					catch (SocketException ex)
					{
						flag2 = true;
						if (CONFIG.bDebugSpew)
						{
							OpenWebApplication.DebugSpew("ReadResponse() failure {0}", new object[]
							{
								Utilities.DescribeException(ex)
							});
						}
						if (ex.ErrorCode == 10060)
						{
							this.m_session["X-ServerPipeError"] = "Timed out while reading response.";
						}
					}
					catch (System.Exception ex2)
					{
						flag2 = true;
						if (CONFIG.bDebugSpew)
						{
							OpenWebApplication.DebugSpew("ReadResponse() failure {0}\n{1}", new object[]
							{
								Utilities.DescribeException(ex2),
								Utilities.ByteArrayToHexView(this.m_responseData.ToArray(), 32)
							});
						}
						if (ex2 is System.OperationCanceledException)
						{
							this.m_session.state = SessionStates.Aborted;
						}
						else
						{
							if (ex2 is System.OutOfMemoryException)
							{
								OpenWebApplication.ReportException(ex2);
								this.m_session.state = SessionStates.Aborted;
							}
						}
					}
				}
				while (!flag && !flag2 && (this.m_inHeaders == null || !this.isResponseBodyComplete()));
				this.m_session.Timers.ServerDoneResponse = System.DateTime.Now;
				if (this.m_session.isFlagSet(SessionFlags.ResponseStreamed))
				{
					this.m_session.Timers.ClientDoneResponse = this.m_session.Timers.ServerDoneResponse;
				}
				array = null;
				OpenWebApplication.DebugSpew("Finished reading server response: {0} bytes.", new object[]
				{
					this.m_responseTotalDataCount
				});
				if (0L == this.m_responseTotalDataCount && this.m_inHeaders == null)
				{
					flag2 = true;
				}
				if (flag2)
				{
					OpenWebApplication.DebugSpew("*** Abort on Read from Server for Session {0} ****", new object[]
					{
						this.m_session.id
					});
					this.m_responseData.Dispose();
					this.m_responseData = null;
					result = false;
				}
				else
				{
					if (this.m_inHeaders == null)
					{
						OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "The Server did not return properly-formatted HTTP Headers. Maybe missing altogether (e.g. HTTP/0.9), maybe only \\r\\r instead of \\r\\n\\r\\n?\n");
						this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, false);
						this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
						this.m_inHeaders.HTTPVersion = "HTTP/1.0";
						this.m_inHeaders.SetStatus(200, "This buggy server did not return headers");
						this.iEntityBodyOffset = 0;
						result = true;
					}
					else
					{
						result = true;
					}
				}
			}
			return result;
		}
		private bool IsWorkableFTPRequest()
		{
			bool result;
			if (this.m_session.isFTP && !this.m_session.isFlagSet(SessionFlags.SentToGateway))
			{
				try
				{
					FTPGateway.MakeFTPRequest(this.m_session, ref this.m_responseData, out this.m_inHeaders);
					bool flag = true;
					result = flag;
					return result;
				}
				catch (System.Exception eX)
				{
					this.m_session["X-ServerPipeError"] = Utilities.DescribeException(eX);
					bool flag = false;
					result = flag;
					return result;
				}
			}
			result = false;
			return result;
		}
		private void ReleaseStreamedChunkedData()
		{
			if ((long)this.iEntityBodyOffset > this._lngLastChunkInfoOffset)
			{
				this._lngLastChunkInfoOffset = (long)this.iEntityBodyOffset;
			}
			long num;
			Utilities.IsChunkedBodyComplete(this.m_session, this.m_responseData, this._lngLastChunkInfoOffset, out this._lngLastChunkInfoOffset, out num);
			int num2 = (int)(this.m_responseData.Length - this._lngLastChunkInfoOffset);
			PipeReadBuffer pipeReadBuffer = new PipeReadBuffer(num2);
			pipeReadBuffer.Write(this.m_responseData.GetBuffer(), (int)this._lngLastChunkInfoOffset, num2);
			this.m_responseData = pipeReadBuffer;
			this._lngLeakedOffset = (long)num2;
			this._lngLastChunkInfoOffset = 0L;
			this.iEntityBodyOffset = 0;
		}
		private void ReleaseStreamedData()
		{
			this.m_responseData = new PipeReadBuffer(false);
			this._lngLeakedOffset = 0L;
			if (this.iEntityBodyOffset > 0)
			{
				this.m_responseTotalDataCount -= (long)this.iEntityBodyOffset;
				this.iEntityBodyOffset = 0;
			}
		}
		private bool LeakResponseBytes()
		{
			bool result;
			try
			{
				if (this.m_session.oRequest.pipeClient == null)
				{
					result = false;
				}
				else
				{
					if (!this._bLeakedHeaders)
					{
						if ((401 == this.m_inHeaders.HTTPResponseCode && this.m_inHeaders["WWW-Authenticate"].OICStartsWith("N")) || (407 == this.m_inHeaders.HTTPResponseCode && this.m_inHeaders["Proxy-Authenticate"].OICStartsWith("N")))
						{
							this.m_inHeaders["Proxy-Support"] = "Session-Based-Authentication";
						}
						this.m_session.Timers.ClientBeginResponse = System.DateTime.Now;
						this._bLeakedHeaders = true;
						this.m_session.oRequest.pipeClient.Send(this.m_inHeaders.ToByteArray(true, true));
						this._lngLeakedOffset = (long)this.iEntityBodyOffset;
					}
					this.m_session.oRequest.pipeClient.Send(this.m_responseData.GetBuffer(), (int)this._lngLeakedOffset, (int)(this.m_responseData.Length - this._lngLeakedOffset));
					this._lngLeakedOffset = this.m_responseData.Length;
					result = true;
				}
			}
			catch (System.Exception innerException)
			{
				this.m_session.PoisonClientPipe();
				if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.streaming.abortifclientaborts", false))
				{
					throw new System.OperationCanceledException("Leaking response to client failed", innerException);
				}
				result = false;
			}
			return result;
		}
		internal void _PoisonPipe()
		{
			if (this.pipeServer != null)
			{
				this.pipeServer.ReusePolicy = PipeReusePolicy.NoReuse;
			}
		}
	}
}
