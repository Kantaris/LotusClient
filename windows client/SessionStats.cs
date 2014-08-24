using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vpngui.WindowsFormsApplication1
{
    class SessionStats
    {
        public int id;
        public string url;
        public string server;
        public string status;
        public DateTime dateTime;
        public SessionStats(int id, string url, string server, string status)
        {
            this.id = id;
            this.url = url;
            this.server = server;
            this.status = status;
            dateTime = DateTime.Now;
        }
    }
}
