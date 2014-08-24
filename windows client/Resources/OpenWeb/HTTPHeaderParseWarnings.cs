using System;
namespace OpenWeb
{
	[System.Flags]
	public enum HTTPHeaderParseWarnings
	{
		None = 0,
		EndedWithLFLF = 1,
		EndedWithLFCRLF = 2,
		Malformed = 4
	}
}
