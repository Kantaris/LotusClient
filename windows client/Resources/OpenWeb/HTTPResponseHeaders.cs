using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
namespace OpenWeb
{
	public class HTTPResponseHeaders : HTTPHeaders, System.ICloneable, System.Collections.Generic.IEnumerable<HTTPHeaderItem>, System.Collections.IEnumerable
	{
		[CodeDescription("Status code from HTTP Response. Call SetStatus() instead of manipulating directly.")]
		public int HTTPResponseCode;
		[CodeDescription("Status text from HTTP Response (e.g. '200 OK'). Call SetStatus() instead of manipulating directly.")]
		public string HTTPResponseStatus = string.Empty;
		public string StatusDescription
		{
			get
			{
				return Utilities.TrimBefore(this.HTTPResponseStatus, ' ');
			}
			set
			{
				this.HTTPResponseStatus = string.Format("{0} {1}", this.HTTPResponseCode, value);
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
			HTTPResponseHeaders hTTPResponseHeaders = (HTTPResponseHeaders)base.MemberwiseClone();
			hTTPResponseHeaders.storage = new System.Collections.Generic.List<HTTPHeaderItem>(this.storage.Count);
			foreach (HTTPHeaderItem current in this.storage)
			{
				hTTPResponseHeaders.storage.Add(new HTTPHeaderItem(current.Name, current.Value));
			}
			return hTTPResponseHeaders;
		}
		public void SetStatus(int iCode, string sDescription)
		{
			this.HTTPResponseCode = iCode;
			this.HTTPResponseStatus = string.Format("{0} {1}", iCode, sDescription);
		}
		public HTTPResponseHeaders()
		{
		}
		public HTTPResponseHeaders(int iStatus, string[] sHeaders) : this(iStatus, "Generated", sHeaders)
		{
		}
		public HTTPResponseHeaders(int iStatusCode, string sStatusText, string[] sHeaders)
		{
			this.SetStatus(iStatusCode, sStatusText);
			if (sHeaders != null)
			{
				string empty = string.Empty;
				Parser.ParseNVPHeaders(this, sHeaders, 0, ref empty);
			}
		}
		public HTTPResponseHeaders(System.Text.Encoding encodingForHeaders)
		{
			this._HeaderEncoding = encodingForHeaders;
		}
		[CodeDescription("Returns a byte[] representing the HTTP headers.")]
		public byte[] ToByteArray(bool prependStatusLine, bool appendEmptyLine)
		{
			return this._HeaderEncoding.GetBytes(this.ToString(prependStatusLine, appendEmptyLine));
		}
		[CodeDescription("Returns a string representing the HTTP headers.")]
		public string ToString(bool prependStatusLine, bool appendEmptyLine)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(512);
			if (prependStatusLine)
			{
				stringBuilder.AppendFormat("{0} {1}\r\n", this.HTTPVersion, this.HTTPResponseStatus);
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
		[CodeDescription("Returns a string containing the HTTP Response headers.")]
		public override string ToString()
		{
			return this.ToString(true, false);
		}
		[CodeDescription("Replaces the current Response header set using a string representing the new HTTP headers.")]
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
			HTTPResponseHeaders hTTPResponseHeaders = null;
			try
			{
				hTTPResponseHeaders = Parser.ParseResponse(sHeaders);
			}
			catch (System.Exception)
			{
			}
			bool result;
			if (hTTPResponseHeaders == null)
			{
				result = false;
			}
			else
			{
				this.SetStatus(hTTPResponseHeaders.HTTPResponseCode, hTTPResponseHeaders.StatusDescription);
				this.HTTPVersion = hTTPResponseHeaders.HTTPVersion;
				this.storage = hTTPResponseHeaders.storage;
				result = true;
			}
			return result;
		}
	}
}
