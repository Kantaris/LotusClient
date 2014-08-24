using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
namespace OpenWeb
{
	internal class RegistryWatcher
	{
		[System.Flags]
		private enum RegistryEventFilter : uint
		{
			Key = 1u,
			Attributes = 2u,
			Values = 4u,
			ACLs = 8u
		}
		private const int KEY_QUERY_VALUE = 1;
		private const int KEY_NOTIFY = 16;
		private const int STANDARD_RIGHTS_READ = 131072;
		private static readonly System.UIntPtr HKEY_CLASSES_ROOT = (System.UIntPtr)2147483648u;
		private static readonly System.UIntPtr HKEY_CURRENT_USER = (System.UIntPtr)2147483649u;
		private static readonly System.UIntPtr HKEY_LOCAL_MACHINE = (System.UIntPtr)2147483650u;
		private static readonly System.UIntPtr HKEY_USERS = (System.UIntPtr)2147483651u;
		private static readonly System.UIntPtr HKEY_PERFORMANCE_DATA = (System.UIntPtr)2147483652u;
		private static readonly System.UIntPtr HKEY_CURRENT_CONFIG = (System.UIntPtr)2147483653u;
		private static readonly System.UIntPtr HKEY_DYN_DATA = (System.UIntPtr)2147483654u;
		private System.UIntPtr _hiveToWatch;
		private string _sSubKey;
		private object _lockForThread = new object();
		private System.Threading.Thread _threadWaitForChanges;
		private bool _disposed;
		private System.Threading.ManualResetEvent _eventTerminate = new System.Threading.ManualResetEvent(false);
		private RegistryWatcher.RegistryEventFilter _regFilter = RegistryWatcher.RegistryEventFilter.Values;
		public event System.EventHandler KeyChanged;
		public bool IsWatching
		{
			get
			{
				return null != this._threadWaitForChanges;
			}
		}
		[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
		private static extern int RegOpenKeyEx(System.UIntPtr hKey, string subKey, uint options, int samDesired, out System.IntPtr phkResult);
		[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
		private static extern int RegNotifyChangeKeyValue(System.IntPtr hKey, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool bWatchSubtree, RegistryWatcher.RegistryEventFilter dwNotifyFilter, System.IntPtr hEvent, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool fAsynchronous);
		[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
		private static extern int RegCloseKey(System.IntPtr hKey);
		protected virtual void OnKeyChanged()
		{
			System.EventHandler keyChanged = this.KeyChanged;
			if (keyChanged != null)
			{
				keyChanged(this, null);
			}
		}
		internal static RegistryWatcher WatchKey(Microsoft.Win32.RegistryHive registryHive, string subKey, System.EventHandler oToNotify)
		{
			RegistryWatcher registryWatcher = new RegistryWatcher(registryHive, subKey);
			registryWatcher.KeyChanged += oToNotify;
			registryWatcher.Start();
			return registryWatcher;
		}
		private RegistryWatcher(Microsoft.Win32.RegistryHive registryHive, string subKey)
		{
			this.InitRegistryKey(registryHive, subKey);
		}
		public void Dispose()
		{
			this.Stop();
			this._disposed = true;
			System.GC.SuppressFinalize(this);
		}
		private void InitRegistryKey(Microsoft.Win32.RegistryHive hive, string name)
		{
			switch (hive)
			{
			case Microsoft.Win32.RegistryHive.ClassesRoot:
				this._hiveToWatch = RegistryWatcher.HKEY_CLASSES_ROOT;
				break;
			case Microsoft.Win32.RegistryHive.CurrentUser:
				this._hiveToWatch = RegistryWatcher.HKEY_CURRENT_USER;
				break;
			case Microsoft.Win32.RegistryHive.LocalMachine:
				this._hiveToWatch = RegistryWatcher.HKEY_LOCAL_MACHINE;
				break;
			case Microsoft.Win32.RegistryHive.Users:
				this._hiveToWatch = RegistryWatcher.HKEY_USERS;
				break;
			case Microsoft.Win32.RegistryHive.PerformanceData:
				this._hiveToWatch = RegistryWatcher.HKEY_PERFORMANCE_DATA;
				break;
			case Microsoft.Win32.RegistryHive.CurrentConfig:
				this._hiveToWatch = RegistryWatcher.HKEY_CURRENT_CONFIG;
				break;
			case Microsoft.Win32.RegistryHive.DynData:
				this._hiveToWatch = RegistryWatcher.HKEY_DYN_DATA;
				break;
			default:
				throw new InvalidEnumArgumentException("hive", (int)hive, typeof(Microsoft.Win32.RegistryHive));
			}
			this._sSubKey = name;
		}
		private void Start()
		{
			if (this._disposed)
			{
				throw new System.ObjectDisposedException(null, "This instance is already disposed");
			}
			lock (this._lockForThread)
			{
				if (!this.IsWatching)
				{
					this._eventTerminate.Reset();
					this._threadWaitForChanges = new System.Threading.Thread(new System.Threading.ThreadStart(this.MonitorThread));
					this._threadWaitForChanges.IsBackground = true;
					this._threadWaitForChanges.Start();
				}
			}
		}
		public void Stop()
		{
			if (this._disposed)
			{
				throw new System.ObjectDisposedException(null, "This instance is already disposed");
			}
			lock (this._lockForThread)
			{
				System.Threading.Thread threadWaitForChanges = this._threadWaitForChanges;
				if (threadWaitForChanges != null)
				{
					this._eventTerminate.Set();
					threadWaitForChanges.Join();
				}
			}
		}
		private void MonitorThread()
		{
			try
			{
				this.WatchAndNotify();
			}
			catch (System.Exception)
			{
			}
			this._threadWaitForChanges = null;
		}
		private void WatchAndNotify()
		{
			System.IntPtr intPtr;
			int num = RegistryWatcher.RegOpenKeyEx(this._hiveToWatch, this._sSubKey, 0u, 131089, out intPtr);
			if (num != 0)
			{
				throw new Win32Exception(num);
			}
			try
			{
				System.Threading.AutoResetEvent autoResetEvent = new System.Threading.AutoResetEvent(false);
				System.Threading.WaitHandle[] waitHandles = new System.Threading.WaitHandle[]
				{
					autoResetEvent,
					this._eventTerminate
				};
				while (!this._eventTerminate.WaitOne(0, true))
				{
					num = RegistryWatcher.RegNotifyChangeKeyValue(intPtr, false, this._regFilter, autoResetEvent.SafeWaitHandle.DangerousGetHandle(), true);
					if (num != 0)
					{
						throw new Win32Exception(num);
					}
					if (System.Threading.WaitHandle.WaitAny(waitHandles) == 0)
					{
						this.OnKeyChanged();
					}
				}
			}
			finally
			{
				if (System.IntPtr.Zero != intPtr)
				{
					RegistryWatcher.RegCloseKey(intPtr);
				}
			}
		}
	}
}
