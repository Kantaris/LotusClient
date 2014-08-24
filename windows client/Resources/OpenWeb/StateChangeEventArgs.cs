using System;
namespace OpenWeb
{
	public class StateChangeEventArgs : System.EventArgs
	{
		public readonly SessionStates oldState;
		public readonly SessionStates newState;
		internal StateChangeEventArgs(SessionStates ssOld, SessionStates ssNew)
		{
			this.oldState = ssOld;
			this.newState = ssNew;
		}
	}
}
