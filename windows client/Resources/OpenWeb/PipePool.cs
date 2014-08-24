using System;
using System.Collections.Generic;
using System.Text;
namespace OpenWeb
{
	internal class PipePool
	{
		internal static uint MSEC_PIPE_POOLED_LIFETIME = 120000u;
		internal static uint MSEC_POOL_CLEANUP_INTERVAL = 30000u;
		private readonly System.Collections.Generic.Dictionary<string, Stack<ServerPipe>> thePool;
		private long lngLastPoolPurge;
		internal PipePool()
		{
			PipePool.MSEC_PIPE_POOLED_LIFETIME = (uint)OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.reuse", 120000);
			this.thePool = new System.Collections.Generic.Dictionary<string, Stack<ServerPipe>>();
			OpenWebApplication.Janitor.assignWork(new SimpleEventHandler(this.ScavengeCache), PipePool.MSEC_POOL_CLEANUP_INTERVAL);
		}
		internal void ScavengeCache()
		{
			if (this.thePool.Count >= 1)
			{
				System.Collections.Generic.List<ServerPipe> list = new System.Collections.Generic.List<ServerPipe>();
				lock (this.thePool)
				{
					System.Collections.Generic.List<string> list2 = new System.Collections.Generic.List<string>();
					ulong num = Utilities.GetTickCount() - (ulong)PipePool.MSEC_PIPE_POOLED_LIFETIME;
					foreach (System.Collections.Generic.KeyValuePair<string, Stack<ServerPipe>> current in this.thePool)
					{
						Stack<ServerPipe> value = current.Value;
						lock (value)
						{
							if (value.Count > 0)
							{
								ServerPipe serverPipe = value.Peek();
								if (serverPipe.ulLastPooled < num)
								{
									list.AddRange(value);
									value.Clear();
								}
								else
								{
									if (value.Count > 1)
									{
										ServerPipe[] array = value.ToArray();
										if (array[array.Length - 1].ulLastPooled < num)
										{
											value.Clear();
											for (int i = array.Length - 1; i >= 0; i--)
											{
												if (array[i].ulLastPooled < num)
												{
													list.Add(array[i]);
												}
												else
												{
													value.Push(array[i]);
												}
											}
										}
									}
								}
							}
							if (value.Count == 0)
							{
								list2.Add(current.Key);
							}
						}
					}
					foreach (string current2 in list2)
					{
						this.thePool.Remove(current2);
					}
				}
				foreach (BasePipe current3 in list)
				{
					current3.End();
				}
			}
		}
		internal void Clear()
		{
			this.lngLastPoolPurge = System.DateTime.Now.Ticks;
			if (this.thePool.Count >= 1)
			{
				System.Collections.Generic.List<ServerPipe> list = new System.Collections.Generic.List<ServerPipe>();
				lock (this.thePool)
				{
					foreach (System.Collections.Generic.KeyValuePair<string, Stack<ServerPipe>> current in this.thePool)
					{
						lock (current.Value)
						{
							list.AddRange(current.Value);
						}
					}
					this.thePool.Clear();
				}
				foreach (ServerPipe current2 in list)
				{
					current2.End();
				}
			}
		}
		internal string InspectPool()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(8192);
			stringBuilder.AppendFormat("ServerPipePool\nOpenWeb.network.timeouts.serverpipe.reuse: {0}ms\nContents\n--------\n", PipePool.MSEC_PIPE_POOLED_LIFETIME);
			lock (this.thePool)
			{
				foreach (string current in this.thePool.Keys)
				{
					Stack<ServerPipe> stack = this.thePool[current];
					stringBuilder.AppendFormat("\t[{0}] entries for '{1}'.\n", stack.Count, current);
					lock (stack)
					{
						foreach (ServerPipe current2 in stack)
						{
							stringBuilder.AppendFormat("\t\t{0}\n", current2.ToString());
						}
					}
				}
			}
			stringBuilder.Append("\n--------\n");
			return stringBuilder.ToString();
		}
		internal ServerPipe TakePipe(string sPoolKey, int iPID, int HackiForSession)
		{
			ServerPipe result;
			if (!CONFIG.bReuseServerSockets)
			{
				result = null;
			}
			else
			{
				Stack<ServerPipe> stack;
				lock (this.thePool)
				{
					if ((iPID == 0 || !this.thePool.TryGetValue(string.Format("PID{0}*{1}", iPID, sPoolKey), out stack) || stack.Count < 1) && (!this.thePool.TryGetValue(sPoolKey, out stack) || stack.Count < 1))
					{
						ServerPipe serverPipe = null;
						result = serverPipe;
						return result;
					}
				}
				ServerPipe serverPipe2;
				lock (stack)
				{
					try
					{
						if (stack.Count == 0)
						{
							ServerPipe serverPipe = null;
							result = serverPipe;
							return result;
						}
						serverPipe2 = stack.Pop();
					}
					catch (System.Exception eX)
					{
						OpenWebApplication.ReportException(eX);
						ServerPipe serverPipe = null;
						result = serverPipe;
						return result;
					}
				}
				result = serverPipe2;
			}
			return result;
		}
		internal void PoolOrClosePipe(ServerPipe oPipe)
		{
			if (!CONFIG.bReuseServerSockets)
			{
				oPipe.End();
			}
			else
			{
				if (oPipe.ReusePolicy == PipeReusePolicy.NoReuse || oPipe.ReusePolicy == PipeReusePolicy.MarriedToClientPipe)
				{
					oPipe.End();
				}
				else
				{
					if (this.lngLastPoolPurge > oPipe.dtConnected.Ticks)
					{
						oPipe.End();
					}
					else
					{
						if (oPipe.sPoolKey == null || oPipe.sPoolKey.Length < 2)
						{
							oPipe.End();
						}
						else
						{
							oPipe.ulLastPooled = Utilities.GetTickCount();
							Stack<ServerPipe> stack;
							lock (this.thePool)
							{
								if (!this.thePool.TryGetValue(oPipe.sPoolKey, out stack))
								{
									stack = new Stack<ServerPipe>();
									this.thePool.Add(oPipe.sPoolKey, stack);
								}
							}
							lock (stack)
							{
								stack.Push(oPipe);
							}
						}
					}
				}
			}
		}
	}
}
