using System;
namespace OpenWeb
{
	public class ProgressCallbackEventArgs : System.EventArgs
	{
		private readonly string _sProgressText;
		private readonly int _PercentDone;
		public bool Cancel
		{
			get;
			set;
		}
		public string ProgressText
		{
			get
			{
				return this._sProgressText;
			}
		}
		public int PercentComplete
		{
			get
			{
				return this._PercentDone;
			}
		}
		public ProgressCallbackEventArgs(float flCompletionRatio, string sProgressText)
		{
			this._sProgressText = (sProgressText ?? string.Empty);
			this._PercentDone = (int)System.Math.Truncate((double)(100f * System.Math.Max(0f, System.Math.Min(1f, flCompletionRatio))));
		}
	}
}
