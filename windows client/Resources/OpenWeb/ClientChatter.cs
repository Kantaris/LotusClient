using System;
using System.Globalization;
using System.IO;
using System.Text;
namespace OpenWeb
{
	public class ClientChatter
	{
		internal static int _cbClientReadBuffer = 8192;
		public ClientPipe pipeClient;
		private HTTPRequestHeaders m_headers;
		private Session m_session;
		private string m_sHostFromURI;
		private PipeReadBuffer m_requestData;
		private int iEntityBodyOffset;
		private int iBodySeekProgress;
		internal long _PeekUploadProgress
		{
			get
			{
				long result;
				if (this.m_requestData == null)
				{
					result = -1L;
				}
				else
				{
					long length = this.m_requestData.Length;
					if (length > (long)this.iEntityBodyOffset)
					{
						result = length - (long)this.iEntityBodyOffset;
					}
					else
					{
						result = length;
					}
				}
				return result;
			}
		}
		public HTTPRequestHeaders headers
		{
			get
			{
				return this.m_headers;
			}
			set
			{
				this.m_headers = value;
			}
		}
		public bool bClientSocketReused
		{
			get
			{
				return this.m_session.isFlagSet(SessionFlags.ClientPipeReused);
			}
		}
		public string host
		{
			get
			{
				string result;
				if (this.m_headers != null)
				{
					result = this.m_headers["Host"];
				}
				else
				{
					result = string.Empty;
				}
				return result;
			}
			internal set
			{
				if (value == null)
				{
					value = string.Empty;
				}
				if (this.m_headers != null)
				{
					if (value.EndsWith(":80") && "HTTP".OICEquals(this.m_headers.UriScheme))
					{
						value = value.Substring(0, value.Length - 3);
					}
					this.m_headers["Host"] = value;
					if ("CONNECT".OICEquals(this.m_headers.HTTPMethod))
					{
						this.m_headers.RequestPath = value;
					}
				}
			}
		}
		public string this[string sHeader]
		{
			get
			{
				string result;
				if (this.m_headers != null)
				{
					result = this.m_headers[sHeader];
				}
				else
				{
					result = string.Empty;
				}
				return result;
			}
			set
			{
				if (this.m_headers != null)
				{
					this.m_headers[sHeader] = value;
					return;
				}
				throw new InvalidDataException("Request Headers object does not exist");
			}
		}
		internal ClientChatter(Session oSession)
		{
			this.m_session = oSession;
		}
		internal ClientChatter(Session oSession, string sData)
		{
			this.m_session = oSession;
			this.headers = Parser.ParseRequest(sData);
			if (this.headers != null && "CONNECT" == this.m_headers.HTTPMethod)
			{
				this.m_session.isTunnel = true;
			}
		}
		internal bool ReadRequestBodyFromFile(string sFilename)
		{
			bool result;
			if (System.IO.File.Exists(sFilename))
			{
				this.m_session.requestBodyBytes = System.IO.File.ReadAllBytes(sFilename);
				this.m_headers["Content-Length"] = this.m_session.requestBodyBytes.Length.ToString();
				result = true;
			}
			else
			{
				this.m_session.requestBodyBytes = System.Text.Encoding.UTF8.GetBytes("File not found: " + sFilename);
				this.m_headers["Content-Length"] = this.m_session.requestBodyBytes.Length.ToString();
				result = false;
			}
			return result;
		}
		private long _calculateExpectedEntityTransferSize()
		{
			long num = 0L;
			long result;
			if (this.m_headers.ExistsAndEquals("Transfer-encoding", "chunked"))
			{
				if ((long)this.iEntityBodyOffset >= this.m_requestData.Length)
				{
					throw new InvalidDataException("Bad request: Chunked Body was missing entirely.");
				}
				long num2;
				long num3;
				if (!Utilities.IsChunkedBodyComplete(this.m_session, this.m_requestData, (long)this.iEntityBodyOffset, out num2, out num3))
				{
					throw new InvalidDataException("Bad request: Chunked Body was incomplete.");
				}
				if (num3 < (long)this.iEntityBodyOffset)
				{
					throw new InvalidDataException("Bad request: Chunked Body was malformed. Entity ends before it starts!");
				}
				result = num3 - (long)this.iEntityBodyOffset;
			}
			else
			{
				if (long.TryParse(this.m_headers["Content-Length"], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out num) && num > -1L)
				{
					result = num;
				}
				else
				{
					result = num;
				}
			}
			return result;
		}
		private void _freeRequestData()
		{
			if (this.m_requestData != null)
			{
				this.m_requestData.Dispose();
				this.m_requestData = null;
			}
		}
		internal byte[] TakeEntity()
		{
			if (this.iEntityBodyOffset < 0)
			{
				throw new InvalidDataException("Request Entity Body Offset must not be negative");
			}
			long num = this.m_requestData.Length - (long)this.iEntityBodyOffset;
			long num2 = this._calculateExpectedEntityTransferSize();
			byte[] array;
			byte[] result;
			if (num != num2)
			{
				if (num > num2)
				{
					try
					{
						array = new byte[num - num2];
						this.m_requestData.Position = (long)this.iEntityBodyOffset + num2;
						this.m_requestData.Read(array, 0, array.Length);
					}
					catch (System.OutOfMemoryException eX)
					{
						OpenWebApplication.ReportException(eX, "HTTP Request Pipeline Too Large");
						array = System.Text.Encoding.ASCII.GetBytes("OpenWeb: Out of memory");
						this.m_session.PoisonClientPipe();
						result = Utilities.emptyByteArray;
						return result;
					}
					this.pipeClient.putBackSomeBytes(array);
					num = num2;
				}
				else
				{
					if (!this.m_session.isFlagSet(SessionFlags.RequestStreamed))
					{
						OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, true, string.Format("Content-Length mismatch: Request Header indicated {0:N0} bytes, but client sent {1:N0} bytes.", num2, num));
					}
				}
			}
			try
			{
				array = new byte[num];
				this.m_requestData.Position = (long)this.iEntityBodyOffset;
				this.m_requestData.Read(array, 0, array.Length);
			}
			catch (System.OutOfMemoryException eX2)
			{
				OpenWebApplication.ReportException(eX2, "HTTP Request Too Large");
				array = System.Text.Encoding.ASCII.GetBytes("OpenWeb: Out of memory");
				this.m_session.PoisonClientPipe();
			}
			this._freeRequestData();
			result = array;
			return result;
		}
		public void FailSession(int iError, string sErrorStatusText, string sErrorBody)
		{
			this.BuildAndReturnResponse(iError, sErrorStatusText, sErrorBody, null);
		}
		internal void BuildAndReturnResponse(int iStatus, string sStatusText, string sBodyText, System.Action<Session> delLastChance)
		{
			this.m_session.SetBitFlag(SessionFlags.ResponseGeneratedByOpenWeb, true);
			if (iStatus >= 400 && sBodyText.Length < 512)
			{
				sBodyText = sBodyText.PadRight(512, ' ');
			}
			this.m_session.responseBodyBytes = System.Text.Encoding.UTF8.GetBytes(sBodyText);
			this.m_session.oResponse.headers = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			this.m_session.oResponse.headers.SetStatus(iStatus, sStatusText);
			this.m_session.oResponse.headers.Add("Date", System.DateTime.Now.ToUniversalTime().ToString("r"));
			this.m_session.oResponse.headers.Add("Content-Type", "text/html; charset=UTF-8");
			this.m_session.oResponse.headers.Add("Connection", "close");
			this.m_session.oResponse.headers.Add("Cache-Control", "no-cache, must-revalidate");
			this.m_session.oResponse.headers.Add("Timestamp", System.DateTime.Now.ToString("HH:mm:ss.fff"));
			this.m_session.state = SessionStates.Aborted;
			if (delLastChance != null)
			{
				delLastChance(this.m_session);
			}
			OpenWebApplication.DoBeforeReturningError(this.m_session);
			this.m_session.ReturnResponse(false);
		}
		private bool ParseRequestForHeaders()
		{
			bool result;
			if (this.m_requestData == null || this.iEntityBodyOffset < 4)
			{
				result = false;
			}
			else
			{
				this.m_headers = new HTTPRequestHeaders(CONFIG.oHeaderEncoding);
				byte[] buffer = this.m_requestData.GetBuffer();
				int num;
				int num2;
				int num3;
				string text;
				Parser.CrackRequestLine(buffer, out num, out num2, out num3, out text);
				if (num < 1 || num2 < 1)
				{
					OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, "Incorrectly formed Request-Line");
					result = false;
				}
				else
				{
					if (!string.IsNullOrEmpty(text))
					{
						OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, text);
					}
					string @string = System.Text.Encoding.ASCII.GetString(buffer, 0, num - 1);
					this.m_headers.HTTPMethod = @string.ToUpperInvariant();
					if (@string != this.m_headers.HTTPMethod)
					{
						OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("Per RFC2616, HTTP Methods are case-sensitive. Client sent '{0}', expected '{1}'.", @string, this.m_headers.HTTPMethod));
					}
					this.m_headers.HTTPVersion = System.Text.Encoding.ASCII.GetString(buffer, num + num2 + 1, num3 - num2 - num - 2).Trim().ToUpperInvariant();
					int num4 = 0;
					if (buffer[num] != 47)
					{
						if (num2 > 7 && buffer[num + 4] == 58 && buffer[num + 5] == 47 && buffer[num + 6] == 47)
						{
							this.m_headers.UriScheme = System.Text.Encoding.ASCII.GetString(buffer, num, 4);
							num4 = num + 6;
							num += 7;
							num2 -= 7;
						}
						else
						{
							if (num2 > 8 && buffer[num + 5] == 58 && buffer[num + 6] == 47 && buffer[num + 7] == 47)
							{
								this.m_headers.UriScheme = System.Text.Encoding.ASCII.GetString(buffer, num, 5);
								num4 = num + 7;
								num += 8;
								num2 -= 8;
							}
							else
							{
								if (num2 > 6 && buffer[num + 3] == 58 && buffer[num + 4] == 47 && buffer[num + 5] == 47)
								{
									this.m_headers.UriScheme = System.Text.Encoding.ASCII.GetString(buffer, num, 3);
									num4 = num + 5;
									num += 6;
									num2 -= 6;
								}
							}
						}
					}
					if (num4 == 0)
					{
						if (this.pipeClient != null && this.pipeClient.bIsSecured)
						{
							this.m_headers.UriScheme = "https";
						}
						else
						{
							this.m_headers.UriScheme = "http";
						}
					}
					if (num4 > 0)
					{
						if (num2 == 0)
						{
							OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, "Incorrectly formed Request-Line. Request-URI component was missing.\r\n\r\n" + System.Text.Encoding.ASCII.GetString(buffer, 0, num3));
							result = false;
							return result;
						}
						while (num2 > 0 && buffer[num] != 47 && buffer[num] != 63)
						{
							num++;
							num2--;
						}
						int num5 = num4 + 1;
						int num6 = num - num5;
						if (num6 > 0)
						{
							this.m_sHostFromURI = CONFIG.oHeaderEncoding.GetString(buffer, num5, num6);
							if (this.m_headers.UriScheme == "ftp" && this.m_sHostFromURI.Contains("@"))
							{
								int num7 = this.m_sHostFromURI.LastIndexOf("@") + 1;
								this.m_headers.UriUserInfo = this.m_sHostFromURI.Substring(0, num7);
								this.m_sHostFromURI = this.m_sHostFromURI.Substring(num7);
							}
						}
					}
					byte[] array = new byte[num2];
					System.Buffer.BlockCopy(buffer, num, array, 0, num2);
					this.m_headers.RawPath = array;
					if (string.IsNullOrEmpty(this.m_headers.RequestPath))
					{
						OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, "Incorrectly formed Request-Line. abs_path was empty (e.g. missing /). RFC2616 Section 5.1.2");
					}
					string text2 = CONFIG.oHeaderEncoding.GetString(buffer, num3, this.iEntityBodyOffset - num3).Trim();
					if (text2.Length >= 1)
					{
						string[] sHeaderLines = text2.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						string empty = string.Empty;
						if (!Parser.ParseNVPHeaders(this.m_headers, sHeaderLines, 0, ref empty))
						{
							OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, "Incorrectly formed request headers.\n" + empty);
						}
					}
					if (this.m_headers.Exists("Content-Length") && this.m_headers.ExistsAndContains("Transfer-Encoding", "chunked"))
					{
						OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, "Content-Length request header MUST NOT be present when Transfer-Encoding is used (RFC2616 Section 4.4)");
					}
					result = true;
				}
			}
			return result;
		}
		private bool isRequestComplete()
		{
			bool result;
			if (this.m_headers == null)
			{
				if (!this.HeadersAvailable())
				{
					result = false;
					return result;
				}
				if (!this.ParseRequestForHeaders())
				{
					string str;
					if (this.m_requestData != null)
					{
						str = Utilities.ByteArrayToHexView(this.m_requestData.GetBuffer(), 24, (int)System.Math.Min(this.m_requestData.Length, 2048L));
					}
					else
					{
						str = "{OpenWeb:no data}";
					}
					if (this.m_headers == null)
					{
						this.m_headers = new HTTPRequestHeaders();
						this.m_headers.HTTPMethod = "BAD";
						this.m_headers["Host"] = "BAD-REQUEST";
						this.m_headers.RequestPath = "/BAD_REQUEST";
					}
					this.FailSession(400, "OpenWeb - Bad Request", "[OpenWeb] Request Header parsing failed. Request was:\n" + str);
					result = true;
					return result;
				}
				this.m_session.Timers.OpenWebGotRequestHeaders = System.DateTime.Now;
				this.m_session._AssignID();
				OpenWebApplication.DoRequestHeadersAvailable(this.m_session);
			}
			if (this.m_headers.ExistsAndEquals("Transfer-encoding", "chunked"))
			{
				long num;
				long num2;
				result = Utilities.IsChunkedBodyComplete(this.m_session, this.m_requestData, (long)this.iEntityBodyOffset, out num, out num2);
			}
			else
			{
				if (Utilities.isRPCOverHTTPSMethod(this.m_headers.HTTPMethod) && !this.m_headers.ExistsAndEquals("Content-Length", "0"))
				{
					this.m_session.SetBitFlag(SessionFlags.RequestStreamed, true);
					result = true;
				}
				else
				{
					if (this.m_headers.Exists("Content-Length"))
					{
						long num3 = 0L;
						try
						{
							bool flag;
							if (!long.TryParse(this.m_headers["Content-Length"], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out num3) || num3 < 0L)
							{
								OpenWebApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, true, "Request content length was invalid.\nContent-Length: " + this.m_headers["Content-Length"]);
								this.FailSession(400, "OpenWeb - Bad Request", "[OpenWeb] Request Content-Length header parsing failed.\nContent-Length: " + this.m_headers["Content-Length"]);
								flag = true;
								result = flag;
								return result;
							}
							flag = (this.m_requestData.Length >= (long)this.iEntityBodyOffset + num3);
							result = flag;
							return result;
						}
						catch
						{
							this.FailSession(400, "OpenWeb - Bad Request", "[OpenWeb] Unknown error: Check content length header?");
							bool flag = false;
							result = flag;
							return result;
						}
					}
					result = true;
				}
			}
			return result;
		}
		private bool HeadersAvailable()
		{
			bool result;
			if (this.m_requestData.Length < 16L)
			{
				result = false;
			}
			else
			{
				byte[] buffer = this.m_requestData.GetBuffer();
				long length = this.m_requestData.Length;
				HTTPHeaderParseWarnings hTTPHeaderParseWarnings;
				bool flag = Parser.FindEndOfHeaders(buffer, ref this.iBodySeekProgress, length, out hTTPHeaderParseWarnings);
				if (flag)
				{
					this.iEntityBodyOffset = this.iBodySeekProgress + 1;
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}
		internal bool ReadRequest()
		{
			bool result;
			if (this.m_requestData != null)
			{
				OpenWebApplication.ReportException(new System.InvalidOperationException("ReadRequest called when requestData buffer already existed."));
				result = false;
			}
			else
			{
				if (this.pipeClient == null)
				{
					OpenWebApplication.ReportException(new System.InvalidOperationException("ReadRequest called after pipeClient was null'd."));
					result = false;
				}
				else
				{
					this.m_requestData = new PipeReadBuffer(true);
					this.m_session.SetBitFlag(SessionFlags.ClientPipeReused, this.pipeClient.iUseCount > 0u);
					this.pipeClient.IncrementUse(0);
					this.pipeClient.setReceiveTimeout();
					int num = 0;
					bool flag = false;
					bool flag2 = false;
					byte[] array = new byte[ClientChatter._cbClientReadBuffer];
					while (true)
					{
						try
						{
							num = this.pipeClient.Receive(array);
						}
						catch (System.Exception ex)
						{
							if (CONFIG.bDebugSpew)
							{
								OpenWebApplication.DebugSpew(string.Format("ReadRequest {0} threw {1}", (this.pipeClient == null) ? "Null pipeClient" : this.pipeClient.ToString(), ex.Message));
							}
							flag = true;
						}
						if (num <= 0)
						{
							flag2 = true;
							OpenWebApplication.DoReadRequestBuffer(this.m_session, array, 0);
							if (CONFIG.bDebugSpew)
							{
								OpenWebApplication.DebugSpew(string.Format("ReadRequest {0} returned {1}", (this.pipeClient == null) ? "Null pipeClient" : this.pipeClient.ToString(), num));
							}
						}
						else
						{
							if (CONFIG.bDebugSpew)
							{
								OpenWebApplication.DebugSpew(string.Format("READ FROM {0}:\n{1}", this.pipeClient, Utilities.ByteArrayToHexView(array, 32, num)));
							}
							if (!OpenWebApplication.DoReadRequestBuffer(this.m_session, array, num))
							{
								flag = true;
							}
							if (0L == this.m_requestData.Length)
							{
								this.m_session.Timers.ClientBeginRequest = System.DateTime.Now;
								if (1u == this.pipeClient.iUseCount && num > 2 && (array[0] == 5 || array[0] == 5))
								{
									break;
								}
								int num2 = 0;
								while (num2 < num && (13 == array[num2] || 10 == array[num2]))
								{
									num2++;
								}
								this.m_requestData.Write(array, num2, num - num2);
							}
							else
							{
								this.m_requestData.Write(array, 0, num);
							}
						}
						if (flag2 || flag || this.isRequestComplete())
						{
							goto Block_19;
						}
					}
					result = false;
					return result;
					Block_19:
					array = null;
					if (flag || this.m_requestData.Length == 0L)
					{
						this._freeRequestData();
						if (this.pipeClient == null)
						{
							result = false;
						}
						else
						{
							if (this.pipeClient.iUseCount < 2u || (this.pipeClient.bIsSecured && this.pipeClient.iUseCount < 3u))
							{
							}
							result = false;
						}
					}
					else
					{
						if (this.m_headers == null || this.m_session.state >= SessionStates.Done)
						{
							this._freeRequestData();
							result = false;
						}
						else
						{
							if ("CONNECT" == this.m_headers.HTTPMethod)
							{
								this.m_session.isTunnel = true;
								this.m_sHostFromURI = this.m_session.PathAndQuery;
							}
							if (this.m_sHostFromURI != null)
							{
								if (this.m_headers.Exists("Host"))
								{
									if (!Utilities.areOriginsEquivalent(this.m_sHostFromURI, this.m_headers["Host"], this.m_session.isHTTPS ? 443 : (this.m_session.isFTP ? 21 : 80)) && (!this.m_session.isTunnel || !Utilities.areOriginsEquivalent(this.m_sHostFromURI, this.m_headers["Host"], 443)))
									{
										this.m_session.oFlags["X-Original-Host"] = this.m_headers["Host"];
										this.m_session.oFlags["X-URI-Host"] = this.m_sHostFromURI;
										if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.SetHostHeaderFromURL", true))
										{
											this.m_headers["Host"] = this.m_sHostFromURI;
										}
									}
								}
								else
								{
									if ("HTTP/1.1".OICEquals(this.m_headers.HTTPVersion))
									{
										this.m_session.oFlags["X-Original-Host"] = string.Empty;
									}
									this.m_headers["Host"] = this.m_sHostFromURI;
								}
								this.m_sHostFromURI = null;
							}
							if (!this.m_headers.Exists("Host"))
							{
								this._freeRequestData();
								result = false;
							}
							else
							{
								result = true;
							}
						}
					}
				}
			}
			return result;
		}
	}
}
