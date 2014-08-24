using System;
using System.Runtime.InteropServices;
namespace OpenWeb
{
	public class SessionTimers
	{
		public System.DateTime ClientConnected;
		public System.DateTime ClientBeginRequest;
		public System.DateTime OpenWebGotRequestHeaders;
		public System.DateTime ClientDoneRequest;
		public System.DateTime ServerConnected;
		public System.DateTime OpenWebBeginRequest;
		public System.DateTime ServerGotRequest;
		public System.DateTime ServerBeginResponse;
		public System.DateTime OpenWebGotResponseHeaders;
		public System.DateTime ServerDoneResponse;
		public System.DateTime ClientBeginResponse;
		public System.DateTime ClientDoneResponse;
		public int GatewayDeterminationTime;
		public int DNSTime;
		public int TCPConnectTime;
		public int HTTPSHandshakeTime;
		private static bool _EnabledHighResTimers;
		public static bool EnableHighResolutionTimers
		{
			get
			{
				return SessionTimers._EnabledHighResTimers;
			}
			set
			{
				if (value != SessionTimers._EnabledHighResTimers)
				{
					if (value)
					{
						uint num = SessionTimers.MM_timeBeginPeriod(1u);
						SessionTimers._EnabledHighResTimers = (num == 0u);
					}
					else
					{
						uint num2 = SessionTimers.MM_timeEndPeriod(1u);
						SessionTimers._EnabledHighResTimers = (num2 != 0u);
					}
				}
			}
		}
		public override string ToString()
		{
			return this.ToString(false);
		}
		public string ToString(bool bMultiLine)
		{
			string result;
			if (bMultiLine)
			{
				result = string.Format("ClientConnected:\t{0:HH:mm:ss.fff}\r\nClientBeginRequest:\t{1:HH:mm:ss.fff}\r\nGotRequestHeaders:\t{2:HH:mm:ss.fff}\r\nClientDoneRequest:\t{3:HH:mm:ss.fff}\r\nDetermine Gateway:\t{4,0}ms\r\nDNS Lookup: \t\t{5,0}ms\r\nTCP/IP Connect:\t{6,0}ms\r\nHTTPS Handshake:\t{7,0}ms\r\nServerConnected:\t{8:HH:mm:ss.fff}\r\nOpenWebBeginRequest:\t{9:HH:mm:ss.fff}\r\nServerGotRequest:\t{10:HH:mm:ss.fff}\r\nServerBeginResponse:\t{11:HH:mm:ss.fff}\r\nGotResponseHeaders:\t{12:HH:mm:ss.fff}\r\nServerDoneResponse:\t{13:HH:mm:ss.fff}\r\nClientBeginResponse:\t{14:HH:mm:ss.fff}\r\nClientDoneResponse:\t{15:HH:mm:ss.fff}\r\n\r\n{16}", new object[]
				{
					this.ClientConnected,
					this.ClientBeginRequest,
					this.OpenWebGotRequestHeaders,
					this.ClientDoneRequest,
					this.GatewayDeterminationTime,
					this.DNSTime,
					this.TCPConnectTime,
					this.HTTPSHandshakeTime,
					this.ServerConnected,
					this.OpenWebBeginRequest,
					this.ServerGotRequest,
					this.ServerBeginResponse,
					this.OpenWebGotResponseHeaders,
					this.ServerDoneResponse,
					this.ClientBeginResponse,
					this.ClientDoneResponse,
					(System.TimeSpan.Zero < this.ClientDoneResponse - this.ClientBeginRequest) ? string.Format("\tOverall Elapsed:\t{0:h\\:mm\\:ss\\.fff}\r\n", this.ClientDoneResponse - this.ClientBeginRequest) : string.Empty
				});
			}
			else
			{
				result = string.Format("ClientConnected: {0:HH:mm:ss.fff}, ClientBeginRequest: {1:HH:mm:ss.fff}, GotRequestHeaders: {2:HH:mm:ss.fff}, ClientDoneRequest: {3:HH:mm:ss.fff}, Determine Gateway: {4,0}ms, DNS Lookup: {5,0}ms, TCP/IP Connect: {6,0}ms, HTTPS Handshake: {7,0}ms, ServerConnected: {8:HH:mm:ss.fff},OpenWebBeginRequest: {9:HH:mm:ss.fff}, ServerGotRequest: {10:HH:mm:ss.fff}, ServerBeginResponse: {11:HH:mm:ss.fff}, GotResponseHeaders: {12:HH:mm:ss.fff}, ServerDoneResponse: {13:HH:mm:ss.fff}, ClientBeginResponse: {14:HH:mm:ss.fff}, ClientDoneResponse: {15:HH:mm:ss.fff}{16}", new object[]
				{
					this.ClientConnected,
					this.ClientBeginRequest,
					this.OpenWebGotRequestHeaders,
					this.ClientDoneRequest,
					this.GatewayDeterminationTime,
					this.DNSTime,
					this.TCPConnectTime,
					this.HTTPSHandshakeTime,
					this.ServerConnected,
					this.OpenWebBeginRequest,
					this.ServerGotRequest,
					this.ServerBeginResponse,
					this.OpenWebGotResponseHeaders,
					this.ServerDoneResponse,
					this.ClientBeginResponse,
					this.ClientDoneResponse,
					(System.TimeSpan.Zero < this.ClientDoneResponse - this.ClientBeginRequest) ? string.Format(", Overall Elapsed: {0:h\\:mm\\:ss\\.fff}", this.ClientDoneResponse - this.ClientBeginRequest) : string.Empty
				});
			}
			return result;
		}
		[System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint MM_timeBeginPeriod(uint iMS);
		[System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
		private static extern uint MM_timeEndPeriod(uint iMS);
	}
}
