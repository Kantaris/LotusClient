using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace WindowsFormsApplication1
{
	public class Form1 : Form
	{
		private IContainer components = null;
		//private MainControl mainControl1;
		private MainControl mainControl2;
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
			this.mainControl2 = new MainControl();
			base.SuspendLayout();
			this.mainControl2.Dock = DockStyle.Fill;
			this.mainControl2.Location = new Point(0, 0);
			this.mainControl2.Name = "mainControl2";
			this.mainControl2.Size = new Size(238, 379);
			this.mainControl2.TabIndex = 0;
			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.ClientSize = new Size(238, 349);
			base.Controls.Add(this.mainControl2);
			base.FormBorderStyle = FormBorderStyle.FixedSingle;
			base.MaximizeBox = false;
			base.Name = "Form1";
            
			base.SizeGripStyle = SizeGripStyle.Hide;
			base.TopMost = true;
			base.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
			base.Load += new System.EventHandler(this.Form1_Load);
			base.ResumeLayout(false);
		}
		public Form1(string username, string xml)
		{
            
			this.InitializeComponent();
            this.Text = "LotusVPN";
            mainControl2.setServerXml(username, xml);
		}
		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			this.mainControl2.close();
		}
		private void Form1_Load(object sender, System.EventArgs e)
		{
            //this.BackColor = Color.White;
            //this.Width = 900;
		}
	}
}
