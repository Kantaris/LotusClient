using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace vpngui
{
    public partial class Update : Form
    {
        public Update()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Application.StartupPath + "\\updater.exe");
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }





        internal void showInfo(string udata)
        {
            if(udata.Contains("<version>")){

                label2.Text = "";
                string version = udata.Substring(udata.IndexOf("<version>") + 9);
                version = version.Substring(0, version.IndexOf("</version>"));
                label2.Text = "Version: " + version + "\nChangelog: \n";
                string changeItems = udata;
                while (changeItems.Contains("<changeItem>"))
                {
                    changeItems = changeItems.Substring(udata.IndexOf("<changeItem>") + 12);
                    string changeItem = changeItems.Substring(changeItems.IndexOf("</changeItem>"));
                    label2.Text = label2.Text + changeItem + "\n";

                }

            }

        }

     
    }
}
