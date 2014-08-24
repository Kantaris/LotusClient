using System;
namespace OpenWeb
{
	[System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class RequiredVersionAttribute : System.Attribute
	{
		private string _sVersion;
		public string RequiredVersion
		{
			get
			{
				return this._sVersion;
			}
		}
		public RequiredVersionAttribute(string sVersion)
		{
			if (sVersion.StartsWith("2."))
			{
				sVersion = "4." + sVersion.Substring(2);
			}
			this._sVersion = sVersion;
		}
	}
}
