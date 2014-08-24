using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace OpenWeb
{
	public class HTTPRequestHeaders : HTTPHeaders, System.ICloneable, System.Collections.Generic.IEnumerable<HTTPHeaderItem>, System.Collections.IEnumerable
	{
		private string _UriScheme = "http";
		[CodeDescription("HTTP Method or Verb from HTTP Request.")]
		public string HTTPMethod = string.Empty;
		private byte[] _RawPath = Utilities.emptyByteArray;
		private string _Path = string.Empty;
		private string _uriUserInfo;
		[CodeDescription("URI Scheme for this HTTP Request; usually 'http' or 'https'")]
		public string UriScheme
		{
			get
			{
				return this._UriScheme ?? string.Empty;
			}
			set
			{
				this._UriScheme = value.ToLowerInvariant();
			}
		}
		[CodeDescription("For FTP URLs, returns either null or user:pass@")]
		public string UriUserInfo
		{
			get
			{
				return this._uriUserInfo;
			}
			internal set
			{
				if (string.Empty == value)
				{
					value = null;
				}
				this._uriUserInfo = value;
			}
		}
		[CodeDescription("String representing the HTTP Request path.")]
		public string RequestPath
		{
			get
			{
				return this._Path ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					value = string.Empty;
				}
				this._Path = value;
				this._RawPath = this._HeaderEncoding.GetBytes(value);
			}
		}
		[CodeDescription("Byte array representing the HTTP Request path.")]
		public byte[] RawPath
		{
			get
			{
				return this._RawPath ?? Utilities.emptyByteArray;
			}
			set
			{
				if (value == null)
				{
					value = Utilities.emptyByteArray;
				}
				this._RawPath = Utilities.Dupe(value);
				this._Path = this._HeaderEncoding.GetString(this._RawPath);
			}
		}
		public new System.Collections.Generic.IEnumerator<HTTPHeaderItem> GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}
		public object Clone()
		{
			HTTPRequestHeaders hTTPRequestHeaders = (HTTPRequestHeaders)base.MemberwiseClone();
			hTTPRequestHeaders.storage = new System.Collections.Generic.List<HTTPHeaderItem>(this.storage.Count);
			foreach (HTTPHeaderItem current in this.storage)
			{
				hTTPRequestHeaders.storage.Add(new HTTPHeaderItem(current.Name, current.Value));
			}
			return hTTPRequestHeaders;
		}
		public HTTPRequestHeaders()
		{
		}
		public HTTPRequestHeaders(string sPath, string[] sHeaders)
		{
			this.HTTPMethod = "GET";
			this.RequestPath = sPath;
			if (sHeaders != null)
			{
				string empty = string.Empty;
				Parser.ParseNVPHeaders(this, sHeaders, 0, ref empty);
			}
		}
		public HTTPRequestHeaders(System.Text.Encoding encodingForHeaders)
		{
			this._HeaderEncoding = encodingForHeaders;
		}
		[CodeDescription("Replaces the current Request header set using a string representing the new HTTP headers.")]
		public override bool AssignFromString(string sHeaders)
		{
			if (string.IsNullOrEmpty(sHeaders))
			{
				throw new System.ArgumentException("Header string must not be null or empty");
			}
			if (!sHeaders.Contains("\r\n\r\n"))
			{
				sHeaders += "\r\n\r\n";
			}
			HTTPRequestHeaders hTTPRequestHeaders = null;
			try
			{
				hTTPRequestHeaders = Parser.ParseRequest(sHeaders);
			}
			catch (System.Exception)
			{
			}
			bool result;
			if (hTTPRequestHeaders == null)
			{
				result = false;
			}
			else
			{
				this.HTTPMethod = hTTPRequestHeaders.HTTPMethod;
				this._Path = hTTPRequestHeaders._Path;
				this._RawPath = hTTPRequestHeaders._RawPath;
				this._UriScheme = hTTPRequestHeaders._UriScheme;
				this.HTTPVersion = hTTPRequestHeaders.HTTPVersion;
				this._uriUserInfo = hTTPRequestHeaders._uriUserInfo;
				this.storage = hTTPRequestHeaders.storage;
				result = true;
			}
			return result;
		}
		[CodeDescription("Returns current Request Headers as a byte array.")]
		public byte[] ToByteArray(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolInPath)
		{
			return this.ToByteArray(prependVerbLine, appendEmptyLine, includeProtocolInPath, null);
		}
		[CodeDescription("Returns current Request Headers as a byte array.")]
		public byte[] ToByteArray(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolInPath, string sVerbLineHost)
		{
			byte[] result;
			if (!prependVerbLine)
			{
				result = this._HeaderEncoding.GetBytes(this.ToString(false, appendEmptyLine, false));
			}
			else
			{
				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(this.HTTPMethod);
				byte[] bytes2 = System.Text.Encoding.ASCII.GetBytes(this.HTTPVersion);
				byte[] bytes3 = this._HeaderEncoding.GetBytes(this.ToString(false, appendEmptyLine, false));
				System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(bytes3.Length + 1024);
				memoryStream.Write(bytes, 0, bytes.Length);
				memoryStream.WriteByte(32);
				if (includeProtocolInPath && !"CONNECT".OICEquals(this.HTTPMethod))
				{
					if (sVerbLineHost == null)
					{
						sVerbLineHost = base["Host"];
					}
					byte[] bytes4 = this._HeaderEncoding.GetBytes(this._UriScheme + "://" + this._uriUserInfo + sVerbLineHost);
					memoryStream.Write(bytes4, 0, bytes4.Length);
				}
				if ("CONNECT".OICEquals(this.HTTPMethod) && sVerbLineHost != null)
				{
					byte[] bytes5 = this._HeaderEncoding.GetBytes(sVerbLineHost);
					memoryStream.Write(bytes5, 0, bytes5.Length);
				}
				else
				{
					memoryStream.Write(this._RawPath, 0, this._RawPath.Length);
				}
				memoryStream.WriteByte(32);
				memoryStream.Write(bytes2, 0, bytes2.Length);
				memoryStream.WriteByte(13);
				memoryStream.WriteByte(10);
				memoryStream.Write(bytes3, 0, bytes3.Length);
				result = memoryStream.ToArray();
			}
			return result;
		}
		[CodeDescription("Returns current Request Headers as a string.")]
		public string ToString(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolAndHostInPath)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(512);
			if (prependVerbLine)
			{
				if (includeProtocolAndHostInPath && !"CONNECT".OICEquals(this.HTTPMethod))
				{
					stringBuilder.AppendFormat("{0} {1}://{2}{3}{4} {5}\r\n", new object[]
					{
						this.HTTPMethod,
						this._UriScheme,
						this._uriUserInfo,
						base["Host"],
						this.RequestPath,
						this.HTTPVersion
					});
				}
				else
				{
					stringBuilder.AppendFormat("{0} {1} {2}\r\n", this.HTTPMethod, this.RequestPath, this.HTTPVersion);
				}
			}
			for (int i = 0; i < this.storage.Count; i++)
			{
				stringBuilder.AppendFormat("{0}: {1}\r\n", this.storage[i].Name, this.storage[i].Value);
			}
			if (appendEmptyLine)
			{
				stringBuilder.Append("\r\n");
			}
			return stringBuilder.ToString();
		}
		[CodeDescription("Returns a string representing the HTTP Request.")]
		public string ToString(bool prependVerbLine, bool appendEmptyLine)
		{
			return this.ToString(prependVerbLine, appendEmptyLine, false);
		}
		[CodeDescription("Returns a string representing the HTTP Request.")]
		public override string ToString()
		{
			return this.ToString(true, false, false);
		}
	}
}
