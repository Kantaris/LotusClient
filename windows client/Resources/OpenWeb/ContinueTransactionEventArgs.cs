using System;
namespace OpenWeb
{
	public class ContinueTransactionEventArgs : System.EventArgs
	{
		private Session _sessOriginal;
		private Session _sessNew;
		private ContinueTransactionReason _reason;
		public ContinueTransactionReason reason
		{
			get
			{
				return this._reason;
			}
		}
		public Session originalSession
		{
			get
			{
				return this._sessOriginal;
			}
		}
		public Session newSession
		{
			get
			{
				return this._sessNew;
			}
		}
		internal ContinueTransactionEventArgs(Session originalSession, Session newSession, ContinueTransactionReason reason)
		{
			this._sessOriginal = originalSession;
			this._sessNew = newSession;
			this._reason = reason;
		}
	}
}
