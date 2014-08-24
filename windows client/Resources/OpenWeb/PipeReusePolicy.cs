using System;
namespace OpenWeb
{
	public enum PipeReusePolicy
	{
		NoRestrictions,
		MarriedToClientProcess,
		MarriedToClientPipe,
		NoReuse
	}
}
