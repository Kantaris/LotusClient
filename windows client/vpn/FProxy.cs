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

    
		private int counter = 0;
		private int nbrOfRequests = 0;
        int currentRequests = 0;
		//private string add = "";
		private int nbrRequest = 0;
		private bool isClosing = false;
		private bool canConnect = true; //change back
        public Server server;
        System.Windows.Forms.Timer invokeTimer= new System.Windows.Forms.Timer();
        private bool shouldUpdate = false;
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
		/*	for (int i = 0; i < this.serverList.Count; i++)
			{
				BackgroundWorker backgroundWorker = new BackgroundWorker();
				backgroundWorker.DoWork += new DoWorkEventHandler(this.bw_DoWork);
				backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.bw_RunWorkerCompleted);
				backgroundWorker.RunWorkerAsync(this.serverList[i]);
			}*/
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
			if (this.server != null)
			{
				this.server.Disconnect();
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
            invokeTimer.Interval = 500;
            invokeTimer.Tick += new EventHandler(invokeTimer_Tick);
            invokeTimer.Start();
            server = new Server();
            
            
            //server.image = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\2.png"));
            
        
           
			this.checkServerConnection();
		}

        void invokeTimer_Tick(object sender, EventArgs e)
        {
            if (shouldUpdate)
            {
                ServerChangeEvent(this, null);
                shouldUpdate = false;
            }
        }

        void server_ChangeServer(object sender, SessionStats e)
        {
            shouldUpdate = true;
        }
        void server_ChangeServer2(object sender, SessionStats e)
        {
        }
	/*	public Server fastestAsiaServer()
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
			if (num2 > -1 )
			{
                result = serverList[1];
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
		}*/
		
		public void connect()
		{
			if (this.canConnect)
			{
                server.title = "Tokyo";
                server.name = "Tokyo Server 2";
                server.address = "Auto"; // "157.7.234.46"; //"153.121.58.118";
                server.port = "1181";
                server.password = "barfoo!";
                server.continent = "Asia";
                server.ChangeServer += new Server.ChangeServerEventHandler(server_ChangeServer);
               server.Connect();

				
				string arg = "NoSAZ";
				System.Console.WriteLine(string.Format("Starting {0} ({1})...", OpenWebApplication.GetVersionString(), arg));
				CONFIG.IgnoreServerCertErrors = false;
				OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.streaming.abortifclientaborts", true);
				OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.streaming.ForgetStreamedData", true);
				OpenWebCoreStartupFlags openWebCoreStartupFlags = OpenWebCoreStartupFlags.Default;
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.DecryptSSL;
				OpenWebApplication.Startup(0, openWebCoreStartupFlags);
				OpenWebApplication.BeforeRequest += new SessionStateHandler(this.OpenWebApplication_BeforeRequestClient);
				OpenWebApplication.BeforeResponse += new SessionStateHandler(this.OpenWebApplication_BeforeResponseClient);
				OpenWebApplication.BeforeReturningError += new SessionStateHandler(this.OpenWebApplication_BeforeReturningError);
              
			}
		}

       
		private void OpenWebApplication_BeforeReturningError(Session oSession)
		{
            SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, "", "Failed");
            oSession.utilSetResponseBody("VPN connection to " + oSession.hostname + oSession.PathAndQuery + " failed. Website could be down");
			
		}
		private void OpenWebApplication_BeforeResponseClient(Session oSession)
		{
            SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, "", "Recieved Response");
           
			int value = oSession.responseBodyBytes.Length;
			this.nbrOfRequests--;
			string text = "NbrOfRequests: " + this.nbrOfRequests;
			System.Console.WriteLine(value);
		}
		private void OpenWebApplication_BeforeRequestClient(Session oSession)
		{
            oSession.bBufferResponse = false;
    
            SessionStats ss = new SessionStats(oSession.id, oSession.fullUrl, server.name, "Sent Request");
           
			this.nbrOfRequests++;
			this.nbrRequest++;
			oSession["x-overrideGateway"] = "socks=127.0.0.1:" + server.port;
			string value = "BeforeRequest:  " + oSession.fullUrl + "\n";
			System.Console.WriteLine(value);
		}




        internal void connectServer(vpngui.ServerDetails serverDetails)
        {
        
			if (this.canConnect)
			{
                server = new Server();
                server.title = serverDetails.title;
                server.name = serverDetails.name;
                server.address = serverDetails.address; // "157.7.234.46"; //"153.121.58.118";
                server.port = "1180";
                server.password = "barfoo!";
                server.image = serverDetails.image;
                // server.continent = "Asia";
                server.ChangeServer +=new Server.ChangeServerEventHandler(server_ChangeServer2);
                server.Connect();

				
				string arg = "NoSAZ";
				System.Console.WriteLine(string.Format("Starting {0} ({1})...", OpenWebApplication.GetVersionString(), arg));
				CONFIG.IgnoreServerCertErrors = false;
				OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.streaming.abortifclientaborts", true);
				OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.streaming.ForgetStreamedData", true);
				OpenWebCoreStartupFlags openWebCoreStartupFlags = OpenWebCoreStartupFlags.Default;
				openWebCoreStartupFlags &= ~OpenWebCoreStartupFlags.DecryptSSL;
				OpenWebApplication.Startup(0, openWebCoreStartupFlags);
				OpenWebApplication.BeforeRequest += new SessionStateHandler(this.OpenWebApplication_BeforeRequestClient);
				OpenWebApplication.BeforeResponse += new SessionStateHandler(this.OpenWebApplication_BeforeResponseClient);
				OpenWebApplication.BeforeReturningError += new SessionStateHandler(this.OpenWebApplication_BeforeReturningError);
              
			}
		
        }
    }
}
