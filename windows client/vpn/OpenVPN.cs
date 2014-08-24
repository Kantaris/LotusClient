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
		private Connection m_vpn;
		public event OpenVPN.LogEventHandler LogUpdate;
		public event OpenVPN.StatusEventHandler StatusUpdate;
		public void ReadConfigs()
		{
			System.Collections.Generic.List<string> list = UtilsHelper.LocateOpenVPNConfigs("C:\\Program Files (x86)\\OpenVPN\\config\\");
			if (list != null)
			{
				foreach (string current in list)
				{
					try
					{
						this.m_vpn = new UserSpaceConnection(Application.StartupPath + "\\openvpn\\openvpn.exe", current, new System.EventHandler<LogEventArgs>(this.addLog), 0, false);
						this.m_vpn.Connect();
						this.m_vpn.State.StateChanged += new System.EventHandler<StateChangedEventArgs>(this.State_StateChanged);
					}
					catch (System.ArgumentException ex)
					{
					}
				}
			}
		}
		private void State_StateChanged(object sender, StateChangedEventArgs e)
		{
			string text = "";
			long num = 0L;
			StateSnapshot stateSnapshot = this.m_vpn.State.CreateSnapshot();
			string text2 = stateSnapshot.VPNState[1];
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
		public void Disconnect()
		{
			if (this.m_vpn != null)
			{
				this.m_vpn.Disconnect();
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
