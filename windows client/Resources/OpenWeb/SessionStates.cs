using System;
namespace OpenWeb
{
	public enum SessionStates
	{
		Created,
		ReadingRequest,
		AutoTamperRequestBefore,
		HandTamperRequest,
		AutoTamperRequestAfter,
		SendingRequest,
		ReadingResponse,
		AutoTamperResponseBefore,
		HandTamperResponse,
		AutoTamperResponseAfter,
		SendingResponse,
		Done,
		Aborted
	}
}
