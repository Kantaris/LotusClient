using OpenVPNUtils;
using OpenVPNUtils.States;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
namespace vpn
{
	internal class OpenVPN
	{
		public delegate void LogEventHandler(object sender, string es);
		public delegate void StatusEventHandler(object sender, object[] es);
		private Connection m_vpnTCP;
        private Connection m_vpnUDP;
		public event OpenVPN.LogEventHandler LogUpdate;
		public event OpenVPN.StatusEventHandler StatusUpdate;
		public void ReadConfigs()
		{
			System.Collections.Generic.List<string> list = UtilsHelper.LocateOpenVPNConfigs("C:\\Program Files (x86)\\OpenVPN\\config\\");
			if (list != null)
			{
				
				try
				{
					m_vpnTCP = new UserSpaceConnection(Application.StartupPath + "\\openvpn\\openvpn.exe", list[0], new System.EventHandler<LogEventArgs>(this.addLog), 0, false);
                    m_vpnUDP = new UserSpaceConnection(Application.StartupPath + "\\openvpn\\openvpn.exe", list[1], new System.EventHandler<LogEventArgs>(this.addLog), 0, false);
				}
				catch (System.ArgumentException ex)
				{
				}
				
			}
		}
		private void State_StateChanged(object sender, StateChangedEventArgs e)
		{
			string text = "";
			long num = 0L;
            State sta = (State)sender;
			StateSnapshot stateSnapshot = sta.CreateSnapshot();
			string text2 = stateSnapshot.VPNState[1];
            if(stateSnapshot.ConnectionState == VPNConnectionState.Initializing){
                if (text2.Equals("ADD_ROUTES"))
                {
                    num = 150L;
                    text = "Adding routes...";
                }
                else
                {
                    if (text2.Equals("ASSIGN_IP"))
                    {
                        num = 850L;
                        text = "Assigning IP...";
                    }
                    else
                    {
                        if (text2.Equals("AUTH"))
                        {
                            num = 350L;
                            text = "Authenticating...";
                        }
                        else
                        {
                            if (text2.Equals("CONNECTED"))
                            {
                                num = 1000L;
                                text = "Connected";
                            }
                            else
                            {
                                if (text2.Equals("CONNECTING"))
                                {
                                    text = "Connecting...";
                                }
                                else
                                {
                                    if (text2.Equals("EXITING"))
                                    {
                                        text = "Exiting...";
                                    }
                                    else
                                    {
                                        if (text2.Equals("GET_CONFIG"))
                                        {
                                            num = 550L;
                                            text = "Downloading config...";
                                        }
                                        else
                                        {
                                            if (text2.Equals("RECONNECTING"))
                                            {
                                                text = "Reconnecting...";
                                            }
                                            else
                                            {
                                                if (text2.Equals("WAIT"))
                                                {
                                                    text = "Waiting for Server...";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
			}
			if (text.Length > 0)
			{
				object[] es = new object[]
				{
					text,
					num
				};
				OpenVPN.StatusEventHandler statusUpdate = this.StatusUpdate;
				if (statusUpdate != null)
				{
					statusUpdate(this, es);
				}
			}
		}
        public void Connect(bool tcp)
        {
            if (tcp)
            {
                if (this.m_vpnTCP != null)
                {
                    this.m_vpnTCP.State.StateChanged += new System.EventHandler<StateChangedEventArgs>(this.State_StateChanged);
                    m_vpnTCP.Connect();
                }
            }
            else
            {
                if (this.m_vpnUDP != null)
                {
                    this.m_vpnUDP.State.StateChanged += new System.EventHandler<StateChangedEventArgs>(this.State_StateChanged);
                    m_vpnUDP.Connect();
                }
            }
        }
		public void Disconnect(bool tcp)
		{
            if (tcp)
            {
                if (m_vpnTCP != null)
                {
                    m_vpnTCP.Disconnect();
                }
            }
            else
            {
                if (m_vpnUDP != null)
                {
                    m_vpnUDP.Disconnect();
                }
            }
		}
		private void addLog(object sender, LogEventArgs e)
		{
			OpenVPN.LogEventHandler logUpdate = this.LogUpdate;
			if (logUpdate != null)
			{
				logUpdate(this, e.Message);
			}
		}
	}
}
