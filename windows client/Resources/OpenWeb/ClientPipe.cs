using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
namespace OpenWeb
{
	public class ClientPipe : BasePipe
	{
		internal static int _timeoutReceiveInitial = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.clientpipe.receive.initial", 60000);
		internal static int _timeoutReceiveReused = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.clientpipe.receive.reuse", 30000);
		private static bool _bWantClientCert = OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.https.requestclientcertificate", false);
		private string _sProcessName;
		private int _iProcessID;
		private byte[] _arrReceivedAndPutBack;
		public int LocalProcessID
		{
			get
			{
				return this._iProcessID;
			}
		}
		public string LocalProcessName
		{
			get
			{
				return this._sProcessName ?? string.Empty;
			}
		}
		internal System.DateTime dtAccepted
		{
			get;
			set;
		}
		public override bool HasDataAvailable()
		{
			return this._arrReceivedAndPutBack != null || base.HasDataAvailable();
		}
		internal void putBackSomeBytes(byte[] toPutback)
		{
			this._arrReceivedAndPutBack = new byte[toPutback.Length];
			System.Buffer.BlockCopy(toPutback, 0, this._arrReceivedAndPutBack, 0, toPutback.Length);
		}
		internal new int Receive(byte[] arrBuffer)
		{
			int result;
			if (this._arrReceivedAndPutBack == null)
			{
				result = base.Receive(arrBuffer);
			}
			else
			{
				int num = this._arrReceivedAndPutBack.Length;
				System.Buffer.BlockCopy(this._arrReceivedAndPutBack, 0, arrBuffer, 0, num);
				this._arrReceivedAndPutBack = null;
				result = num;
			}
			return result;
		}
		internal ClientPipe(Socket oSocket, System.DateTime dtCreationTime) : base(oSocket, "C")
		{
			try
			{
				this.dtAccepted = dtCreationTime;
				oSocket.NoDelay = true;
				if (CONFIG.bMapSocketToProcess)
				{
					this._iProcessID = Winsock.MapLocalPortToProcessId(base.Port);
					if (this._iProcessID > 0)
					{
						this._sProcessName = ProcessHelper.GetProcessName(this._iProcessID);
					}
				}
			}
			catch
			{
			}
		}
		internal void setReceiveTimeout()
		{
			try
			{
				this._baseSocket.ReceiveTimeout = ((this.iUseCount < 2u) ? ClientPipe._timeoutReceiveInitial : ClientPipe._timeoutReceiveReused);
			}
			catch
			{
			}
		}
		public override string ToString()
		{
			return string.Format("[ClientPipe: {0}:{1}; UseCnt: {2}[{3}]; Port: {4}; {5} established {6}]", new object[]
			{
				this._sProcessName,
				this._iProcessID,
				this.iUseCount,
				string.Empty,
				base.Port,
				base.bIsSecured ? "SECURE" : "PLAINTTEXT",
				this.dtAccepted
			});
		}
		internal bool SecureClientPipeDirect(X509Certificate2 certServer)
		{
			bool result;
			try
			{
				OpenWebApplication.DebugSpew(string.Format("SecureClientPipeDirect({0})", certServer.Subject));
				this._httpsStream = new SslStream(new NetworkStream(this._baseSocket, false), false);
				this._httpsStream.AuthenticateAsServer(certServer, ClientPipe._bWantClientCert, CONFIG.oAcceptedClientHTTPSProtocols, false);
				result = true;
				return result;
			}
			catch (AuthenticationException var_0_4C)
			{
				base.End();
			}
			catch (System.Exception var_1_58)
			{
				base.End();
			}
			result = false;
			return result;
		}
		internal bool SecureClientPipe(string sHostname, HTTPResponseHeaders oHeaders)
		{
			try
			{
			}
			catch (System.Exception var_1_05)
			{
			}
			bool result;
			try
			{
				if (CONFIG.bDebugSpew)
				{
					OpenWebApplication.DebugSpew("SecureClientPipe for: " + this.ToString() + " sending data to client:\n" + Utilities.ByteArrayToHexView(oHeaders.ToByteArray(true, true), 32));
				}
				base.Send(oHeaders.ToByteArray(true, true));
				bool flag;
				if (oHeaders.HTTPResponseCode != 200)
				{
					OpenWebApplication.DebugSpew("SecureClientPipe returning FALSE because HTTPResponseCode != 200");
					flag = false;
					result = flag;
					return result;
				}
				this._httpsStream = new SslStream(new NetworkStream(this._baseSocket, false), false);
				flag = true;
				result = flag;
				return result;
			}
			catch (System.Exception var_3_9C)
			{
				try
				{
					base.End();
				}
				catch (System.Exception)
				{
				}
			}
			result = false;
			return result;
		}
	}
}
