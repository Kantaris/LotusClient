using System;
namespace OpenWeb
{
	public class HTTPHeaderItem : System.ICloneable
	{
		[CodeDescription("String name of the HTTP header.")]
		public string Name;
		[CodeDescription("String value of the HTTP header.")]
		public string Value;
		public object Clone()
		{
			return base.MemberwiseClone();
		}
		public HTTPHeaderItem(string sName, string sValue)
		{
			if (string.IsNullOrEmpty(sName))
			{
				sName = string.Empty;
			}
			if (sValue == null)
			{
				sValue = string.Empty;
			}
			this.Name = sName;
			this.Value = sValue;
		}
		public override string ToString()
		{
			return string.Format("{0}: {1}", this.Name, this.Value);
		}
	}
}
