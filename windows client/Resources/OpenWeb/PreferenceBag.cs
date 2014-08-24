using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
namespace OpenWeb
{
	public class PreferenceBag : IOpenWebPreferences
	{
		public struct PrefWatcher
		{
			internal readonly System.EventHandler<PrefChangeEventArgs> fnToNotify;
			internal readonly string sPrefixToWatch;
			internal PrefWatcher(string sPrefixFilter, System.EventHandler<PrefChangeEventArgs> fnHandler)
			{
				this.sPrefixToWatch = sPrefixFilter;
				this.fnToNotify = fnHandler;
			}
		}
		private readonly StringDictionary _dictPrefs = new StringDictionary();
		private readonly System.Collections.Generic.List<PreferenceBag.PrefWatcher> _listWatchers = new System.Collections.Generic.List<PreferenceBag.PrefWatcher>();
		private readonly ReaderWriterLockSlim _RWLockPrefs = new ReaderWriterLockSlim();
		private readonly ReaderWriterLockSlim _RWLockWatchers = new ReaderWriterLockSlim();
		private string _sRegistryPath;
		private string _sCurrentProfile = ".default";
		private static char[] _arrForbiddenChars = new char[]
		{
			'*',
			' ',
			'$',
			'%',
			'@',
			'?',
			'!'
		};
		public string CurrentProfile
		{
			get
			{
				return this._sCurrentProfile;
			}
		}
		public string this[string sPrefName]
		{
			get
			{
				string result;
				try
				{
					PreferenceBag.GetReaderLock(this._RWLockPrefs);
					result = this._dictPrefs[sPrefName];
				}
				finally
				{
					PreferenceBag.FreeReaderLock(this._RWLockPrefs);
				}
				return result;
			}
			set
			{
				if (!PreferenceBag.isValidName(sPrefName))
				{
					throw new System.ArgumentException(string.Format("Preference name must contain 1 to 255 characters from the set A-z0-9-_ and may not contain the word Internal.\n\nCaller tried to set: \"{0}\"", sPrefName));
				}
				if (value == null)
				{
					this.RemovePref(sPrefName);
				}
				else
				{
					bool flag = false;
					try
					{
						PreferenceBag.GetWriterLock(this._RWLockPrefs);
						if (value != this._dictPrefs[sPrefName])
						{
							flag = true;
							this._dictPrefs[sPrefName] = value;
						}
					}
					finally
					{
						PreferenceBag.FreeWriterLock(this._RWLockPrefs);
					}
					if (flag)
					{
						PrefChangeEventArgs oNotifyArgs = new PrefChangeEventArgs(sPrefName, value);
						this.AsyncNotifyWatchers(oNotifyArgs);
					}
				}
			}
		}
		private static void GetReaderLock(ReaderWriterLockSlim oLock)
		{
			oLock.EnterReadLock();
		}
		private static void FreeReaderLock(ReaderWriterLockSlim oLock)
		{
			oLock.ExitReadLock();
		}
		private static void GetWriterLock(ReaderWriterLockSlim oLock)
		{
			oLock.EnterWriteLock();
		}
		private static void FreeWriterLock(ReaderWriterLockSlim oLock)
		{
			oLock.ExitWriteLock();
		}
		internal PreferenceBag(string sRegPath)
		{
			this._sRegistryPath = sRegPath;
			this.ReadRegistry();
		}
		public static bool isValidName(string sName)
		{
			return !string.IsNullOrEmpty(sName) && 256 > sName.Length && !sName.OICContains("internal") && 0 > sName.IndexOfAny(PreferenceBag._arrForbiddenChars);
		}
		private void ReadRegistry()
		{
			if (this._sRegistryPath != null)
			{
				Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(this._sRegistryPath + "\\" + this._sCurrentProfile, Microsoft.Win32.RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.ExecuteKey);
				if (registryKey != null)
				{
					string[] valueNames = registryKey.GetValueNames();
					try
					{
						PreferenceBag.GetWriterLock(this._RWLockPrefs);
						string[] array = valueNames;
						for (int i = 0; i < array.Length; i++)
						{
							string text = array[i];
							if (text.Length >= 1 && !text.OICContains("ephemeral"))
							{
								try
								{
									this._dictPrefs[text] = (string)registryKey.GetValue(text, string.Empty);
								}
								catch (System.Exception)
								{
								}
							}
						}
					}
					finally
					{
						PreferenceBag.FreeWriterLock(this._RWLockPrefs);
						registryKey.Close();
					}
				}
			}
		}
		private void WriteRegistry()
		{
			if (!CONFIG.bIsViewOnly)
			{
				if (this._sRegistryPath != null)
				{
					Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(this._sRegistryPath, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
					if (registryKey != null)
					{
						try
						{
							PreferenceBag.GetReaderLock(this._RWLockPrefs);
							registryKey.DeleteSubKey(this._sCurrentProfile, false);
							if (this._dictPrefs.Count >= 1)
							{
								registryKey = registryKey.CreateSubKey(this._sCurrentProfile, Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
								foreach (System.Collections.DictionaryEntry dictionaryEntry in this._dictPrefs)
								{
									string text = (string)dictionaryEntry.Key;
									if (!text.OICContains("ephemeral"))
									{
										registryKey.SetValue(text, dictionaryEntry.Value);
									}
								}
							}
						}
						finally
						{
							PreferenceBag.FreeReaderLock(this._RWLockPrefs);
							registryKey.Close();
						}
					}
				}
			}
		}
		public string[] GetPrefArray()
		{
			string[] result;
			try
			{
				PreferenceBag.GetReaderLock(this._RWLockPrefs);
				string[] array = new string[this._dictPrefs.Count];
				this._dictPrefs.Keys.CopyTo(array, 0);
				result = array;
			}
			finally
			{
				PreferenceBag.FreeReaderLock(this._RWLockPrefs);
			}
			return result;
		}
		public string GetStringPref(string sPrefName, string sDefault)
		{
			string text = this[sPrefName];
			return text ?? sDefault;
		}
		public bool GetBoolPref(string sPrefName, bool bDefault)
		{
			string text = this[sPrefName];
			bool result;
			if (text == null)
			{
				result = bDefault;
			}
			else
			{
				bool flag;
				if (bool.TryParse(text, out flag))
				{
					result = flag;
				}
				else
				{
					result = bDefault;
				}
			}
			return result;
		}
		public int GetInt32Pref(string sPrefName, int iDefault)
		{
			string text = this[sPrefName];
			int result;
			if (text == null)
			{
				result = iDefault;
			}
			else
			{
				int num;
				if (int.TryParse(text, out num))
				{
					result = num;
				}
				else
				{
					result = iDefault;
				}
			}
			return result;
		}
		public void SetStringPref(string sPrefName, string sValue)
		{
			this[sPrefName] = sValue;
		}
		public void SetInt32Pref(string sPrefName, int iValue)
		{
			this[sPrefName] = iValue.ToString();
		}
		public void SetBoolPref(string sPrefName, bool bValue)
		{
			this[sPrefName] = bValue.ToString();
		}
		public void RemovePref(string sPrefName)
		{
			bool flag = false;
			try
			{
				PreferenceBag.GetWriterLock(this._RWLockPrefs);
				flag = this._dictPrefs.ContainsKey(sPrefName);
				this._dictPrefs.Remove(sPrefName);
			}
			finally
			{
				PreferenceBag.FreeWriterLock(this._RWLockPrefs);
			}
			if (flag)
			{
				PrefChangeEventArgs oNotifyArgs = new PrefChangeEventArgs(sPrefName, null);
				this.AsyncNotifyWatchers(oNotifyArgs);
			}
		}
		private void _clearWatchers()
		{
			PreferenceBag.GetWriterLock(this._RWLockWatchers);
			try
			{
				this._listWatchers.Clear();
			}
			finally
			{
				PreferenceBag.FreeWriterLock(this._RWLockWatchers);
			}
		}
		public void Close()
		{
			this._clearWatchers();
			this.WriteRegistry();
		}
		public override string ToString()
		{
			return this.ToString(true);
		}
		public string ToString(bool bVerbose)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);
			try
			{
				PreferenceBag.GetReaderLock(this._RWLockPrefs);
				stringBuilder.AppendFormat("PreferenceBag [{0} Preferences. {1} Watchers.]", this._dictPrefs.Count, this._listWatchers.Count);
				if (bVerbose)
				{
					stringBuilder.Append("\n");
					foreach (System.Collections.DictionaryEntry dictionaryEntry in this._dictPrefs)
					{
						stringBuilder.AppendFormat("{0}:\t{1}\n", dictionaryEntry.Key, dictionaryEntry.Value);
					}
				}
			}
			finally
			{
				PreferenceBag.FreeReaderLock(this._RWLockPrefs);
			}
			return stringBuilder.ToString();
		}
		internal string FindMatches(string sFilter)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);
			try
			{
				PreferenceBag.GetReaderLock(this._RWLockPrefs);
				foreach (System.Collections.DictionaryEntry dictionaryEntry in this._dictPrefs)
				{
					if (((string)dictionaryEntry.Key).OICContains(sFilter))
					{
						stringBuilder.AppendFormat("{0}:\t{1}\r\n", dictionaryEntry.Key, dictionaryEntry.Value);
					}
				}
			}
			finally
			{
				PreferenceBag.FreeReaderLock(this._RWLockPrefs);
			}
			return stringBuilder.ToString();
		}
		public PreferenceBag.PrefWatcher AddWatcher(string sPrefixFilter, System.EventHandler<PrefChangeEventArgs> pcehHandler)
		{
			PreferenceBag.PrefWatcher prefWatcher = new PreferenceBag.PrefWatcher(sPrefixFilter.ToLower(), pcehHandler);
			PreferenceBag.GetWriterLock(this._RWLockWatchers);
			try
			{
				this._listWatchers.Add(prefWatcher);
			}
			finally
			{
				PreferenceBag.FreeWriterLock(this._RWLockWatchers);
			}
			return prefWatcher;
		}
		public void RemoveWatcher(PreferenceBag.PrefWatcher wliToRemove)
		{
			PreferenceBag.GetWriterLock(this._RWLockWatchers);
			try
			{
				this._listWatchers.Remove(wliToRemove);
			}
			finally
			{
				PreferenceBag.FreeWriterLock(this._RWLockWatchers);
			}
		}
		private void _NotifyThreadExecute(object objThreadState)
		{
			PrefChangeEventArgs prefChangeEventArgs = (PrefChangeEventArgs)objThreadState;
			string prefName = prefChangeEventArgs.PrefName;
			System.Collections.Generic.List<System.EventHandler<PrefChangeEventArgs>> list = null;
			try
			{
				PreferenceBag.GetReaderLock(this._RWLockWatchers);
				try
				{
					foreach (PreferenceBag.PrefWatcher current in this._listWatchers)
					{
						if (prefName.OICStartsWith(current.sPrefixToWatch))
						{
							if (list == null)
							{
								list = new System.Collections.Generic.List<System.EventHandler<PrefChangeEventArgs>>();
							}
							list.Add(current.fnToNotify);
						}
					}
				}
				finally
				{
					PreferenceBag.FreeReaderLock(this._RWLockWatchers);
				}
				if (list != null)
				{
					foreach (System.EventHandler<PrefChangeEventArgs> current2 in list)
					{
						try
						{
							current2(this, prefChangeEventArgs);
						}
						catch (System.Exception eX)
						{
							OpenWebApplication.ReportException(eX);
						}
					}
				}
			}
			catch (System.Exception eX2)
			{
				OpenWebApplication.ReportException(eX2);
			}
		}
		private void AsyncNotifyWatchers(PrefChangeEventArgs oNotifyArgs)
		{
			System.Threading.ThreadPool.UnsafeQueueUserWorkItem(new System.Threading.WaitCallback(this._NotifyThreadExecute), oNotifyArgs);
		}
	}
}
