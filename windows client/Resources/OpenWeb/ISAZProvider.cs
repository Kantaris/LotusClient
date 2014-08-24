using System;
namespace OpenWeb
{
	public interface ISAZProvider
	{
		bool SupportsEncryption
		{
			get;
		}
		bool BufferLocally
		{
			get;
		}
		ISAZReader LoadSAZ(string sFilename);
		ISAZWriter CreateSAZ(string sFilename);
	}
}
