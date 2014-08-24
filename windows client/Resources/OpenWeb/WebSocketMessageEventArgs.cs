using System;
namespace OpenWeb
{
	public class WebSocketMessageEventArgs : System.EventArgs
	{
		public WebSocketMessage oWSM
		{
			get;
			private set;
		}
		public WebSocketMessageEventArgs(WebSocketMessage _inMsg)
		{
			this.oWSM = _inMsg;
		}
	}
}
