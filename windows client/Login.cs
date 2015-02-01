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
using System.Xml;
using System.Reflection;

namespace vpngui
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
            
            System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
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
            string url = "https://157.7.234.46/api/User/Login";
            using (var wb = new WebClient())
            {
                wb.Proxy = null;
                var data = new NameValueCollection();
                int major = Assembly.GetExecutingAssembly().GetName().Version.Major;
                int minor = Assembly.GetExecutingAssembly().GetName().Version.Minor;
                data["username"] = textBox1.Text;
                if (!textBox2.Text.Equals("XXXXXXXX"))
                {
                    data["password"] =  textBox2.Text;
                }
                else
                {
                    data["password"] = Properties.Settings.Default.hash;
                }
                data["major"] = major.ToString();
                data["minor"] = minor.ToString();
                data["os"] = "Windows";
                var response = wb.UploadValues(url, "POST", data);
               
                e.Result = response;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string ss = System.Text.Encoding.Default.GetString((byte[])e.Result);
            if (ss.Contains("<software>"))
            {
                
                    string udata = ss.Substring(ss.IndexOf("<software>"));
                    udata = udata.Substring(0, udata.IndexOf("</software>"));
                    string smajor = udata.Substring(udata.IndexOf("<vmajor>") + 8);
                    smajor = smajor.Substring(0, smajor.IndexOf("</vmajor>"));
                    string sminor = udata.Substring(udata.IndexOf("<vminor>") + 8);
                    sminor = sminor.Substring(0, sminor.IndexOf("</vminor>"));
                    int imajor = int.Parse(smajor);
                    int iminor = int.Parse(sminor);
                    int major = Assembly.GetExecutingAssembly().GetName().Version.Major;
                    int minor = Assembly.GetExecutingAssembly().GetName().Version.Minor;
                    bool needUpdate = false;
                    if (major < imajor)
                    {
                        needUpdate = true;
                    }
                    else if (major == imajor && minor < iminor)
                    {
                        needUpdate = true;
                    }
                    if (needUpdate)
                    {
                        Update up = new Update();
                        up.showInfo(udata);
                        up.Show();
                        this.Hide();
                    }
                
               
            }
            if (ss.Contains("<msg>Success</msg>"))
            {
                if (checkBox1.Checked && textBox1.Text.Length > 0)
                {
                    Properties.Settings.Default.username = textBox1.Text;
                    var parseString = ss.Substring(ss.IndexOf("<hash>") + 6);
                    parseString = parseString.Substring(0, parseString.IndexOf("</hash>"));
                    Properties.Settings.Default.hash = parseString;
                    Properties.Settings.Default.Save();
                }
                Form1 main = new Form1(textBox1.Text, ss);
                main.Show();
                main.FormClosed += new FormClosedEventHandler(main_FormClosed);
               // this.ShowInTaskbar = false;
                this.Hide();
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
