using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using VPN;
using WindowsFormsApplication1.Properties;
using vpngui.Properties;
using vpngui.WindowsFormsApplication1;
using vpn;
using System.Diagnostics;
namespace WindowsFormsApplication1
{
	public class MainControl : UserControl
	{
		private enum connectionStatus
		{
			Disconnected,
			Disconnecting,
			Connecting,
			Connected
		}
		private enum buttonStatus
		{
			Disconnected,
			DisconnectHover,
			Neutral,
			Connecting,
			Connected
		}
		private Bitmap bmp = null;
		private Bitmap red = null;
		private Bitmap redT = null;
		private Bitmap green = null;
		private Bitmap greenT = null;
		private Bitmap serverImage = null;
		private Bitmap previousImage = null;
		private bool hasLeft = true;
		private FProxy fproxy;
		private int fadeIn = 0;
		private Bitmap[] sImg = new Bitmap[25];
		private MainControl.buttonStatus bs = MainControl.buttonStatus.Disconnected;
		private MainControl.connectionStatus cs = MainControl.connectionStatus.Disconnected;
		private PrivateFontCollection fonts;
		private FontFamily family;
		private NetworkInterface[] networkInterfaces;
		private System.Collections.Generic.List<long> bitlist = new System.Collections.Generic.List<long>();
		private long oldValue = 0L;
		private string speedString = "";
		private string connectionString = "Not connected to any VPN service.";
		private string titleString = "Disconnected";
		private IContainer components = null;
		private Timer timerFadeIn;
		private Timer speedTimer;
		private Timer serverTimer;
      //  StatsForm sf;
		public MainControl()
		{
			this.InitializeComponent();
			base.SetStyle(ControlStyles.UserPaint, true);
			base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			base.SetStyle(ControlStyles.DoubleBuffer, true);
			this.fproxy = new FProxy();
         //   sf = new StatsForm();
            try
            {
                DirectoryInfo myProfileDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mozilla\\Firefox\\Profiles\\");
                DirectoryInfo[] dirList = myProfileDirectory.GetDirectories("*.default");
                string myFFPrefFile = dirList[0].FullName + "\\prefs.js";
                if (File.Exists(myFFPrefFile))
                {
                    //We have a pref file so let's make sure it has the proxy setting
                    StreamReader myReader = new StreamReader(myFFPrefFile);
                    string myPrefContents = myReader.ReadToEnd();
                    myReader.Close();
                    if (myPrefContents.Contains("user_pref(\"network.proxy.type\""))
                    {
                        // Add the proxy enable line and write it back to the file
                        string firstpart = myPrefContents.Substring(0, myPrefContents.IndexOf("user_pref(\"network.proxy.type\""));
                        string lastpart = myPrefContents.Substring(myPrefContents.IndexOf("user_pref(\"network.proxy.type\""));
                        lastpart = lastpart.Substring(lastpart.IndexOf(";") + 1);
                        myPrefContents = firstpart + lastpart;
                        File.Delete(myFFPrefFile);
                        File.WriteAllText(myFFPrefFile, myPrefContents);
                        MessageBox.Show("Settings for Firefox have been changed. If Firefox is currently running it will need to be restarted before the changes take affect.");
                    }
                }

            }
            catch (Exception ex) { }
            Version win8version = new Version(6, 2, 9200, 0);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= win8version)
            {
                Loopback.LoopUtil lu = new Loopback.LoopUtil();
                for (int i = 0; i < lu.Apps.Count; i++)
                {
                    if (lu.Apps[i].LoopUtil == false)
                    {
                        MessageBox.Show(this, "Settings for Internet Explorer need to be changed on Windows 8");
                        ProcessStartInfo startInfo = new ProcessStartInfo(Application.StartupPath + "\\loopback.exe");
                        startInfo.Verb = "runas";
                        startInfo.UseShellExecute = true;
                        System.Diagnostics.Process.Start(startInfo);
                        break;
                    }
                }
            }
          
           // OpenVPN ovpn = new OpenVPN();
           // ovpn.ReadConfigs();
           // sf.Show();
           // sf.TopMost = true;
			this.fproxy.ServerChangeEvent += new System.EventHandler(this.fproxy_ServerChangeEvent);
            this.fproxy.BeginRequest += new FProxy.BeginRequestEventHandler(fproxy_BeginRequest);
            this.fproxy.BeginResponse += new FProxy.BeginResponseEventHandler(fproxy_BeginResponse);
            this.fproxy.BeginError += new FProxy.BeginErrorEventHandler(fproxy_BeginError);
			string tempPath = System.IO.Path.GetTempPath();
			System.IO.Directory.CreateDirectory(tempPath + "vpnstuff\\");
			System.IO.Directory.CreateDirectory(tempPath + "vpnstuff\\lib\\");
			if (!System.IO.File.Exists(tempPath + "vpnstuff\\node.dll"))
			{
				System.IO.File.WriteAllBytes(tempPath + "vpnstuff\\node.dll", Resources.node);
			}
			System.IO.File.WriteAllBytes(tempPath + "vpnstuff\\sslocal", Resources.sslocal);
			System.IO.File.WriteAllText(tempPath + "vpnstuff\\lib\\encrypt.js", Resources.encrypt);
			System.IO.File.WriteAllText(tempPath + "vpnstuff\\lib\\inet.js", Resources.inet);
			System.IO.File.WriteAllText(tempPath + "vpnstuff\\lib\\local.js", Resources.local);
			System.IO.File.WriteAllText(tempPath + "vpnstuff\\lib\\merge_sort.js", Resources.merge_sort);
			System.IO.File.WriteAllText(tempPath + "vpnstuff\\lib\\udprelay.js", Resources.udprelay);
			System.IO.File.WriteAllText(tempPath + "vpnstuff\\lib\\utils.js", Resources.utils);
			if (System.IO.File.Exists(Application.StartupPath + "\\font\\CaviarDreams.ttf"))
			{
				this.family = MainControl.LoadFontFamily(Application.StartupPath + "\\font\\CaviarDreams.ttf", out this.fonts);
			}
			this.bmp = new Bitmap(Resources.gui2);
			this.red = new Bitmap(Resources.red3);
			this.green = new Bitmap(Resources.green3);
			this.greenT = new Bitmap(Resources.green3);
			for (int i = 0; i < this.greenT.Width; i++)
			{
				for (int j = 0; j < this.greenT.Height; j++)
				{
					Color pixel = this.greenT.GetPixel(i, j);
					int a = (int)pixel.A;
					if (a > 128)
					{
						this.greenT.SetPixel(i, j, Color.FromArgb(128, pixel));
					}
				}
			}
		}

        void fproxy_BeginError(object sender, SessionStats e)
        {
           // sf.modifyRow(e);
        }

        void fproxy_BeginResponse(object sender, SessionStats e)
        {
          //  sf.modifyRow(e);
        }

        void fproxy_BeginRequest(object sender, vpngui.WindowsFormsApplication1.SessionStats e)
        {
         //   sf.addRow(e);
        }
		private void fproxy_ServerChangeEvent(object sender, System.EventArgs e)
		{
			this.connectionString = "Connected to " + this.fproxy.currentAsiaServer.name;
			this.titleString = this.fproxy.currentAsiaServer.title;
			if (this.serverImage != null)
			{
				this.previousImage = this.serverImage;
			}
			this.serverImage = this.fproxy.currentAsiaServer.image;
			for (int i = 0; i < 25; i++)
			{
				this.sImg[i] = this.serverImage;
			}
			for (int i = 0; i < 25; i++)
			{
				this.sImg[i] = this.SetImageOpacity(this.serverImage, (float)i / 25f);
			}
			this.networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			this.oldValue = 0L;
			for (int i = 0; i < this.networkInterfaces.Length; i++)
			{
				this.oldValue += this.networkInterfaces[i].GetIPv4Statistics().BytesReceived / 1000L * 8L;
			}
			this.fadeIn = 0;
			this.bs = MainControl.buttonStatus.Connected;
			this.cs = MainControl.connectionStatus.Connected;
			this.timerFadeIn.Start();
			this.speedTimer.Start();
		}
		public void close()
		{
			if (this.fproxy != null)
			{
				this.fproxy.DoQuit();
			}
		}
		public static FontFamily LoadFontFamily(string fileName, out PrivateFontCollection fontCollection)
		{
			fontCollection = new PrivateFontCollection();
			fontCollection.AddFontFile(fileName);
			return fontCollection.Families[0];
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
			Font font = new Font(this.family, 92f, FontStyle.Regular, GraphicsUnit.Pixel);
			Font font2 = new Font(this.family, 32f, FontStyle.Bold);
			Color color = ColorTranslator.FromHtml("#2c1b4e");
			Color foreColor = Color.FromArgb(130, 130, 130);
			Brush brush = new SolidBrush(color);
			e.Graphics.DrawImage(this.bmp, 0, 0, this.bmp.Width, this.bmp.Height);
			Brush brush2 = new SolidBrush(Color.White);
			Rectangle rect = new Rectangle(0, 253, this.bmp.Width, 80);
			e.Graphics.FillRectangle(brush2, rect);
			Bitmap bitmap = new Bitmap(976, 488);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				int width = TextRenderer.MeasureText(graphics, this.titleString, font).Width;
				TextRenderer.DrawText(graphics, this.titleString, font, new Point((976 - width) / 2, 0), color);
				int width2 = TextRenderer.MeasureText(graphics, this.connectionString, font2).Width;
				TextRenderer.DrawText(graphics, this.connectionString, font2, new Point((976 - width2) / 2, 150), foreColor);
				int width3 = TextRenderer.MeasureText(graphics, this.speedString, font2).Width;
				TextRenderer.DrawText(graphics, this.speedString, font2, new Point((976 - width3) / 2, 218), foreColor);
				int width4 = TextRenderer.MeasureText(graphics, "Protocol: OpenWeb", font2).Width;
				TextRenderer.DrawText(graphics, "Protocol: OpenWeb", font2, new Point((976 - width4) / 2, 286), foreColor);
				graphics.Save();
			}
			e.Graphics.DrawImage(bitmap, 0, 250, 244, 122);
			bitmap.Dispose();
			if (this.cs == MainControl.connectionStatus.Connected)
			{
				if (this.fadeIn > 24)
				{
					this.fadeIn = 24;
				}
				if (this.previousImage != null)
				{
					e.Graphics.DrawImage(this.previousImage, 0, 0, this.previousImage.Width, this.previousImage.Height);
				}
				e.Graphics.DrawImage(this.sImg[this.fadeIn], 0, 0, this.serverImage.Width, this.serverImage.Height);
			}
			if (this.bs == MainControl.buttonStatus.Connected)
			{
				e.Graphics.DrawImage(this.green, 82, 159, this.green.Width, this.green.Height);
			}
			else
			{
				if (this.bs == MainControl.buttonStatus.Disconnected)
				{
					e.Graphics.DrawImage(this.red, 82, 159, this.red.Width, this.red.Height);
				}
			}
		}
		public Bitmap SetImageOpacity(Image image, float opacity)
		{
			Bitmap result;
			try
			{
				Bitmap bitmap = new Bitmap(image.Width, image.Height);
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					ColorMatrix colorMatrix = new ColorMatrix();
					colorMatrix.Matrix33 = opacity;
					ImageAttributes imageAttributes = new ImageAttributes();
					imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
					graphics.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
				}
				result = bitmap;
			}
			catch (System.Exception ex)
			{
				result = null;
			}
			return result;
		}
		private void MainControl_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.X > 82 && e.X < 82 + this.red.Width && e.Y > 159 && e.Y < 159 + this.green.Height)
			{
				if (this.bs == MainControl.buttonStatus.Connected || (this.bs == MainControl.buttonStatus.Neutral && this.cs == MainControl.connectionStatus.Connected))
				{
					this.connectionString = "Not connected to any VPN service.";
					this.speedTimer.Stop();
					this.speedString = "";
					this.titleString = "Disconnected";
					this.bs = MainControl.buttonStatus.Disconnected;
					this.cs = MainControl.connectionStatus.Disconnected;
					if (this.fproxy != null)
					{
						this.fproxy.DoQuit();
					}
					this.hasLeft = false;
				}
				else
				{
					if (this.bs == MainControl.buttonStatus.Disconnected || (this.bs == MainControl.buttonStatus.Neutral && this.cs == MainControl.connectionStatus.Disconnected))
					{
						this.start();
					}
				}
				this.Refresh();
			}
		}
		private void start()
		{
			this.fproxy.connect();
			this.hasLeft = false;
		}
		private void MainControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.X > 82 && e.X < 82 + this.red.Width && e.Y > 159 && e.Y < 159 + this.green.Height)
			{
				if (this.bs == MainControl.buttonStatus.Connected && this.hasLeft)
				{
					this.bs = MainControl.buttonStatus.Neutral;
					this.Refresh();
				}
				else
				{
					if (this.bs == MainControl.buttonStatus.Disconnected && this.hasLeft)
					{
						this.bs = MainControl.buttonStatus.Neutral;
						this.Refresh();
					}
				}
			}
			else
			{
				if (this.bs == MainControl.buttonStatus.Neutral)
				{
					if (this.cs == MainControl.connectionStatus.Connected)
					{
						this.bs = MainControl.buttonStatus.Connected;
						this.Refresh();
					}
					else
					{
						if (this.cs == MainControl.connectionStatus.Disconnected)
						{
							this.bs = MainControl.buttonStatus.Disconnected;
							this.Refresh();
						}
					}
				}
				else
				{
					this.hasLeft = true;
				}
			}
		}
		private void timerFadeIn_Tick(object sender, System.EventArgs e)
		{
			if (this.fadeIn < 24)
			{
				this.fadeIn++;
				this.Refresh();
			}
			else
			{
				this.timerFadeIn.Stop();
			}
		}
		private void speedTimer_Tick(object sender, System.EventArgs e)
		{
			long num = 0L;
			for (int i = 0; i < this.networkInterfaces.Length; i++)
			{
				num += this.networkInterfaces[i].GetIPv4Statistics().BytesReceived / 1000L * 8L;
			}
			long item = num - this.oldValue;
			if (this.bitlist.Count > 10)
			{
				this.bitlist.RemoveAt(0);
			}
			this.bitlist.Add(item);
			long num2 = 0L;
			for (int i = 0; i < this.bitlist.Count; i++)
			{
				num2 += this.bitlist[i];
			}
			this.speedString = "Download Speed: " + num2 / (long)this.bitlist.Count + " kb/s";
			this.oldValue = num;
			this.Refresh();
		}
		private void serverTimer_Tick(object sender, System.EventArgs e)
		{
			this.fproxy.sortServers();
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}
		private void InitializeComponent()
		{
			this.components = new Container();
			this.timerFadeIn = new Timer(this.components);
			this.speedTimer = new Timer(this.components);
			this.serverTimer = new Timer(this.components);
			base.SuspendLayout();
			this.timerFadeIn.Interval = 50;
			this.timerFadeIn.Tick += new System.EventHandler(this.timerFadeIn_Tick);
			this.speedTimer.Interval = 1000;
			this.speedTimer.Tick += new System.EventHandler(this.speedTimer_Tick);
			this.serverTimer.Interval = 100000;
			this.serverTimer.Tick += new System.EventHandler(this.serverTimer_Tick);
			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.Name = "MainControl";
			base.MouseDown += new MouseEventHandler(this.MainControl_MouseDown);
			base.MouseMove += new MouseEventHandler(this.MainControl_MouseMove);
			base.ResumeLayout(false);
		}
	}
}
