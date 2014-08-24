using System;
namespace OpenWeb
{
	[System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
	public sealed class CodeDescription : System.Attribute
	{
		private string sDesc;
		public string Description
		{
			get
			{
				return this.sDesc;
			}
		}
		public CodeDescription(string desc)
		{
			this.sDesc = desc;
		}
	}
}
