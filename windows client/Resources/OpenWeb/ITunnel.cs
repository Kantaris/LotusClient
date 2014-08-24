using System;
namespace OpenWeb
{
	public interface ITunnel
	{
		long IngressByteCount
		{
			get;
		}
		long EgressByteCount
		{
			get;
		}
		bool IsOpen
		{
			get;
		}
		void CloseTunnel();
	}
}
