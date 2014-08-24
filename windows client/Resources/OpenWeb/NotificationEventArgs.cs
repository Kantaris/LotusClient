using System;
namespace OpenWeb
{
	public class NotificationEventArgs : System.EventArgs
	{
		private readonly string _sMessage;
		public string NotifyString
		{
			get
			{
				return this._sMessage;
			}
		}
		internal NotificationEventArgs(string sMsg)
		{
			this._sMessage = sMsg;
		}
	}
}
