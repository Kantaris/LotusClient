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
using vpngui;
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
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem modeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openWebToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openVPNToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stealthVPNToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem serversToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;


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
        int fadeInProgress = 0;
        bool hasLoggedIn = true; //false;  
        private OpenVPN openvpn = new OpenVPN();
        enum MODE 
        {
            OPENWEB = 0,
            OPENVPN,
            STEALTHVPN
        };
        MODE mode = MODE.OPENWEB;
        private List<ServerDetails> serverList = new List<ServerDetails>();
        string username = "";
        string hash = "";
      //  StatsForm sf;
		public MainControl()
		{
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.InitializeComponent();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 260);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(216, 20);
            this.textBox1.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(12, 300);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(216, 20);
            this.textBox2.TabIndex = 1;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(12, 325);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(94, 17);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "Remember me";
            this.checkBox1.BackColor = Color.White;

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 230);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Username:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 270);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Password:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 144);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 13);
            this.label3.TabIndex = 6;
           // this.Controls.Add(this.label3);
           // this.Controls.Add(this.label2);
           // this.Controls.Add(this.label1);
           // this.Controls.Add(this.textBox2);
           // this.Controls.Add(this.textBox1);
           // this.Controls.Add(this.checkBox1);
			base.SetStyle(ControlStyles.UserPaint, true);
			base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			base.SetStyle(ControlStyles.DoubleBuffer, true);
			this.fproxy = new FProxy();
            this.MouseEnter += new EventHandler(MainControl_MouseHover);
            this.MouseHover += new EventHandler(MainControl_MouseHover);
            this.MouseLeave += new EventHandler(MainControl_MouseLeave);
            menuStrip1.MouseLeave += new EventHandler(MainControl_MouseLeave);
            openvpn.StatusUpdate += new OpenVPN.StatusEventHandler(openvpn_StatusUpdate);
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
          
			string tempPath = System.IO.Path.GetTempPath();

			
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
           // this.timerFadeIn.Start();
		}

        void MainControl_MouseEnter(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

      

     

        void MainControl_MouseLeave(object sender, EventArgs e)
        {
            bool mouse_on_control = false;
            foreach (Control c in Controls)
                mouse_on_control |= c.RectangleToScreen(c.ClientRectangle).Contains(Cursor.Position);
            if (!mouse_on_control)
            {
                if (menuStrip1.Visible)
                {
                    if (!menuStrip1.ContainsFocus)
                    {
                        menuStrip1.Hide();
                    }
                }
            }
        }

        void MainControl_MouseHover(object sender, EventArgs e)
        {
            if (!menuStrip1.Visible)
            {
                menuStrip1.Show();
            }
          
        }

        void openvpn_StatusUpdate(object sender, object[] es)
        {
            connectionString = (string)es[0];
            fadeInProgress = (int)((long)es[1] / 40);
        }

   
		private void fproxy_ServerChangeEvent(object sender, System.EventArgs e)
		{
            updateServer();
            
		}

        private void updateServer()
        {
            this.connectionString = "Connected to " + this.fproxy.server.name;
            this.titleString = this.fproxy.server.title;
            if (this.serverImage != null)
            {
                this.previousImage = this.serverImage;
            }
            this.serverImage = new Bitmap(Image.FromFile(Application.StartupPath + "\\img\\" + this.fproxy.server.image));
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
            if (mode == MODE.OPENWEB)
            {
                if (this.fproxy != null)
                {
                    this.fproxy.DoQuit();
                }
            }
            else
            {
                openvpn.Disconnect(true);
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
            if(hasLoggedIn){
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
            }
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
                e.Graphics.DrawImage(this.sImg[this.fadeIn], 0, 0, sImg[this.fadeIn].Width, sImg[this.fadeIn].Height);
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
                    this.serverImage = null;
                    this.previousImage = null;
                    if (mode == MODE.OPENWEB)
                    {
                        if (this.fproxy != null)
                        {
                            this.fproxy.DoQuit();
                        }
                    }
                    else if(mode == MODE.STEALTHVPN){
                        openvpn.Disconnect(true);
                    }
					this.hasLeft = false;
				}
				else
				{
					if (this.bs == MainControl.buttonStatus.Disconnected || (this.bs == MainControl.buttonStatus.Neutral && this.cs == MainControl.connectionStatus.Disconnected))
					{
                        if (mode == MODE.OPENWEB)
                        {
                            this.bs = MainControl.buttonStatus.Connected;
                            this.start();
                        }
                        else if (mode == MODE.STEALTHVPN)
                        {
                            this.bs = MainControl.buttonStatus.Connected;
                            fadeInProgress = 0;
                            openvpn.Connect(true);
                            timerFadeIn.Start();
                        }
					}
				}
				this.Refresh();
			}
           
		}
		private void start()
		{
			this.fproxy.connect(username, hash);
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
                if (fadeIn < fadeInProgress || mode == MODE.OPENWEB)
                {
                    this.fadeIn++;
                }
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

			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.Name = "MainControl";
			base.MouseDown += new MouseEventHandler(this.MainControl_MouseDown);
			base.MouseMove += new MouseEventHandler(this.MainControl_MouseMove);

            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.modeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openWebToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openVPNToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stealthVPNToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serversToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.modeToolStripMenuItem,
            this.serversToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(284, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // modeToolStripMenuItem
            // 
            this.modeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openWebToolStripMenuItem,
            this.openVPNToolStripMenuItem,
            this.stealthVPNToolStripMenuItem});
            this.modeToolStripMenuItem.Name = "modeToolStripMenuItem";
            this.modeToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
            this.modeToolStripMenuItem.Text = "Mode";
            // 
            // openWebToolStripMenuItem
            // 
            this.openWebToolStripMenuItem.Name = "openWebToolStripMenuItem";
            this.openWebToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openWebToolStripMenuItem.Click += new EventHandler(openWebToolStripMenuItem_Click);
            this.openWebToolStripMenuItem.Text = "OpenWeb+";
            this.openWebToolStripMenuItem.Checked = true;
            // 
            // openVPNToolStripMenuItem
            // 
            this.openVPNToolStripMenuItem.Name = "openVPNToolStripMenuItem";
            this.openVPNToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openVPNToolStripMenuItem.Click += new EventHandler(openVPNToolStripMenuItem_Click);
            this.openVPNToolStripMenuItem.Text = "OpenVPN";
            // 
            // stealthVPNToolStripMenuItem
            // 
            this.stealthVPNToolStripMenuItem.Name = "stealthVPNToolStripMenuItem";
            this.stealthVPNToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.stealthVPNToolStripMenuItem.Click += new EventHandler(stealthVPNToolStripMenuItem_Click);
            this.stealthVPNToolStripMenuItem.Text = "StealthVPN";
            // 
            // serversToolStripMenuItem
            // 
            this.serversToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoToolStripMenuItem});
            this.serversToolStripMenuItem.Name = "serversToolStripMenuItem";
            this.serversToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.serversToolStripMenuItem.Text = "Servers";
            // 
            // autoToolStripMenuItem
            // 
            this.autoToolStripMenuItem.Name = "autoToolStripMenuItem";
            this.autoToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.autoToolStripMenuItem.Text = "Auto";
            this.autoToolStripMenuItem.Click += new EventHandler(autoToolStripMenuItem_Click);
            this.autoToolStripMenuItem.Checked = true;
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "About";
            menuStrip1.ResumeLayout(false);
            menuStrip1.Hide();
            this.Controls.Add(this.menuStrip1);
			base.ResumeLayout(false);
		}

        void stealthVPNToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < modeToolStripMenuItem.DropDownItems.Count; i++)
            {
                ((System.Windows.Forms.ToolStripMenuItem)modeToolStripMenuItem.DropDownItems[i]).Checked = false;
            }
            ((System.Windows.Forms.ToolStripMenuItem)sender).Checked = true;
            openvpn.ReadConfigs();
            mode = MODE.STEALTHVPN;
        }

        void openVPNToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < modeToolStripMenuItem.DropDownItems.Count; i++)
            {
                ((System.Windows.Forms.ToolStripMenuItem)modeToolStripMenuItem.DropDownItems[i]).Checked = false;
            }
            ((System.Windows.Forms.ToolStripMenuItem)sender).Checked = true;
            openvpn.ReadConfigs();
            mode = MODE.OPENVPN;
        }

        void openWebToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < modeToolStripMenuItem.DropDownItems.Count; i++)
            {
                ((System.Windows.Forms.ToolStripMenuItem)modeToolStripMenuItem.DropDownItems[i]).Checked = false;
            }
            ((System.Windows.Forms.ToolStripMenuItem)sender).Checked = true;
            mode = MODE.OPENWEB;
        }

        void autoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < serversToolStripMenuItem.DropDownItems.Count; i++)
            {
                ((System.Windows.Forms.ToolStripMenuItem)serversToolStripMenuItem.DropDownItems[i]).Checked = false;
            }
            ((System.Windows.Forms.ToolStripMenuItem)sender).Checked = true;
            this.serverImage = null;
            if (this.fproxy != null)
            {
                this.fproxy.DoQuit();
            }
            fproxy.connect(username, hash);
        }

        internal void setServerXml(string username, string result)
        {
            this.username = username;
            string xhash = result.Substring(result.IndexOf("<hash>") + 6);
            xhash = xhash.Substring(0, xhash.IndexOf("</hash>"));
            hash = xhash;
            while(result.IndexOf("<server>") > -1){
				var sserver = new ServerDetails();
				var parseString = result.Substring(result.IndexOf("<title>") + 7);
				parseString = parseString.Substring(0, parseString.IndexOf("</title>"));
				sserver.title = parseString;
				parseString = result.Substring(result.IndexOf("<name>") + 6);
				parseString = parseString.Substring(0, parseString.IndexOf("</name>"));
				sserver.name = parseString;
				parseString = result.Substring(result.IndexOf("<address>") + 9);
				parseString = parseString.Substring(0, parseString.IndexOf("</address>"));
				sserver.address = parseString;
				parseString = result.Substring(result.IndexOf("<port>") + 6);
				parseString = parseString.Substring(0, parseString.IndexOf("</port>"));
				sserver.port = parseString;
				parseString = result.Substring(result.IndexOf("<country>") + 9);
				parseString = parseString.Substring(0, parseString.IndexOf("</country>"));
				sserver.country = parseString;
				parseString = result.Substring(result.IndexOf("<continent>") + 11);
				parseString = parseString.Substring(0, parseString.IndexOf("</continent>"));
				sserver.continent = parseString;
				parseString = result.Substring(result.IndexOf("<hulu>") + 6);
				parseString = parseString.Substring(0, parseString.IndexOf("</hulu>"));
				sserver.hulu = parseString;
				parseString = result.Substring(result.IndexOf("<image>") + 7);
				parseString = parseString.Substring(0, parseString.IndexOf("</image>"));
				sserver.image = parseString;
				serverList.Add(sserver);
                System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
                item.Size = new System.Drawing.Size(152, 22);
                item.Text = sserver.name;
                
                item.Click += new EventHandler(item_Click);
                serversToolStripMenuItem.DropDownItems.Add(item);
				result = result.Substring(result.IndexOf("</server>") + 9);
			}
            
            
        }

        void item_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < serversToolStripMenuItem.DropDownItems.Count; i++)
            {
                ((System.Windows.Forms.ToolStripMenuItem)serversToolStripMenuItem.DropDownItems[i]).Checked = false;
            }
            ((System.Windows.Forms.ToolStripMenuItem)sender).Checked = true;
            for (int i = 0; i < serverList.Count; i++){
                string serverName = ((System.Windows.Forms.ToolStripMenuItem)sender).Text;
                if(serverName.Equals(serverList[i].name)){
                     
                     this.serverImage = null;
                     if (this.fproxy != null)
                     {
                         this.fproxy.DoQuit();
                     }
                     fproxy.connectServer(username, hash, serverList[i]);
                     updateServer();
                     break;
                }
            }
           
        }
    }
}
