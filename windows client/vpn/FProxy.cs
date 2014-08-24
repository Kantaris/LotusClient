using OpenWeb;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using vpn;
using vpngui.WindowsFormsApplication1;
namespace VPN
{
	internal class FProxy
	{
		private class RetryRequest
		{
            
			private System.Threading.ManualResetEvent oWaitForIt = new System.Threading.ManualResetEvent(false);
			public Session Retry(Session oSession, int iRetry)
			{
				StringDictionary stringDictionary = new StringDictionary();
				stringDictionary["X-RetryNumber"] = iRetry.ToString();
				Session session = OpenWebApplication.oProxy.SendRequest(oSession.oRequest.headers, oSession.requestBodyBytes, stringDictionary);
				session.OnStateChanged += new System.EventHandler<StateChangeEventArgs>(this.newSession_OnStateChanged);
				this.oWaitForIt.WaitOne();
				return session;
			}
			private void newSession_OnStateChanged(object sender, StateChangeEventArgs e)
			{
				if (e.newState >= SessionStates.Done)
				{
					this.oWaitForIt.Set();
				}
			}
		}
		public class TransferItem
		{
			public long bytes = 0L;
			public System.DateTime time;
			public TransferItem(long bytes, System.DateTime time)
			{
				this.bytes = bytes;
				this.time = time;
			}
		}

        public delegate void BeginRequestEventHandler(object sender, SessionStats e);
        public delegate void BeginResponseEventHandler(object sender, SessionStats e);
        public delegate void BeginErrorEventHandler(object sender, SessionStats e);
        public event BeginRequestEventHandler BeginRequest;
        public event BeginResponseEventHandler BeginResponse;
        public event BeginErrorEventHandler BeginError;
		private int counter = 0;
		private int nbrOfRequests = 0;
        int currentRequests = 0;
		//private string add = "";
		private int nbrRequest = 0;
		private bool isClosing = false;
		private bool canConnect = true; //change back
        List<SessionStats> ssList = new List<SessionStats>();
		private System.Collections.Generic.List<string> domainList = new System.Collections.Generic.List<string>();
		private System.Collections.Generic.List<string> portList = new System.Collections.Generic.List<string>();
		public Server currentAsiaServer;
		public Server currentNAServer;
        public Server currentJapanServer;
		private System.Collections.Generic.List<Server> serverList = new System.Collections.Generic.List<Server>();
		private System.Collections.Generic.List<Server> sortedServerList = new System.Collections.Generic.List<Server>();
		private static bool bUpdateTitle = true;
		private System.Collections.Generic.List<Session> oAllSessions = new System.Collections.Generic.List<Session>();
		public event System.EventHandler ServerChangeEvent;
		public static void WriteCommandResponse(string s)
		{
			System.ConsoleColor foregroundColor = System.Console.ForegroundColor;
			System.Console.ForegroundColor = System.ConsoleColor.Yellow;
			System.Console.WriteLine(s);
			System.Console.ForegroundColor = foregroundColor;
		}
		public void checkServerConnection()
		{
			for (int i = 0; i < this.serverList.Count; i++)
			{
				BackgroundWorker backgroundWorker = new BackgroundWorker();
				backgroundWorker.DoWork += new DoWorkEventHandler(this.bw_DoWork);
				backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.bw_RunWorkerCompleted);
				backgroundWorker.RunWorkerAsync(this.serverList[i]);
			}
		}
		private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			Server server = (Server)e.Result;
			System.Console.WriteLine(server.name + " " + server.ping);
			if (server.ping > 0)
			{
				this.canConnect = true;
			}
		}
		private void bw_DoWork(object sender, DoWorkEventArgs e)
		{
			Server server = (Server)e.Argument;
			server.pingServer();
			e.Result = server;
		}
		public static void WriteErrorResponse(string s)
		{
			System.ConsoleColor foregroundColor = System.Console.ForegroundColor;
			System.Console.ForegroundColor = System.ConsoleColor.Red;
			System.Console.WriteLine(s);
			System.Console.ForegroundColor = foregroundColor;
		}
		public void DoQuit()
		{
			this.isClosing = true;
			if (this.currentAsiaServer != null)
			{
				this.currentAsiaServer.Disconnect();
			}
			if (this.currentNAServer != null)
			{
				this.currentNAServer.Disconnect();
			}
            if (this.currentJapanServer != null)
            {
                this.currentJapanServer.Disconnect();
            }
			FProxy.WriteCommandResponse("Shutting down...");
			if (OpenWebApplication.oProxy != null)
			{
				OpenWebApplication.oProxy.Detach();
				OpenWebApplication.Shutdown();
			}
		}
		private static string Ellipsize(string s, int iLen)
		{
			string result;
			if (s.Length <= iLen)
			{
				result = s;
			}
			else
			{
				result = s.Substring(0, iLen - 3) + "...";
			}
			return result;
		}
		private void dispatcherTimer_Tick(object sender, System.EventArgs e)
		{
			this.garbageCollect();
		}
		public void garbageCollect()
		{
			System.GC.Collect();
		}
		public FProxy()
		{
            //this.strtup = strtup;
            Server server = new Server();
            
            server.title = "Tokyo";
            server.name = "Tokyo Server 2";
            server.address = "153.121.58.118";
            server.port = "1179";
            server.password = "barfoo!";
            server.continent = "Asia";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\2.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Seoul";
            server.name = "Seoul Server 1";
            server.address = "175.126.195.116";
            server.port = "1170";
            server.password = "barfoo!";
            server.continent = "Asia";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\0.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Hong Kong";
            server.name = "Hong Kong Server 2";
            server.address = "103.6.86.61";
            server.port = "1171";
            server.password = "barfoo!";
            server.continent = "Asia";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\1.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Hong Kong";
            server.name = "Hong Kong Server 3";
            server.address = "119.81.135.218";
            server.port = "1172";
            server.password = "barfoo!";
            server.continent = "Asia";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\1.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Osaka";
            server.name = "Osaka Server 1";
            server.address = "49.212.130.109"; // "66.212.31.178"; //"67.215.233.2"; //"49.212.130.109";
            server.port = "1173";
            server.password = "barfoo!";
            server.continent = "Asia";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\2.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Dallas";
            server.name = "Dallas Server 1";
            server.address = "67.228.194.106";
            server.port = "1174";
            server.password = "barfoo!";
            server.continent = "North America";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\3.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Los Angeles";
            server.name = "Los Angeles Server 1";
            server.address = "66.212.31.178"; //"192.73.244.252";
            server.port = "1175";
            server.password = "barfoo!";
            server.continent = "North America";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\4.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "San Francisco";
            server.name = "San Francisco Server 1";
            server.address = "50.97.198.130";
            server.port = "1176";
            server.password = "barfoo!";
            server.continent = "North America";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\5.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "San Francisco";
            server.name = "San Francisco Server 2";
            server.address = "173.245.71.68";
            server.port = "1177";
            server.password = "barfoo!";
            server.continent = "North America";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\5.png"));
            this.serverList.Add(server);
            server = new Server();
            server.title = "Tokyo";
            server.name = "Tokyo Server 1";
            server.address = "153.120.1.187";
            server.port = "1178";
            server.password = "barfoo!";
            server.continent = "Asia";
            server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\2.png"));
            this.serverList.Add(server);
           
			this.checkServerConnection();
		}
		public Server fastestAsiaServer()
		{
			int num = 10000;
			int num2 = -1;
			for (int i = 0; i < this.serverList.Count; i++)
			{
				if (this.serverList[i].ping > 0 && this.serverList[i].ping < num)
				{
					num = this.serverList[i].ping;
					num2 = i;
				}
			}
			Server result;
			if (num2 > -1 && num2 != 2)
			{
                result = serverList[num2];
                counter++;
			}
			else
			{
				result = null;
			}
			return result;
		}
		public Server fastestNAServer()
		{
			int num = 10000;
			int num2 = -1;
			for (int i = 0; i < this.serverList.Count; i++)
			{
				if (this.serverList[i].ping > 0 && this.serverList[i].ping < num && this.serverList[i].continent.Contains("North America"))
				{
					num = this.serverList[i].ping;
					num2 = i;
				}
			}
			Server result;
			if (num2 > -1)
			{
				result = this.serverList[6];
			}
			else
			{
				result = null;
			}
			return result;
		}
		public void sortServers()
		{
			this.sortedServerList.Clear();
			for (int i = 0; i < this.serverList.Count; i++)
			{
				if (this.serverList[i].ping > 0 && this.serverList[i].ping < 1500)
				{
					if (!this.serverList[i].isConnected)
					{
						this.serverList[i].Connect();
					}
					this.sortedServerList.Add(this.serverList[i]);
				}
			}
		}
		public void connect()
		{
			if (this.canConnect)
			{
                this.currentAsiaServer =  this.fastestAsiaServer();
				if (this.currentAsiaServer != null)
				{
					if (!this.currentAsiaServer.isConnected)
					{
						this.currentAsiaServer.Connect();
					}
					this.ServerChangeEvent(this, null);
				}
				else
				{
					this.currentAsiaServer = this.serverList[0];
					if (!this.currentAsiaServer.isConnected)
					{
						this.currentAsiaServer.Connect();
					}
					this.ServerChangeEvent(this, null);
				}
				this.currentNAServer = this.fastestNAServer();
				if (this.currentNAServer != null)
				{
					if (!this.currentNAServer.isConnected)
					{
						this.currentNAServer.Connect();
					}
				}
				else
				{
					this.currentNAServer = this.serverList[6];
					if (!this.currentNAServer.isConnected)
					{
						this.currentNAServer.Connect();
					}
				}
            //    currentJapanServer = serverList[5];
            //    currentJapanServer.Connect();

				OpenWebApplication.AfterSessionComplete += delegate(Session oS)
				{
					if (FProxy.bUpdateTitle)
					{
					}
				};
				string arg = "NoSAZ";
				System.Console.WriteLine(string.Format("Starting {0} ({1})...", OpenWebApplication.GetVersionString(), arg));
				CONFIG.IgnoreServerCertErrors = false;
				OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.streaming.abortifclientaborts", true);
				OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.streaming.ForgetStreamedData", false);
				OpenWebCoreStartupFlags openWebCoreStartupFlags = OpenWebCoreStartupFlags.Default;
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.DecryptSSL;
				OpenWebApplication.Startup(8877, openWebCoreStartupFlags);
				OpenWebApplication.BeforeRequest += new SessionStateHandler(this.OpenWebApplication_BeforeRequestClient);
				OpenWebApplication.BeforeResponse += new SessionStateHandler(this.OpenWebApplication_BeforeResponseClient);
				OpenWebApplication.BeforeReturningError += new SessionStateHandler(this.OpenWebApplication_BeforeReturningError);
                OpenWebApplication.AfterSessionComplete += new SessionStateHandler(OpenWebApplication_AfterSessionComplete);
                OpenWebApplication.AfterSocketConnect += new EventHandler<ConnectionEventArgs>(OpenWebApplication_AfterSocketConnect);
			}
		}

        void OpenWebApplication_AfterSocketConnect(object sender, ConnectionEventArgs e)
        {
           
        }

        void OpenWebApplication_AfterSessionComplete(Session oSession)
        {
            if (oSession.state == SessionStates.Aborted)
            {
                SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, "", "Failed");
                this.BeginError(this, ss);
            }
            else
            {
                SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, "", "Recieved Response");
                this.BeginResponse(this, ss);
            }
        }
		private void OpenWebApplication_BeforeReturningError(Session oSession)
		{
            SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, "", "Failed");
            this.BeginError(this, ss);
            ssList[oSession.id - 1] = ss;
            updateStats();
			bool flag = false;
			this.nbrOfRequests--;
			string text = "NbrOfRequests: " + this.nbrOfRequests;
			bool flag2 = false;
			bool flag3 = false;
			if (oSession["x-overrideGateway"].Contains(this.currentAsiaServer.port))
			{
				flag2 = true;
			}
			else
			{
				if (oSession["x-overrideGateway"].Contains(this.currentNAServer.port))
				{
					flag3 = true;
				}
			}
			if (string.IsNullOrEmpty(oSession["X-RetryNumber"]))
			{
				if (!this.isClosing)
				{
					FProxy.RetryRequest retryRequest = new FProxy.RetryRequest();
					HTTPRequestHeaders hTTPRequestHeaders = new HTTPRequestHeaders();
					if (flag2)
					{
						hTTPRequestHeaders.AssignFromString("GET /ping.htm HTTP/1.1\r\nHost: " + this.currentAsiaServer.address + "\r\nProxy-Connection: keep-alive\r\nCache-Control: max-age=0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*//*//;q=0.8\r\nUser-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.64 Safari/537.31\r\nAccept-Encoding: gzip,deflate,sdch\r\nAccept-Language: sv-SE,sv;q=0.8,en-US;q=0.6,en;q=0.4\r\nAccept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3\r\n\r\n");
						Session session = new Session(hTTPRequestHeaders, null);
						session.PathAndQuery = "/ping.htm";
						session.host = this.currentAsiaServer.address;
						session["x-overrideGateway"] = "socks=127.0.0.1:" + this.currentAsiaServer.port;
						Session session2 = retryRequest.Retry(session, 1);
						if (200 == session2.responseCode)
						{
						}
					}
					oSession["x-overrideGateway"] = "socks=127.0.0.1:" + this.currentNAServer.port;
					Session session3 = retryRequest.Retry(oSession, 1);
					if (200 == session3.responseCode)
					{
						flag = true;
						oSession.oResponse.headers = session3.oResponse.headers;
						oSession.responseBodyBytes = session3.responseBodyBytes;
						oSession.oResponse.headers["Connection"] = "close";
					}
					if (flag3)
					{
						hTTPRequestHeaders.AssignFromString("GET /ping.htm HTTP/1.1\r\nHost: " + this.currentNAServer.address + "\r\nProxy-Connection: keep-alive\r\nCache-Control: max-age=0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*//*//;q=0.8\r\nUser-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.64 Safari/537.31\r\nAccept-Encoding: gzip,deflate,sdch\r\nAccept-Language: sv-SE,sv;q=0.8,en-US;q=0.6,en;q=0.4\r\nAccept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3\r\n\r\n");
						Session session = new Session(hTTPRequestHeaders, null);
						session.PathAndQuery = "/ping.htm";
						session.host = this.currentNAServer.address;
						session["x-overrideGateway"] = "socks=127.0.0.1:" + this.currentNAServer.port;
						Session session2 = retryRequest.Retry(session, 1);
						if (200 == session2.responseCode)
						{
						}
					}
					oSession["x-overrideGateway"] = "socks=127.0.0.1:" + this.currentAsiaServer.port;
					session3 = retryRequest.Retry(oSession, 1);
					if (200 == session3.responseCode)
					{
						flag = true;
						oSession.oResponse.headers = session3.oResponse.headers;
						oSession.responseBodyBytes = session3.responseBodyBytes;
						oSession.oResponse.headers["Connection"] = "close";
					}
				}
				if (!flag)
				{
					oSession.utilSetResponseBody("VPN connection to " + oSession.hostname + oSession.PathAndQuery + " failed. Website could be down");
				}
			}
		}
		private void OpenWebApplication_BeforeResponseClient(Session oSession)
		{
            SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, "", "Recieved Response");
            this.BeginResponse(this, ss);
            if (oSession.id <= ssList.Count)
            {
                ssList[oSession.id - 1] = ss;
            }
            updateStats();
			int value = oSession.responseBodyBytes.Length;
			this.nbrOfRequests--;
			string text = "NbrOfRequests: " + this.nbrOfRequests;
			System.Console.WriteLine(value);
		}
		private void OpenWebApplication_BeforeRequestClient(Session oSession)
		{
            Server serverPick = null;
            oSession.bBufferResponse = false;
            if (currentRequests % 2 == 0)
            {
                 serverPick = currentAsiaServer;
            }
            else if (currentRequests % 2 == 1)
            {
                serverPick = currentNAServer;
            }
         //   else if (currentRequests % 3 == 2)
         //   {
         //       serverPick = currentAsiaServer;
         //   }
                if (oSession.fullUrl.Contains("s.hulu.com") || oSession.fullUrl.Contains("theplatform.com")) // || oSession.fullUrl.Contains("t2.hulu.com"))
            {
                serverPick = currentNAServer;
            }
            SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, serverPick.name, "Sent Request");
            this.BeginRequest(this, ss);
            ssList.Add(ss);
            updateStats();
			this.nbrOfRequests++;
			this.nbrRequest++;
			oSession["x-overrideGateway"] = "socks=127.0.0.1:" + serverPick.port;
			string value = "BeforeRequest:  " + oSession.fullUrl + "\n";
			System.Console.WriteLine(value);
		}

        public void updateStats()
        {
            
            currentRequests = 0;
            for (int i = 0; i < ssList.Count; i++)
            {
                if (ssList[i].status.Contains("Sent"))
                {
                    TimeSpan ts = DateTime.Now.Subtract(ssList[i].dateTime);
                    if (ts.TotalMinutes >= 1)
                    {
                        ssList[i].status = "Timed out";
                    }
                    else
                    {
                        currentRequests++;
                    }
                }
               
            }
           

        }

	}
}
