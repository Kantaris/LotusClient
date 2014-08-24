using System;
using System.Globalization;
using System.IO;
namespace OpenWeb
{
	public class Parser
	{
		internal static void CrackRequestLine(byte[] arrRequest, out int ixURIOffset, out int iURILen, out int ixHeaderNVPOffset, out string sMalformedURI)
		{
			ixURIOffset = (iURILen = (ixHeaderNVPOffset = 0));
			int num = 0;
			sMalformedURI = null;
			do
			{
				if (32 == arrRequest[num])
				{
					if (ixURIOffset == 0)
					{
						ixURIOffset = num + 1;
					}
					else
					{
						if (iURILen == 0)
						{
							iURILen = num - ixURIOffset;
						}
						else
						{
							sMalformedURI = "Extra whitespace found in Request Line";
						}
					}
				}
				else
				{
					if (arrRequest[num] == 10)
					{
						ixHeaderNVPOffset = num + 1;
					}
				}
				num++;
			}
			while (ixHeaderNVPOffset == 0);
		}
		internal static bool FindEndOfHeaders(byte[] arrData, ref int iBodySeekProgress, long lngDataLen, out HTTPHeaderParseWarnings oWarnings)
		{
			oWarnings = HTTPHeaderParseWarnings.None;
			while (true)
			{
				bool flag = false;
				while ((long)iBodySeekProgress < lngDataLen - 1L)
				{
					iBodySeekProgress++;
					if (10 == arrData[iBodySeekProgress - 1])
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					break;
				}
				if (13 == arrData[iBodySeekProgress] || 10 == arrData[iBodySeekProgress])
				{
					goto Block_4;
				}
				iBodySeekProgress++;
			}
			bool result = false;
			return result;
			Block_4:
			if (10 == arrData[iBodySeekProgress])
			{
				oWarnings = HTTPHeaderParseWarnings.EndedWithLFLF;
				result = true;
			}
			else
			{
				iBodySeekProgress++;
				if ((long)iBodySeekProgress < lngDataLen && 10 == arrData[iBodySeekProgress])
				{
					if (13 != arrData[iBodySeekProgress - 3])
					{
						oWarnings = HTTPHeaderParseWarnings.EndedWithLFCRLF;
					}
					result = true;
				}
				else
				{
					if (iBodySeekProgress > 3)
					{
						iBodySeekProgress -= 4;
					}
					else
					{
						iBodySeekProgress = 0;
					}
					result = false;
				}
			}
			return result;
		}
		private static bool IsPrefixedWithWhitespace(string s)
		{
			return s.Length > 0 && char.IsWhiteSpace(s[0]);
		}
		internal static bool ParseNVPHeaders(HTTPHeaders oHeaders, string[] sHeaderLines, int iStartAt, ref string sErrors)
		{
			bool result = true;
			int i = iStartAt;
			while (i < sHeaderLines.Length)
			{
				int num = sHeaderLines[i].IndexOf(':');
				HTTPHeaderItem hTTPHeaderItem;
				if (num > 0)
				{
					hTTPHeaderItem = oHeaders.Add(sHeaderLines[i].Substring(0, num), sHeaderLines[i].Substring(num + 1).TrimStart(new char[]
					{
						' ',
						'\t'
					}));
				}
				else
				{
					if (num == 0)
					{
						hTTPHeaderItem = null;
						sErrors += string.Format("Missing Header name #{0}, {1}\n", 1 + i - iStartAt, sHeaderLines[i]);
						result = false;
					}
					else
					{
						hTTPHeaderItem = oHeaders.Add(sHeaderLines[i], string.Empty);
						sErrors += string.Format("Missing colon in header #{0}, {1}\n", 1 + i - iStartAt, sHeaderLines[i]);
						result = false;
					}
				}
				i++;
				bool flag = hTTPHeaderItem != null && i < sHeaderLines.Length && Parser.IsPrefixedWithWhitespace(sHeaderLines[i]);
				while (flag)
				{
					hTTPHeaderItem.Value = hTTPHeaderItem.Value + " " + sHeaderLines[i].TrimStart(new char[]
					{
						' ',
						'\t'
					});
					i++;
					flag = (i < sHeaderLines.Length && Parser.IsPrefixedWithWhitespace(sHeaderLines[i]));
				}
			}
			return result;
		}
		public static bool FindEntityBodyOffsetFromArray(byte[] arrData, out int iHeadersLen, out int iEntityBodyOffset, out HTTPHeaderParseWarnings outWarnings)
		{
			bool result;
			if (arrData != null && arrData.Length >= 2)
			{
				int num = 0;
				long lngDataLen = (long)arrData.Length;
				bool flag = Parser.FindEndOfHeaders(arrData, ref num, lngDataLen, out outWarnings);
				if (flag)
				{
					iEntityBodyOffset = num + 1;
					switch ((int)outWarnings)
					{
					case 0:
						iHeadersLen = num - 3;
						result = true;
						return result;
					case 1:
						iHeadersLen = num - 1;
						result = true;
						return result;
					case 2:
						iHeadersLen = num - 2;
						result = true;
						return result;
					}
				}
			}
			iHeadersLen = (iEntityBodyOffset = -1);
			outWarnings = HTTPHeaderParseWarnings.Malformed;
			result = false;
			return result;
		}
		private static int _GetEntityLengthFromHeaders(HTTPHeaders oHeaders, System.IO.MemoryStream strmData)
		{
			int result;
			if (oHeaders.ExistsAndEquals("Transfer-encoding", "chunked"))
			{
				long num;
				long num2;
				if (Utilities.IsChunkedBodyComplete(null, strmData, strmData.Position, out num, out num2))
				{
					result = (int)(num2 - strmData.Position);
				}
				else
				{
					result = (int)(strmData.Length - strmData.Position);
				}
			}
			else
			{
				string text = oHeaders["Content-Length"];
				if (!string.IsNullOrEmpty(text))
				{
					long num3 = 0L;
					if (!long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out num3) || num3 < 0L)
					{
						result = (int)(strmData.Length - strmData.Position);
					}
					else
					{
						result = (int)num3;
					}
				}
				else
				{
					if (oHeaders.ExistsAndContains("Connection", "close"))
					{
						result = (int)(strmData.Length - strmData.Position);
					}
					else
					{
						result = 0;
					}
				}
			}
			return result;
		}
		public static bool TakeRequest(System.IO.MemoryStream strmClient, out HTTPRequestHeaders headersRequest, out byte[] arrRequestBody)
		{
			headersRequest = null;
			arrRequestBody = Utilities.emptyByteArray;
			bool result;
			if (strmClient.Length - strmClient.Position < 16L)
			{
				result = false;
			}
			else
			{
				byte[] buffer = strmClient.GetBuffer();
				long length = strmClient.Length;
				int num = (int)strmClient.Position;
				HTTPHeaderParseWarnings hTTPHeaderParseWarnings;
				if (!Parser.FindEndOfHeaders(buffer, ref num, length, out hTTPHeaderParseWarnings))
				{
					result = false;
				}
				else
				{
					byte[] array = new byte[(long)(1 + num) - strmClient.Position];
					strmClient.Read(array, 0, array.Length);
					string @string = CONFIG.oHeaderEncoding.GetString(array);
					headersRequest = Parser.ParseRequest(@string);
					if (headersRequest != null)
					{
						int num2 = Parser._GetEntityLengthFromHeaders(headersRequest, strmClient);
						arrRequestBody = new byte[num2];
						strmClient.Read(arrRequestBody, 0, arrRequestBody.Length);
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
		public static bool TakeResponse(System.IO.MemoryStream strmServer, string sRequestMethod, out HTTPResponseHeaders headersResponse, out byte[] arrResponseBody)
		{
			headersResponse = null;
			arrResponseBody = Utilities.emptyByteArray;
			bool result;
			if (strmServer.Length - strmServer.Position < 16L)
			{
				result = false;
			}
			else
			{
				byte[] buffer = strmServer.GetBuffer();
				long length = strmServer.Length;
				int num = (int)strmServer.Position;
				HTTPHeaderParseWarnings hTTPHeaderParseWarnings;
				if (!Parser.FindEndOfHeaders(buffer, ref num, length, out hTTPHeaderParseWarnings))
				{
					result = false;
				}
				else
				{
					byte[] array = new byte[(long)(1 + num) - strmServer.Position];
					strmServer.Read(array, 0, array.Length);
					string @string = CONFIG.oHeaderEncoding.GetString(array);
					headersResponse = Parser.ParseResponse(@string);
					if (headersResponse == null)
					{
						result = false;
					}
					else
					{
						if (sRequestMethod == "HEAD")
						{
							result = true;
						}
						else
						{
							int num2 = Parser._GetEntityLengthFromHeaders(headersResponse, strmServer);
							if (sRequestMethod == "CONNECT")
							{
								int hTTPResponseCode = headersResponse.HTTPResponseCode;
							}
							arrResponseBody = new byte[num2];
							strmServer.Read(arrResponseBody, 0, arrResponseBody.Length);
							result = true;
						}
					}
				}
			}
			return result;
		}
		public static HTTPRequestHeaders ParseRequest(string sRequest)
		{
			string[] array = Parser._GetHeaderLines(sRequest);
			HTTPRequestHeaders result;
			if (array == null)
			{
				result = null;
			}
			else
			{
				HTTPRequestHeaders hTTPRequestHeaders = new HTTPRequestHeaders(CONFIG.oHeaderEncoding);
				int num = array[0].IndexOf(' ');
				if (num > 0)
				{
					hTTPRequestHeaders.HTTPMethod = array[0].Substring(0, num).ToUpperInvariant();
					array[0] = array[0].Substring(num).Trim();
				}
				num = array[0].LastIndexOf(' ');
				if (num > 0)
				{
					string text = array[0].Substring(0, num);
					hTTPRequestHeaders.HTTPVersion = array[0].Substring(num).Trim().ToUpperInvariant();
					if (text.OICStartsWith("http://"))
					{
						hTTPRequestHeaders.UriScheme = "http";
						num = text.IndexOfAny(new char[]
						{
							'/',
							'?'
						}, 7);
						if (num == -1)
						{
							hTTPRequestHeaders.RequestPath = "/";
						}
						else
						{
							hTTPRequestHeaders.RequestPath = text.Substring(num);
						}
					}
					else
					{
						if (text.OICStartsWith("https://"))
						{
							hTTPRequestHeaders.UriScheme = "https";
							num = text.IndexOfAny(new char[]
							{
								'/',
								'?'
							}, 8);
							if (num == -1)
							{
								hTTPRequestHeaders.RequestPath = "/";
							}
							else
							{
								hTTPRequestHeaders.RequestPath = text.Substring(num);
							}
						}
						else
						{
							if (text.OICStartsWith("ftp://"))
							{
								hTTPRequestHeaders.UriScheme = "ftp";
								num = text.IndexOf('/', 6);
								if (num == -1)
								{
									hTTPRequestHeaders.RequestPath = "/";
								}
								else
								{
									string text2 = text.Substring(6, num - 6);
									if (text2.Contains("@"))
									{
										hTTPRequestHeaders.UriUserInfo = Utilities.TrimTo(text2, text2.IndexOf("@") + 1);
									}
									hTTPRequestHeaders.RequestPath = text.Substring(num);
								}
							}
							else
							{
								hTTPRequestHeaders.RequestPath = text;
							}
						}
					}
					string empty = string.Empty;
					Parser.ParseNVPHeaders(hTTPRequestHeaders, array, 1, ref empty);
					result = hTTPRequestHeaders;
				}
				else
				{
					result = null;
				}
			}
			return result;
		}
		private static string[] _GetHeaderLines(string sInput)
		{
			int num = sInput.IndexOf("\r\n\r\n", System.StringComparison.Ordinal);
			if (num < 1)
			{
				num = sInput.Length;
			}
			string[] result;
			if (num < 1)
			{
				result = null;
			}
			else
			{
				string[] array = sInput.Substring(0, num).Replace("\r\n", "\n").Split(new char[]
				{
					'\n'
				});
				if (array == null || array.Length < 1)
				{
					result = null;
				}
				else
				{
					result = array;
				}
			}
			return result;
		}
		public static HTTPResponseHeaders ParseResponse(string sResponse)
		{
			string[] array = Parser._GetHeaderLines(sResponse);
			HTTPResponseHeaders result;
			if (array == null)
			{
				result = null;
			}
			else
			{
				HTTPResponseHeaders hTTPResponseHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
				int num = array[0].IndexOf(' ');
				if (num <= 0)
				{
					result = null;
				}
				else
				{
					hTTPResponseHeaders.HTTPVersion = array[0].Substring(0, num).ToUpperInvariant();
					array[0] = array[0].Substring(num + 1).Trim();
					if (!hTTPResponseHeaders.HTTPVersion.OICStartsWith("HTTP/"))
					{
						result = null;
					}
					else
					{
						hTTPResponseHeaders.HTTPResponseStatus = array[0];
						num = array[0].IndexOf(' ');
						bool flag;
						if (num > 0)
						{
							flag = int.TryParse(array[0].Substring(0, num).Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out hTTPResponseHeaders.HTTPResponseCode);
						}
						else
						{
							flag = int.TryParse(array[0].Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out hTTPResponseHeaders.HTTPResponseCode);
						}
						if (!flag)
						{
							result = null;
						}
						else
						{
							string empty = string.Empty;
							Parser.ParseNVPHeaders(hTTPResponseHeaders, array, 1, ref empty);
							result = hTTPResponseHeaders;
						}
					}
				}
			}
			return result;
		}
	}
}
