using System;
using System.Collections.Generic;
using System.Threading;
namespace OpenWeb
{
	internal class PeriodicWorker
	{
		internal class taskItem
		{
			public ulong _ulLastRun;
			public uint _iPeriod;
			public SimpleEventHandler _oTask;
			public taskItem(SimpleEventHandler oTask, uint iPeriod)
			{
				this._ulLastRun = Utilities.GetTickCount();
				this._iPeriod = iPeriod;
				this._oTask = oTask;
			}
		}
		private const int CONST_MIN_RESOLUTION = 500;
		private System.Threading.Timer timerInternal;
		private System.Collections.Generic.List<PeriodicWorker.taskItem> oTaskList = new System.Collections.Generic.List<PeriodicWorker.taskItem>();
		internal PeriodicWorker()
		{
			this.timerInternal = new System.Threading.Timer(new System.Threading.TimerCallback(this.doWork), null, 500, 500);
		}
		private void doWork(object objState)
		{
			if (OpenWebApplication.isClosing)
			{
				this.timerInternal.Dispose();
			}
			else
			{
				PeriodicWorker.taskItem[] array;
				lock (this.oTaskList)
				{
					array = new PeriodicWorker.taskItem[this.oTaskList.Count];
					this.oTaskList.CopyTo(array);
				}
				PeriodicWorker.taskItem[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					PeriodicWorker.taskItem taskItem = array2[i];
					if (Utilities.GetTickCount() > taskItem._ulLastRun + (ulong)taskItem._iPeriod)
					{
						taskItem._oTask();
						taskItem._ulLastRun = Utilities.GetTickCount();
					}
				}
			}
		}
		internal PeriodicWorker.taskItem assignWork(SimpleEventHandler workFunction, uint iMS)
		{
			PeriodicWorker.taskItem taskItem = new PeriodicWorker.taskItem(workFunction, iMS);
			lock (this.oTaskList)
			{
				this.oTaskList.Add(taskItem);
			}
			return taskItem;
		}
		internal void revokeWork(PeriodicWorker.taskItem oToRevoke)
		{
			if (oToRevoke != null)
			{
				lock (this.oTaskList)
				{
					this.oTaskList.Remove(oToRevoke);
				}
			}
		}
	}
}
