using System;
using System.Text;
namespace OpenWeb
{
	public class WebSocketTimers
	{
		public System.DateTime dtDoneRead;
		public System.DateTime dtBeginSend;
		public System.DateTime dtDoneSend;
		internal string ToHeaderString()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			if (this.dtDoneRead.Ticks > 0L)
			{
				stringBuilder.AppendFormat("DoneRead: {0}\r\n", this.dtDoneRead.ToString("o"));
			}
			if (this.dtBeginSend.Ticks > 0L)
			{
				stringBuilder.AppendFormat("BeginSend: {0}\r\n", this.dtBeginSend.ToString("o"));
			}
			if (this.dtDoneSend.Ticks > 0L)
			{
				stringBuilder.AppendFormat("DoneSend: {0}\r\n", this.dtDoneSend.ToString("o"));
			}
			return stringBuilder.ToString();
		}
		public override string ToString()
		{
			return this.ToString(false);
		}
		public string ToString(bool bMultiLine)
		{
			string result;
			if (bMultiLine)
			{
				result = string.Format("DoneRead:\t{0:HH:mm:ss.fff}\r\nBeginSend:\t{1:HH:mm:ss.fff}\r\nDoneSend:\t{2:HH:mm:ss.fff}\r\n{3}", new object[]
				{
					this.dtDoneRead,
					this.dtBeginSend,
					this.dtDoneSend,
					(System.TimeSpan.Zero < this.dtDoneSend - this.dtDoneRead) ? string.Format("\r\n\tOverall Elapsed:\t{0:h\\:mm\\:ss\\.fff}\r\n", this.dtDoneSend - this.dtDoneRead) : string.Empty
				});
			}
			else
			{
				result = string.Format("DoneRead: {0:HH:mm:ss.fff}, BeginSend: {1:HH:mm:ss.fff}, DoneSend: {2:HH:mm:ss.fff}{3}", new object[]
				{
					this.dtDoneRead,
					this.dtBeginSend,
					this.dtDoneSend,
					(System.TimeSpan.Zero < this.dtDoneSend - this.dtDoneRead) ? string.Format(",Overall Elapsed: {0:h\\:mm\\:ss\\.fff}", this.dtDoneSend - this.dtDoneRead) : string.Empty
				});
			}
			return result;
		}
	}
}
