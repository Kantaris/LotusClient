using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
namespace vpn
{
	internal class Server
	{
		private Process myProcess;
		private bool hasConnected = false;
		private bool hasExit = false;
		public string title;
		public string name;
		public Bitmap image;
		public string address;
		public string password;
		public string port;
		public string continent;
		public string country;
		public int ping = -1;
		public bool isConnected = false;
		public void Connect()
		{
			this.startSSH();
			this.hasConnected = true;
			this.isConnected = true;
		}
		public int pingServer()
		{
			System.DateTime now = System.DateTime.Now;
			WebClient webClient = new WebClient();
			int result;
			try
			{
				webClient.Proxy = null;
				webClient.DownloadString("http://" + this.address + "/index.php");
				System.TimeSpan timeSpan = System.DateTime.Now.Subtract(now);
				this.ping = (int)timeSpan.TotalMilliseconds;
				result = (int)timeSpan.TotalMilliseconds;
			}
			catch (System.Exception var_3_5C)
			{
				this.ping = -1;
				result = -1;
			}
			return result;
		}
		private void startSSH()
		{
			string tempPath = System.IO.Path.GetTempPath();
			if (this.myProcess != null)
			{
				this.Disconnect();
			}
			this.myProcess = new Process();
			this.myProcess.Exited += new System.EventHandler(this.myProcess_Exited);
			this.hasConnected = false;
			this.myProcess.StartInfo.FileName = "\"" + tempPath + "vpnstuff\\node.dll\"";
			this.myProcess.StartInfo.Arguments = string.Concat(new string[]
			{
				"\"",
				tempPath,
				"vpnstuff\\sslocal\" -s ",
				this.address,
				" -p 8388 -l ",
				this.port,
				" -k ",
				this.password,
				" -m aes-256-cfb"
			});
			this.myProcess.StartInfo.UseShellExecute = false;
			this.myProcess.StartInfo.CreateNoWindow = true;
			this.myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			this.myProcess.StartInfo.RedirectStandardError = true;
			this.myProcess.StartInfo.RedirectStandardInput = true;
			this.myProcess.StartInfo.RedirectStandardOutput = true;
			this.myProcess.OutputDataReceived += new DataReceivedEventHandler(this.ProcessOutputHandler);
			this.myProcess.ErrorDataReceived += new DataReceivedEventHandler(this.ProcessErrorHandler);
			this.myProcess.Start();
			this.myProcess.StandardInput.AutoFlush = true;
			this.myProcess.BeginErrorReadLine();
			this.myProcess.BeginOutputReadLine();
		}
		private void myProcess_Exited(object sender, System.EventArgs e)
		{
			this.hasExit = true;
		}
		public void Disconnect()
		{
			if (!this.hasExit)
			{
				this.hasExit = true;
				if (this.myProcess != null && !this.myProcess.HasExited)
				{
					try
					{
						this.myProcess.Kill();
					}
					catch (System.Exception var_0_3E)
					{
					}
				}
			}
		}
		public static void WriteErrorResponse(string s)
		{
			System.ConsoleColor foregroundColor = System.Console.ForegroundColor;
			System.Console.ForegroundColor = System.ConsoleColor.Red;
			System.Console.WriteLine(s);
			System.Console.ForegroundColor = foregroundColor;
		}
		private void ProcessErrorHandler(object sender, DataReceivedEventArgs e)
		{
			string data = e.Data;
			Server.WriteErrorResponse(data);
			if (((Process)sender).StartInfo.RedirectStandardInput)
			{
				try
				{
					((Process)sender).StandardInput.WriteLine("y");
					this.hasConnected = true;
				}
				catch (System.Exception var_1_48)
				{
				}
			}
		}
		private void ProcessOutputHandler(object sender, DataReceivedEventArgs e)
		{
			this.hasExit = false;
			string data = e.Data;
			Server.WriteErrorResponse(data);
			if (e.Data != null && e.Data.Contains("Last login:"))
			{
			}
		}
	}
}
