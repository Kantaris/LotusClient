using System;
namespace OpenWeb
{
	public enum ContinueTransactionReason : byte
	{
		None,
		Authenticate,
		Redirect,
		Tunnel
	}
}
