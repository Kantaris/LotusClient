using System;
using System.Collections.Generic;
using System.Threading;
namespace OpenWeb
{
	public static class ScheduledTasks
	{
		private class jobItem
		{
			internal ulong _ulRunAfter;
			internal SimpleEventHandler _oJob;
			internal jobItem(SimpleEventHandler oJob, uint iMaxDelay)
			{
				this._ulRunAfter = (ulong)iMaxDelay + Utilities.GetTickCount();
				this._oJob = oJob;
			}
		}
		private const int CONST_MIN_RESOLUTION = 15;
		private static System.Collections.Generic.Dictionary<string, ScheduledTasks.jobItem> _dictSchedule = new System.Collections.Generic.Dictionary<string, ScheduledTasks.jobItem>();
		private static System.Threading.Timer _timerInternal = null;
		private static System.Threading.ReaderWriterLock _RWLockDict = new System.Threading.ReaderWriterLock();
		private static void doWork(object objState)
		{
			System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, ScheduledTasks.jobItem>> list = null;
			try
			{
				ScheduledTasks._RWLockDict.AcquireReaderLock(-1);
				ulong tickCount = Utilities.GetTickCount();
				foreach (System.Collections.Generic.KeyValuePair<string, ScheduledTasks.jobItem> current in ScheduledTasks._dictSchedule)
				{
					if (tickCount > current.Value._ulRunAfter)
					{
						current.Value._ulRunAfter = 18446744073709551615uL;
						if (list == null)
						{
							list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, ScheduledTasks.jobItem>>();
						}
						list.Add(current);
					}
				}
				if (list == null)
				{
					return;
				}
				System.Threading.LockCookie lockCookie = ScheduledTasks._RWLockDict.UpgradeToWriterLock(-1);
				try
				{
					foreach (System.Collections.Generic.KeyValuePair<string, ScheduledTasks.jobItem> current2 in list)
					{
						ScheduledTasks._dictSchedule.Remove(current2.Key);
					}
					if (ScheduledTasks._dictSchedule.Count < 1 && ScheduledTasks._timerInternal != null)
					{
						ScheduledTasks._timerInternal.Dispose();
						ScheduledTasks._timerInternal = null;
					}
				}
				finally
				{
					ScheduledTasks._RWLockDict.DowngradeFromWriterLock(ref lockCookie);
				}
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseReaderLock();
			}
			foreach (System.Collections.Generic.KeyValuePair<string, ScheduledTasks.jobItem> current3 in list)
			{
				try
				{
					current3.Value._oJob();
				}
				catch (System.Exception)
				{
				}
			}
		}
		public static bool CancelWork(string sTaskName)
		{
			bool result;
			try
			{
				ScheduledTasks._RWLockDict.AcquireWriterLock(-1);
				result = ScheduledTasks._dictSchedule.Remove(sTaskName);
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseWriterLock();
			}
			return result;
		}
		public static bool ScheduleWork(string sTaskName, uint iMaxDelay, SimpleEventHandler workFunction)
		{
			bool result;
			try
			{
				ScheduledTasks._RWLockDict.AcquireReaderLock(-1);
				if (ScheduledTasks._dictSchedule.ContainsKey(sTaskName))
				{
					bool flag = false;
					result = flag;
					return result;
				}
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseReaderLock();
			}
			ScheduledTasks.jobItem value = new ScheduledTasks.jobItem(workFunction, iMaxDelay);
			try
			{
				ScheduledTasks._RWLockDict.AcquireWriterLock(-1);
				if (ScheduledTasks._dictSchedule.ContainsKey(sTaskName))
				{
					bool flag = false;
					result = flag;
					return result;
				}
				ScheduledTasks._dictSchedule.Add(sTaskName, value);
				if (ScheduledTasks._timerInternal == null)
				{
					ScheduledTasks._timerInternal = new System.Threading.Timer(new System.Threading.TimerCallback(ScheduledTasks.doWork), null, 15, 15);
				}
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseWriterLock();
			}
			result = true;
			return result;
		}
	}
}
