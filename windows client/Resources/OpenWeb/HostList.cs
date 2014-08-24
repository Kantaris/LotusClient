using System;
using System.Collections.Generic;
using System.Text;
namespace OpenWeb
{
	public class HostList
	{
		private class HostPortTuple
		{
			public int _iPort;
			public string _sHostname;
			public bool _bTailMatch;
			internal HostPortTuple(string sHostname, int iPort)
			{
				this._iPort = iPort;
				if (sHostname.StartsWith("*"))
				{
					this._bTailMatch = true;
					this._sHostname = sHostname.Substring(1);
				}
				else
				{
					this._sHostname = sHostname;
				}
			}
		}
		private HashSet<string> slSimpleHosts = new HashSet<string>();
		private System.Collections.Generic.List<HostList.HostPortTuple> hplComplexRules = new System.Collections.Generic.List<HostList.HostPortTuple>();
		private bool bEverythingMatches;
		private bool bNonPlainHostnameMatches;
		private bool bPlainHostnameMatches;
		private bool bLoopbackMatches;
		public HostList()
		{
		}
		public HostList(string sInitialList) : this()
		{
			if (!string.IsNullOrEmpty(sInitialList))
			{
				this.AssignFromString(sInitialList);
			}
		}
		public void Clear()
		{
			this.bLoopbackMatches = (this.bPlainHostnameMatches = (this.bNonPlainHostnameMatches = (this.bEverythingMatches = false)));
			this.slSimpleHosts.Clear();
			this.hplComplexRules.Clear();
		}
		public bool AssignFromString(string sIn)
		{
			string text;
			return this.AssignFromString(sIn, out text);
		}
		public bool AssignFromString(string sIn, out string sErrors)
		{
			sErrors = string.Empty;
			this.Clear();
			bool result;
			if (sIn == null)
			{
				result = true;
			}
			else
			{
				sIn = sIn.Trim();
				if (sIn.Length < 1)
				{
					result = true;
				}
				else
				{
					string[] array = sIn.ToLower().Split(new char[]
					{
						',',
						';',
						'\t',
						' ',
						'\r',
						'\n'
					}, System.StringSplitOptions.RemoveEmptyEntries);
					string[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						string text = array2[i];
						if (text.Equals("*"))
						{
							this.bEverythingMatches = true;
						}
						else
						{
							if (text.StartsWith("<"))
							{
								if (text.Equals("<loopback>"))
								{
									this.bLoopbackMatches = true;
									goto IL_1DB;
								}
								if (text.Equals("<local>"))
								{
									this.bPlainHostnameMatches = true;
									goto IL_1DB;
								}
								if (text.Equals("<nonlocal>"))
								{
									this.bNonPlainHostnameMatches = true;
									goto IL_1DB;
								}
							}
							if (text.Length >= 1)
							{
								if (text.Contains("?"))
								{
									sErrors += string.Format("Ignored invalid rule '{0}'-- ? may not appear.\n", text);
								}
								else
								{
									if (text.LastIndexOf("*") > 0)
									{
										sErrors += string.Format("Ignored invalid rule '{0}'-- * may only appear once, at the front of the string.\n", text);
									}
									else
									{
										int num = -1;
										string text2;
										Utilities.CrackHostAndPort(text, out text2, ref num);
										if (-1 == num && !text2.StartsWith("*"))
										{
											this.slSimpleHosts.Add(text);
										}
										else
										{
											HostList.HostPortTuple item = new HostList.HostPortTuple(text2, num);
											this.hplComplexRules.Add(item);
										}
									}
								}
							}
						}
						IL_1DB:;
					}
					if (this.bNonPlainHostnameMatches && this.bPlainHostnameMatches)
					{
						this.bEverythingMatches = true;
					}
					result = string.IsNullOrEmpty(sErrors);
				}
			}
			return result;
		}
		public override string ToString()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			if (this.bEverythingMatches)
			{
				stringBuilder.Append("*; ");
			}
			if (this.bPlainHostnameMatches)
			{
				stringBuilder.Append("<local>; ");
			}
			if (this.bNonPlainHostnameMatches)
			{
				stringBuilder.Append("<nonlocal>; ");
			}
			if (this.bLoopbackMatches)
			{
				stringBuilder.Append("<loopback>; ");
			}
			foreach (string current in this.slSimpleHosts)
			{
				stringBuilder.Append(current);
				stringBuilder.Append("; ");
			}
			foreach (HostList.HostPortTuple current2 in this.hplComplexRules)
			{
				if (current2._bTailMatch)
				{
					stringBuilder.Append("*");
				}
				stringBuilder.Append(current2._sHostname);
				if (current2._iPort > -1)
				{
					stringBuilder.Append(":");
					stringBuilder.Append(current2._iPort.ToString());
				}
				stringBuilder.Append("; ");
			}
			if (stringBuilder.Length > 1)
			{
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
			}
			return stringBuilder.ToString();
		}
		public bool ContainsHost(string sHost)
		{
			int iPort = -1;
			string sHostname;
			Utilities.CrackHostAndPort(sHost, out sHostname, ref iPort);
			return this.ContainsHost(sHostname, iPort);
		}
		public bool ContainsHostname(string sHostname)
		{
			return this.ContainsHost(sHostname, -1);
		}
		public bool ContainsHost(string sHostname, int iPort)
		{
			bool result;
			if (this.bEverythingMatches)
			{
				result = true;
			}
			else
			{
				if (this.bPlainHostnameMatches || this.bNonPlainHostnameMatches)
				{
					bool flag = Utilities.isPlainHostName(sHostname);
					if (this.bPlainHostnameMatches && flag)
					{
						result = true;
						return result;
					}
					if (this.bNonPlainHostnameMatches && !flag)
					{
						result = true;
						return result;
					}
				}
				if (this.bLoopbackMatches && Utilities.isLocalhostname(sHostname))
				{
					result = true;
				}
				else
				{
					sHostname = sHostname.ToLower();
					if (this.slSimpleHosts.Contains(sHostname))
					{
						result = true;
					}
					else
					{
						foreach (HostList.HostPortTuple current in this.hplComplexRules)
						{
							if (iPort == current._iPort || -1 == current._iPort)
							{
								if (current._bTailMatch && sHostname.EndsWith(current._sHostname))
								{
									bool flag2 = true;
									result = flag2;
									return result;
								}
								if (current._sHostname == sHostname)
								{
									bool flag2 = true;
									result = flag2;
									return result;
								}
							}
						}
						result = false;
					}
				}
			}
			return result;
		}
	}
}
