using System;
using System.Runtime.InteropServices;
namespace OpenWeb
{
	internal static class Winsock
	{
		private enum TcpTableType
		{
			BasicListener,
			BasicConnections,
			BasicAll,
			OwnerPidListener,
			OwnerPidConnections,
			OwnerPidAll,
			OwnerModuleListener,
			OwnerModuleConnections,
			OwnerModuleAll
		}
		private const int AF_INET = 2;
		private const int AF_INET6 = 23;
		private const int ERROR_INSUFFICIENT_BUFFER = 122;
		private const int NO_ERROR = 0;
		[System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true, SetLastError = true)]
		private static extern uint GetExtendedTcpTable(System.IntPtr pTcpTable, ref uint dwTcpTableLength, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool sort, uint ipVersion, Winsock.TcpTableType tcpTableType, uint reserved);
		internal static int MapLocalPortToProcessId(int iPort)
		{
			return Winsock.FindPIDForPort(iPort);
		}
		private static int FindPIDForConnection(int iTargetPort, uint iAddressType)
		{
			System.IntPtr intPtr = System.IntPtr.Zero;
			uint num = 32768u;
			int result;
			try
			{
				intPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(32768);
				uint extendedTcpTable = Winsock.GetExtendedTcpTable(intPtr, ref num, false, iAddressType, Winsock.TcpTableType.OwnerPidConnections, 0u);
				while (122u == extendedTcpTable)
				{
					System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
					num += 2048u;
					intPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal((int)num);
					extendedTcpTable = Winsock.GetExtendedTcpTable(intPtr, ref num, false, iAddressType, Winsock.TcpTableType.OwnerPidConnections, 0u);
				}
				if (extendedTcpTable != 0u)
				{
					int num2 = 0;
					result = num2;
					return result;
				}
				int num3;
				int ofs;
				int num4;
				if (iAddressType == 2u)
				{
					num3 = 12;
					ofs = 12;
					num4 = 24;
				}
				else
				{
					num3 = 24;
					ofs = 32;
					num4 = 56;
				}
				int num5 = ((iTargetPort & 255) << 8) + ((iTargetPort & 65280) >> 8);
				int num6 = System.Runtime.InteropServices.Marshal.ReadInt32(intPtr);
				if (num6 == 0)
				{
					int num2 = 0;
					result = num2;
					return result;
				}
				System.IntPtr intPtr2 = (System.IntPtr)((long)intPtr + (long)num3);
				for (int i = 0; i < num6; i++)
				{
					if (num5 == System.Runtime.InteropServices.Marshal.ReadInt32(intPtr2))
					{
						int num2 = System.Runtime.InteropServices.Marshal.ReadInt32(intPtr2, ofs);
						result = num2;
						return result;
					}
					intPtr2 = (System.IntPtr)((long)intPtr2 + (long)num4);
				}
			}
			finally
			{
				System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
			}
			result = 0;
			return result;
		}
		private static int FindPIDForPort(int iTargetPort)
		{
			int result;
			try
			{
				int num = Winsock.FindPIDForConnection(iTargetPort, 2u);
				int num2;
				if (num > 0 || !CONFIG.bEnableIPv6)
				{
					num2 = num;
					result = num2;
					return result;
				}
				num2 = Winsock.FindPIDForConnection(iTargetPort, 23u);
				result = num2;
				return result;
			}
			catch (System.Exception var_2_30)
			{
			}
			result = 0;
			return result;
		}
	}
}
