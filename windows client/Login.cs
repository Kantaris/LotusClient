using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Collections.Specialized;
using WindowsFormsApplication1;

namespace vpngui
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
          
            textBox2.UseSystemPasswordChar = true;
            textBox1.Text = Properties.Settings.Default.username;
            string hash = Properties.Settings.Default.hash;
            if (hash.Length > 0)
            {
                textBox2.Text = "XXXXXXXX"; 
            }
            Properties.Settings.Default.Save();
            
        }

     

     

        private void Login_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = true;

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            label3.Text = "Connecting";
            timer1.Start();
            backgroundWorker1.RunWorkerAsync();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (label3.Text.Equals("Connecting"))
            {
                label3.Text = "Connecting.";
            }
            else if (label3.Text.Equals("Connecting."))
            {
                label3.Text = "Connecting..";
            }
            else if (label3.Text.Equals("Connecting.."))
            {
                label3.Text = "Connecting...";
            }
            else if (label3.Text.Equals("Connecting..."))
            {
                label3.Text = "Connecting";
            }

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string url = "https://lotusvpn.com/login.ashx";
            using (var wb = new WebClient())
            {
                wb.Proxy = null;
                var data = new NameValueCollection();
                data["username"] = textBox1.Text;
                if (!textBox2.Text.Equals("XXXXXXXX"))
                {
                    data["password"] = textBox2.Text;
                }
                else
                {
                    data["password"] = Properties.Settings.Default.hash;
                }
                var response = wb.UploadValues(url, "POST", data);
                e.Result = response;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string ss = System.Text.Encoding.Default.GetString((byte[])e.Result);
            if (!ss.Contains("<error>") )
            {
                if(checkBox1.Checked){
                    Properties.Settings.Default.username = textBox1.Text;
                    var parseString = ss.Substring(ss.IndexOf("<hash>") + 6);
                    parseString = parseString.Substring(0, parseString.IndexOf("</hash>"));
                    Properties.Settings.Default.hash = parseString;
                    Properties.Settings.Default.Save();
                }
                Form1 main = new Form1(ss);
                main.Show();
                main.FormClosed += new FormClosedEventHandler(main_FormClosed);
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                Properties.Settings.Default.username = "";
                Properties.Settings.Default.hash = "";
                Properties.Settings.Default.Save();
                label3.Text = "Username or password is wrong!";
            }
          
        }

        void main_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }
    }
}
