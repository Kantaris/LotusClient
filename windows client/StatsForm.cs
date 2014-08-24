using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace vpngui.WindowsFormsApplication1
{
    public partial class StatsForm : Form
    {
        public StatsForm()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        public void updateStats()
        {
            label1.Text = "Total Requests: " + listView1.Items.Count;
            int successful = 0;
            int failed = 0;
            int current = 0;
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].SubItems[3].Text.Contains("Sent"))
                {
                    DateTime dt = DateTime.Parse(listView1.Items[i].SubItems[4].Text);
                    TimeSpan ts = DateTime.Now.Subtract(dt);
                    if (ts.TotalMinutes >= 1)
                    {
                        listView1.Items[i].SubItems[3].Text = "Timed out";
                    }
                    else
                    {
                        current++;
                    }
                }
                else if (listView1.Items[i].SubItems[3].Text.Contains("Recieved"))
                {
                    successful++;
                }
                else if (listView1.Items[i].SubItems[3].Text.Contains("Failed"))
                {
                    failed++;
                }
            }
            label2.Text = "Successful Requests: " + successful;
            label3.Text = "Failed Requests: " + failed;
            label4.Text = "Current Requests: " + current;

        }

        internal void modifyRow(SessionStats e)
        {
            if (listView1.InvokeRequired)
            {
                listView1.BeginInvoke((Action)(() =>
                {
                    if (e.id <= listView1.Items.Count)
                    {
                        listView1.Items[e.id - 1].SubItems[3].Text = e.status;
                        updateStats();
                    }
                }));
            }
        }

        internal void addRow(SessionStats e)
        {
            if (listView1.InvokeRequired)
            {
                listView1.BeginInvoke((Action)(() =>
                {
                    ListViewItem lvi = new ListViewItem(e.id.ToString());
                    lvi.SubItems.Add(e.url);
                    lvi.SubItems.Add(e.server);
                    lvi.SubItems.Add(e.status);
                    lvi.SubItems.Add(e.dateTime.ToString());
                    listView1.Items.Add(lvi);
                    listView1.TopItem = listView1.Items[listView1.Items.Count - 1];
                    updateStats();
                }));
            }
        }
    }
}
