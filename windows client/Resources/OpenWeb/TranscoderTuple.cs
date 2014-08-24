using System;
namespace OpenWeb
{
	public class TranscoderTuple
	{
		public string sFormatDescription;
		public System.Type typeFormatter;
		internal TranscoderTuple(string sDescription, System.Type oFormatter)
		{
			this.sFormatDescription = sDescription;
			this.typeFormatter = oFormatter;
		}
	}
}
