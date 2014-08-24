using System;
namespace OpenWeb
{
	public enum RetryMode : byte
	{
		Always,
		Never,
		IdempotentOnly
	}
}
