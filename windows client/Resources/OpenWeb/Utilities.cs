using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace OpenWeb
{
	public static class Utilities
	{
		[System.Flags]
		internal enum SoundFlags : uint
		{
			SND_SYNC = 0u,
			SND_ASYNC = 1u,
			SND_NODEFAULT = 2u,
			SND_MEMORY = 4u,
			SND_LOOP = 8u,
			SND_NOSTOP = 16u,
			SND_NOWAIT = 8192u,
			SND_ALIAS = 65536u,
			SND_ALIAS_ID = 1114112u,
			SND_FILENAME = 131072u,
			SND_RESOURCE = 262148u
		}
		internal struct COPYDATASTRUCT
		{
			public System.IntPtr dwData;
			public int cbData;
			public System.IntPtr lpData;
		}
		[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct SendDataStruct
		{
			public System.IntPtr dwData;
			public int cbData;
			public string strData;
		}
		internal const int WM_HOTKEY = 786;
		internal const int WM_COPYDATA = 74;
		internal const int WM_SIZE = 5;
		internal const int WM_SHOWWINDOW = 24;
		internal const int WM_QUERYENDSESSION = 17;
		public const string sCommonRequestHeaders = "Cache-Control,If-None-Match,If-Modified-Since,Pragma,If-Unmodified-Since,If-Range,If-Match,Content-Length,Content-Type,Referer,Origin,Expect,Content-Encoding,TE,Transfer-Encoding,Proxy-Connection,Connection,Accept,Accept-Charset,Accept-Encoding,Accept-Language,User-Agent,UA-Color,UA-CPU,UA-OS,UA-Pixels,Cookie,Cookie2,DNT,Authorization,Proxy-Authorization";
		public const string sCommonResponseHeaders = "Age,Cache-control,Date,Expires,Pragma,Vary,Content-Length,ETag,Last-Modified,Content-Type,Content-Disposition,Content-Encoding,Transfer-encoding,Via,Keep-Alive,Location,Proxy-Connection,Connection,Set-Cookie,WWW-Authenticate,Proxy-Authenticate,P3P,X-UA-Compatible,X-Frame-options,X-Content-Type-Options,X-XSS-Protection,Strict-Transport-Security,X-Content-Security-Policy,Access-Control-Allow-Origin";
		public static readonly byte[] emptyByteArray = new byte[0];
		private static System.Text.Encoding[] sniffableEncodings = new System.Text.Encoding[]
		{
			System.Text.Encoding.UTF32,
			System.Text.Encoding.BigEndianUnicode,
			System.Text.Encoding.Unicode,
			System.Text.Encoding.UTF8
		};
		public static T EnsureInRange<T>(T current, T min, T max)
		{
			T result;
			if (System.Collections.Generic.Comparer<T>.Default.Compare(current, min) < 0)
			{
				result = min;
			}
			else
			{
				if (System.Collections.Generic.Comparer<T>.Default.Compare(current, max) > 0)
				{
					result = max;
				}
				else
				{
					result = current;
				}
			}
			return result;
		}
		public static string ObtainSaveFilename(string sDialogTitle, string sFilter)
		{
			return Utilities.ObtainSaveFilename(sDialogTitle, sFilter, null);
		}
		public static string UNSTABLE_DescribeClientHello(System.IO.MemoryStream msHello)
		{
			HTTPSClientHello hTTPSClientHello = new HTTPSClientHello();
			string result;
			if (hTTPSClientHello.LoadFromStream(msHello))
			{
				result = hTTPSClientHello.ToString();
			}
			else
			{
				result = string.Empty;
			}
			return result;
		}
		public static string UNSTABLE_DescribeServerHello(System.IO.MemoryStream msHello)
		{
			HTTPSServerHello hTTPSServerHello = new HTTPSServerHello();
			string result;
			if (hTTPSServerHello.LoadFromStream(msHello))
			{
				result = hTTPSServerHello.ToString();
			}
			else
			{
				result = string.Empty;
			}
			return result;
		}
		public static string ObtainSaveFilename(string sDialogTitle, string sFilter, string sInitialDirectory)
		{
			FileDialog fileDialog = new SaveFileDialog();
			fileDialog.Title = sDialogTitle;
			fileDialog.Filter = sFilter;
			if (!string.IsNullOrEmpty(sInitialDirectory))
			{
				fileDialog.InitialDirectory = sInitialDirectory;
				fileDialog.RestoreDirectory = true;
			}
			fileDialog.CustomPlaces.Add(CONFIG.GetPath("Captures"));
			string result = null;
			if (DialogResult.OK == fileDialog.ShowDialog())
			{
				result = fileDialog.FileName;
			}
			fileDialog.Dispose();
			return result;
		}
		public static string ObtainOpenFilename(string sDialogTitle, string sFilter)
		{
			return Utilities.ObtainOpenFilename(sDialogTitle, sFilter, null);
		}
		public static string[] ObtainFilenames(string sDialogTitle, string sFilter, string sInitialDirectory, bool bAllowMultiple)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = sDialogTitle;
			openFileDialog.Multiselect = bAllowMultiple;
			openFileDialog.Filter = sFilter;
			if (!string.IsNullOrEmpty(sInitialDirectory))
			{
				openFileDialog.InitialDirectory = sInitialDirectory;
				openFileDialog.RestoreDirectory = true;
			}
			openFileDialog.CustomPlaces.Add(CONFIG.GetPath("Captures"));
			string[] result = null;
			if (DialogResult.OK == openFileDialog.ShowDialog())
			{
				result = openFileDialog.FileNames;
			}
			openFileDialog.Dispose();
			return result;
		}
		public static string ObtainOpenFilename(string sDialogTitle, string sFilter, string sInitialDirectory)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = sDialogTitle;
			openFileDialog.Filter = sFilter;
			if (!string.IsNullOrEmpty(sInitialDirectory))
			{
				openFileDialog.InitialDirectory = sInitialDirectory;
				openFileDialog.RestoreDirectory = true;
			}
			openFileDialog.CustomPlaces.Add(CONFIG.GetPath("Captures"));
			string result = null;
			if (DialogResult.OK == openFileDialog.ShowDialog())
			{
				result = openFileDialog.FileName;
			}
			openFileDialog.Dispose();
			return result;
		}
		internal static bool OpenWebMeetsVersionRequirement(System.Reflection.Assembly assemblyInput, string sWhatType)
		{
			bool result;
			if (!assemblyInput.IsDefined(typeof(RequiredVersionAttribute), false))
			{
				result = false;
			}
			else
			{
				RequiredVersionAttribute requiredVersionAttribute = (RequiredVersionAttribute)System.Attribute.GetCustomAttribute(assemblyInput, typeof(RequiredVersionAttribute));
				int num = Utilities.CompareVersions(requiredVersionAttribute.RequiredVersion, CONFIG.OpenWebVersionInfo);
				if (num > 0)
				{
					OpenWebApplication.DoNotifyUser(string.Format("The {0} in {1} require OpenWeb v{2} or later. (You have v{3})\n\nPlease install the latest version of OpenWeb from http://getOpenWeb.com.\n\nCode: {4}", new object[]
					{
						sWhatType,
						assemblyInput.CodeBase,
						requiredVersionAttribute.RequiredVersion,
						CONFIG.OpenWebVersionInfo,
						num
					}), "Extension Not Loaded");
					result = false;
				}
				else
				{
					result = true;
				}
			}
			return result;
		}
		public static int CompareVersions(string sRequiredVersion, System.Version verTest)
		{
			string[] array = sRequiredVersion.Split(new char[]
			{
				'.'
			});
			int result;
			if (array.Length != 4)
			{
				result = 5;
			}
			else
			{
				VersionStruct versionStruct = new VersionStruct();
				if (!int.TryParse(array[0], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out versionStruct.Major) || !int.TryParse(array[1], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out versionStruct.Minor) || !int.TryParse(array[2], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out versionStruct.Build) || !int.TryParse(array[3], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out versionStruct.Private))
				{
					result = 6;
				}
				else
				{
					if (versionStruct.Major > verTest.Major)
					{
						result = 4;
					}
					else
					{
						if (verTest.Major > versionStruct.Major)
						{
							result = -4;
						}
						else
						{
							if (versionStruct.Minor > verTest.Minor)
							{
								result = 3;
							}
							else
							{
								if (verTest.Minor > versionStruct.Minor)
								{
									result = -3;
								}
								else
								{
									if (versionStruct.Build > verTest.Build)
									{
										result = 2;
									}
									else
									{
										if (verTest.Build > versionStruct.Build)
										{
											result = -2;
										}
										else
										{
											if (versionStruct.Private > verTest.Revision)
											{
												result = 1;
											}
											else
											{
												if (verTest.Revision > versionStruct.Private)
												{
													result = -1;
												}
												else
												{
													result = 0;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		internal static extern System.IntPtr GlobalFree(System.IntPtr hMem);
		internal static void GlobalFreeIfNonZero(System.IntPtr hMem)
		{
			if (System.IntPtr.Zero != hMem)
			{
				Utilities.GlobalFree(hMem);
			}
		}
		[System.Runtime.InteropServices.DllImport("winmm.dll", SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool PlaySound(string pszSound, System.IntPtr hMod, Utilities.SoundFlags sf);
		internal static void PlaySoundFile(string sFilename)
		{
			Utilities.PlaySound(sFilename, System.IntPtr.Zero, Utilities.SoundFlags.SND_ASYNC | Utilities.SoundFlags.SND_NODEFAULT | Utilities.SoundFlags.SND_FILENAME);
		}
		internal static void PlayNamedSound(string sSoundName)
		{
			Utilities.PlaySound(sSoundName, System.IntPtr.Zero, Utilities.SoundFlags.SND_ASYNC | Utilities.SoundFlags.SND_ALIAS);
		}
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		internal static extern bool RegisterHotKey(System.IntPtr hWnd, int id, int fsModifiers, int vlc);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		internal static extern bool UnregisterHotKey(System.IntPtr hWnd, int id);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		internal static extern System.IntPtr SendMessage(System.IntPtr hWnd, uint msg, System.IntPtr wParam, System.IntPtr lParam);
		[System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool PathUnExpandEnvStrings(string pszPath, [System.Runtime.InteropServices.Out] System.Text.StringBuilder pszBuf, int cchBuf);
		[System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		private static extern bool PathCompactPathEx(System.Text.StringBuilder pszOut, string pszSrc, uint cchMax, uint dwFlags);
		public static string CompactPath(string sPath, int iCharLen)
		{
			string result;
			if (string.IsNullOrEmpty(sPath))
			{
				result = string.Empty;
			}
			else
			{
				if (sPath.Length <= iCharLen)
				{
					result = sPath;
				}
				else
				{
					System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(iCharLen + 1);
					if (Utilities.PathCompactPathEx(stringBuilder, sPath, (uint)(iCharLen + 1), 0u))
					{
						result = stringBuilder.ToString();
					}
					else
					{
						result = sPath;
					}
				}
			}
			return result;
		}
		[CodeDescription("Convert a full path into one that uses environment variables, e.g. %SYSTEM%")]
		public static string CollapsePath(string sPath)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(259);
			string result;
			if (Utilities.PathUnExpandEnvStrings(sPath, stringBuilder, stringBuilder.Capacity))
			{
				result = stringBuilder.ToString();
			}
			else
			{
				result = sPath;
			}
			return result;
		}
		public static string EnsureValidAsPath(string sTargetFolder)
		{
			string result;
			try
			{
				if (System.IO.Directory.Exists(sTargetFolder))
				{
					result = sTargetFolder;
				}
				else
				{
					string text = System.IO.Path.GetPathRoot(sTargetFolder);
					if (!System.IO.Directory.Exists(text))
					{
						result = sTargetFolder;
					}
					else
					{
						if (text[text.Length - 1] != System.IO.Path.DirectorySeparatorChar)
						{
							text += System.IO.Path.DirectorySeparatorChar;
						}
						sTargetFolder = sTargetFolder.Substring(text.Length);
						string[] array = sTargetFolder.Split(new char[]
						{
							System.IO.Path.DirectorySeparatorChar
						}, System.StringSplitOptions.RemoveEmptyEntries);
						string text2 = text;
						for (int i = 0; i < array.Length; i++)
						{
							if (System.IO.File.Exists(text2 + array[i]))
							{
								int num = 1;
								string arg = array[i];
								do
								{
									array[i] = string.Format("{0}[{1}]", arg, num);
									num++;
								}
								while (System.IO.File.Exists(text2 + array[i]));
								break;
							}
							if (!System.IO.Directory.Exists(text2 + array[i]))
							{
								break;
							}
							text2 = string.Format("{0}{1}{2}{1}", text2, System.IO.Path.DirectorySeparatorChar, array[i]);
						}
						result = string.Format("{0}{1}", text, string.Join(new string(System.IO.Path.DirectorySeparatorChar, 1), array));
					}
				}
			}
			catch (System.Exception)
			{
				result = sTargetFolder;
			}
			return result;
		}
		public static string EnsureUniqueFilename(string sFilename)
		{
			string text = sFilename;
			try
			{
				string directoryName = System.IO.Path.GetDirectoryName(sFilename);
				string text2 = Utilities.EnsureValidAsPath(directoryName);
				if (directoryName != text2)
				{
					text = string.Format("{0}{1}{2}", text2, System.IO.Path.DirectorySeparatorChar, System.IO.Path.GetFileName(sFilename));
				}
				if (Utilities.FileOrFolderExists(text))
				{
					string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(text);
					string extension = System.IO.Path.GetExtension(text);
					int num = 1;
					do
					{
						text = string.Format("{0}{1}{2}[{3}]{4}", new object[]
						{
							directoryName,
							System.IO.Path.DirectorySeparatorChar,
							fileNameWithoutExtension,
							num.ToString(),
							extension
						});
						num++;
					}
					while (Utilities.FileOrFolderExists(text) || num > 16384);
				}
			}
			catch (System.Exception)
			{
			}
			return text;
		}
		internal static bool FileOrFolderExists(string sResult)
		{
			bool result;
			try
			{
				result = (System.IO.File.Exists(sResult) || System.IO.Directory.Exists(sResult));
			}
			catch (System.Exception)
			{
				result = true;
			}
			return result;
		}
		public static void EnsureOverwritable(string sFilename)
		{
			if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(sFilename)))
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sFilename));
			}
			if (System.IO.File.Exists(sFilename))
			{
				System.IO.FileAttributes attributes = System.IO.File.GetAttributes(sFilename);
				System.IO.File.SetAttributes(sFilename, attributes & ~(System.IO.FileAttributes.ReadOnly | System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System));
			}
		}
		[CodeDescription("Writes arrBytes to a file, creating the target directory and overwriting if the file exists.")]
		public static void WriteArrayToFile(string sFilename, byte[] arrBytes)
		{
			if (arrBytes == null)
			{
				arrBytes = Utilities.emptyByteArray;
			}
			Utilities.EnsureOverwritable(sFilename);
			System.IO.File.WriteAllBytes(sFilename, arrBytes);
		}
		[CodeDescription("Reads oStream until arrBytes is filled.")]
		public static int ReadEntireStream(System.IO.Stream oStream, byte[] arrBytes)
		{
			int num = 0;
			while ((long)num < arrBytes.LongLength)
			{
				num += oStream.Read(arrBytes, num, arrBytes.Length - num);
			}
			return num;
		}
		public static byte[] ReadEntireStream(System.IO.Stream oS)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			byte[] array = new byte[32768];
			int count;
			while ((count = oS.Read(array, 0, array.Length)) > 0)
			{
				memoryStream.Write(array, 0, count);
			}
			return memoryStream.ToArray();
		}
		public static byte[] JoinByteArrays(byte[] arr1, byte[] arr2)
		{
			byte[] array = new byte[arr1.Length + arr2.Length];
			System.Buffer.BlockCopy(arr1, 0, array, 0, arr1.Length);
			System.Buffer.BlockCopy(arr2, 0, array, arr1.Length, arr2.Length);
			return array;
		}
		internal static string ConvertCRAndLFToSpaces(string sIn)
		{
			sIn = sIn.Replace("\r\n", " ");
			sIn = sIn.Replace('\r', ' ');
			sIn = sIn.Replace('\n', ' ');
			return sIn;
		}
		public static string GetCommaTokenValue(string sString, string sTokenName)
		{
			string result = null;
			if (sString != null && sString.Length > 0)
			{
				Regex regex = new Regex(sTokenName + "\\s?=?\\s?[\"]?(?<TokenValue>[^\";,]*)", RegexOptions.IgnoreCase);
				Match match = regex.Match(sString);
				if (match.Success && match.Groups["TokenValue"] != null)
				{
					result = match.Groups["TokenValue"].Value;
				}
			}
			return result;
		}
		[CodeDescription("Returns the first iMaxLength or fewer characters from the target string.")]
		public static string TrimTo(string sString, int iMaxLength)
		{
			string result;
			if (string.IsNullOrEmpty(sString))
			{
				result = string.Empty;
			}
			else
			{
				if (iMaxLength >= sString.Length)
				{
					result = sString;
				}
				else
				{
					result = sString.Substring(0, iMaxLength);
				}
			}
			return result;
		}
		public static string EllipsizeIfNeeded(string sString, int iMaxLength)
		{
			string result;
			if (string.IsNullOrEmpty(sString))
			{
				result = string.Empty;
			}
			else
			{
				if (iMaxLength >= sString.Length)
				{
					result = sString;
				}
				else
				{
					result = sString.Substring(0, iMaxLength - 1) + '…';
				}
			}
			return result;
		}
		[CodeDescription("Returns the part of a string up to (but NOT including) the first instance of specified substring. If delim not found, returns entire string.")]
		public static string TrimAfter(string sString, string sDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				if (sDelim == null)
				{
					result = sString;
				}
				else
				{
					int num = sString.IndexOf(sDelim);
					if (num < 0)
					{
						result = sString;
					}
					else
					{
						result = sString.Substring(0, num);
					}
				}
			}
			return result;
		}
		[CodeDescription("Returns the part of a string up to (but NOT including) the first instance of specified delimiter. If delim not found, returns entire string.")]
		public static string TrimAfter(string sString, char chDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				int num = sString.IndexOf(chDelim);
				if (num < 0)
				{
					result = sString;
				}
				else
				{
					result = sString.Substring(0, num);
				}
			}
			return result;
		}
		public static string TrimAfter(string sString, int iMaxLength)
		{
			return Utilities.TrimTo(sString, iMaxLength);
		}
		[CodeDescription("Returns the part of a string after (but NOT including) the first instance of specified delimiter. If delim not found, returns entire string.")]
		public static string TrimBefore(string sString, char chDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				int num = sString.IndexOf(chDelim);
				if (num < 0)
				{
					result = sString;
				}
				else
				{
					result = sString.Substring(num + 1);
				}
			}
			return result;
		}
		[CodeDescription("Returns the part of a string after (but NOT including) the first instance of specified substring. If delim not found, returns entire string.")]
		public static string TrimBefore(string sString, string sDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				if (sDelim == null)
				{
					result = sString;
				}
				else
				{
					int num = sString.IndexOf(sDelim);
					if (num < 0)
					{
						result = sString;
					}
					else
					{
						result = sString.Substring(num + sDelim.Length);
					}
				}
			}
			return result;
		}
		[CodeDescription("Returns the part of a string after (and including) the first instance of specified substring. If delim not found, returns entire string.")]
		public static string TrimUpTo(string sString, string sDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				if (sDelim == null)
				{
					result = sString;
				}
				else
				{
					int num = sString.IndexOf(sDelim);
					if (num < 0)
					{
						result = sString;
					}
					else
					{
						result = sString.Substring(num);
					}
				}
			}
			return result;
		}
		[CodeDescription("Returns the part of a string after (but not including) the last instance of specified delimiter. If delim not found, returns entire string.")]
		public static string TrimBeforeLast(string sString, char chDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				int num = sString.LastIndexOf(chDelim);
				if (num < 0)
				{
					result = sString;
				}
				else
				{
					result = sString.Substring(num + 1);
				}
			}
			return result;
		}
		[CodeDescription("Returns the part of a string after (but not including) the last instance of specified substring. If delim not found, returns entire string.")]
		public static string TrimBeforeLast(string sString, string sDelim)
		{
			string result;
			if (sString == null)
			{
				result = string.Empty;
			}
			else
			{
				if (sDelim == null)
				{
					result = sString;
				}
				else
				{
					int num = sString.LastIndexOf(sDelim);
					if (num < 0)
					{
						result = sString;
					}
					else
					{
						result = sString.Substring(num + sDelim.Length);
					}
				}
			}
			return result;
		}
		[CodeDescription("Returns TRUE if the HTTP Method MUST have a body.")]
		public static bool HTTPMethodRequiresBody(string sMethod)
		{
			return "PROPPATCH" == sMethod || "PATCH" == sMethod;
		}
		public static bool HTTPMethodIsIdempotent(string sMethod)
		{
			return "GET" == sMethod || "HEAD" == sMethod || "OPTIONS" == sMethod || "TRACE" == sMethod || "PUT" == sMethod || "DELETE" == sMethod;
		}
		[CodeDescription("Returns TRUE if the HTTP Method MAY have a body.")]
		public static bool HTTPMethodAllowsBody(string sMethod)
		{
			return "POST" == sMethod || "PUT" == sMethod || "PROPPATCH" == sMethod || "PATCH" == sMethod || "LOCK" == sMethod || "PROPFIND" == sMethod || "SEARCH" == sMethod;
		}
		[CodeDescription("Returns TRUE if a response body is allowed for this responseCode.")]
		public static bool HTTPStatusAllowsBody(int iResponseCode)
		{
			return 204 != iResponseCode && 205 != iResponseCode && 304 != iResponseCode && (iResponseCode <= 99 || iResponseCode >= 200);
		}
		public static bool IsRedirectStatus(int iResponseCode)
		{
			return iResponseCode == 301 || iResponseCode == 302 || iResponseCode == 303 || iResponseCode == 307 || iResponseCode == 308;
		}
		internal static bool HasImageFileExtension(string sExt)
		{
			return sExt.EndsWith(".gif") || sExt.EndsWith(".jpg") || sExt.EndsWith(".jpeg") || sExt.EndsWith(".png") || sExt.EndsWith(".webp") || sExt.EndsWith(".ico");
		}
		public static bool IsBinaryMIME(string sContentType)
		{
			bool result;
			if (string.IsNullOrEmpty(sContentType))
			{
				result = false;
			}
			else
			{
				if (sContentType.OICStartsWith("image/"))
				{
					result = !sContentType.OICStartsWith("image/svg+xml");
				}
				else
				{
					result = (sContentType.OICStartsWith("audio/") || sContentType.OICStartsWith("video/") || (!sContentType.OICStartsWith("text/") && (sContentType.OICContains("msbin1") || sContentType.OICStartsWith("application/octet") || sContentType.OICStartsWith("application/x-shockwave"))));
				}
			}
			return result;
		}
		[CodeDescription("Gets a string from a byte-array, stripping a BOM if present.")]
		public static string GetStringFromArrayRemovingBOM(byte[] arrInput, System.Text.Encoding oDefaultEncoding)
		{
			string result;
			if (arrInput == null)
			{
				result = string.Empty;
			}
			else
			{
				if (arrInput.Length < 2)
				{
					result = oDefaultEncoding.GetString(arrInput);
				}
				else
				{
					System.Text.Encoding[] array = Utilities.sniffableEncodings;
					for (int i = 0; i < array.Length; i++)
					{
						System.Text.Encoding encoding = array[i];
						byte[] preamble = encoding.GetPreamble();
						if (arrInput.Length >= preamble.Length)
						{
							bool flag = preamble.Length > 0;
							for (int j = 0; j < preamble.Length; j++)
							{
								if (preamble[j] != arrInput[j])
								{
									flag = false;
									break;
								}
							}
							if (flag)
							{
								int num = encoding.GetPreamble().Length;
								result = encoding.GetString(arrInput, num, arrInput.Length - num);
								return result;
							}
						}
					}
					result = oDefaultEncoding.GetString(arrInput);
				}
			}
			return result;
		}
		[CodeDescription("Gets (via Headers or Sniff) the provided body's text Encoding. Returns CONFIG.oHeaderEncoding (usually UTF-8) if unknown. Potentially slow.")]
		public static System.Text.Encoding getEntityBodyEncoding(HTTPHeaders oHeaders, byte[] oBody)
		{
			System.Text.Encoding result;
			if (oHeaders != null)
			{
				string tokenValue = oHeaders.GetTokenValue("Content-Type", "charset");
				if (tokenValue != null)
				{
					try
					{
						result = System.Text.Encoding.GetEncoding(tokenValue);
						return result;
					}
					catch (System.Exception)
					{
					}
				}
			}
			System.Text.Encoding encoding = CONFIG.oHeaderEncoding;
			if (oBody == null || oBody.Length < 2)
			{
				result = encoding;
			}
			else
			{
				System.Text.Encoding[] array = Utilities.sniffableEncodings;
				for (int i = 0; i < array.Length; i++)
				{
					System.Text.Encoding encoding2 = array[i];
					byte[] preamble = encoding2.GetPreamble();
					if (oBody.Length >= preamble.Length)
					{
						bool flag = preamble.Length > 0;
						for (int j = 0; j < preamble.Length; j++)
						{
							if (preamble[j] != oBody[j])
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							encoding = encoding2;
							break;
						}
					}
				}
				if (oHeaders != null && oHeaders.Exists("Content-Type"))
				{
					if (oHeaders.ExistsAndContains("Content-Type", "multipart/form-data"))
					{
						string @string = encoding.GetString(oBody, 0, System.Math.Min(8192, oBody.Length));
						Regex regex = new Regex(".*Content-Disposition: form-data; name=\"_charset_\"\\s+(?<thecharset>[^\\s'&>\\\"]*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
						MatchCollection matchCollection = regex.Matches(@string);
						if (matchCollection.Count > 0 && matchCollection[0].Groups.Count > 0)
						{
							try
							{
								string value = matchCollection[0].Groups[1].Value;
								System.Text.Encoding encoding3 = System.Text.Encoding.GetEncoding(value);
								encoding = encoding3;
							}
							catch (System.Exception)
							{
							}
						}
					}
					if (oHeaders.ExistsAndContains("Content-Type", "application/x-www-form-urlencoded"))
					{
						string string2 = encoding.GetString(oBody, 0, System.Math.Min(4096, oBody.Length));
						Regex regex2 = new Regex(".*_charset_=(?<thecharset>[^'&>\\\"]*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
						MatchCollection matchCollection2 = regex2.Matches(string2);
						if (matchCollection2.Count > 0 && matchCollection2[0].Groups.Count > 0)
						{
							try
							{
								string value2 = matchCollection2[0].Groups[1].Value;
								System.Text.Encoding encoding4 = System.Text.Encoding.GetEncoding(value2);
								encoding = encoding4;
							}
							catch (System.Exception)
							{
							}
						}
					}
					if (oHeaders.ExistsAndContains("Content-Type", "html"))
					{
						string string3 = encoding.GetString(oBody, 0, System.Math.Min(4096, oBody.Length));
						Regex regex3 = new Regex("<meta\\s.*charset\\s*=\\s*['\\\"]?(?<thecharset>[^'>\\\"]*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
						MatchCollection matchCollection3 = regex3.Matches(string3);
						if (matchCollection3.Count > 0 && matchCollection3[0].Groups.Count > 0)
						{
							try
							{
								string value3 = matchCollection3[0].Groups[1].Value;
								System.Text.Encoding encoding5 = System.Text.Encoding.GetEncoding(value3);
								if (encoding5 != encoding && (encoding != System.Text.Encoding.UTF8 || (encoding5 != System.Text.Encoding.BigEndianUnicode && encoding5 != System.Text.Encoding.Unicode && encoding5 != System.Text.Encoding.UTF32)) && (encoding5 != System.Text.Encoding.UTF8 || (encoding != System.Text.Encoding.BigEndianUnicode && encoding != System.Text.Encoding.Unicode && encoding != System.Text.Encoding.UTF32)))
								{
									encoding = encoding5;
								}
							}
							catch (System.Exception)
							{
							}
						}
					}
				}
				result = encoding;
			}
			return result;
		}
		[CodeDescription("Gets (via Headers or Sniff) the Response Text Encoding. Returns CONFIG.oHeaderEncoding (usually UTF-8) if unknown. Potentially slow.")]
		public static System.Text.Encoding getResponseBodyEncoding(Session oSession)
		{
			System.Text.Encoding result;
			if (oSession == null)
			{
				result = CONFIG.oHeaderEncoding;
			}
			else
			{
				if (!oSession.bHasResponse)
				{
					result = CONFIG.oHeaderEncoding;
				}
				else
				{
					result = Utilities.getEntityBodyEncoding(oSession.oResponse.headers, oSession.responseBodyBytes);
				}
			}
			return result;
		}
		public static string HtmlEncode(string sInput)
		{
			string result;
			if (sInput == null)
			{
				result = null;
			}
			else
			{
				result = WebUtility.HtmlEncode(sInput);
			}
			return result;
		}
		private static int HexToByte(char h)
		{
			int result;
			if (h >= '0' && h <= '9')
			{
				result = (int)(h - '0');
			}
			else
			{
				if (h >= 'a' && h <= 'f')
				{
					result = (int)(h - 'a' + '\n');
				}
				else
				{
					if (h >= 'A' && h <= 'F')
					{
						result = (int)(h - 'A' + '\n');
					}
					else
					{
						result = -1;
					}
				}
			}
			return result;
		}
		private static bool IsHexDigit(char ch)
		{
			return (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
		}
		private static string GetUTF8HexString(string sInput, ref int iX)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			do
			{
				if (iX > sInput.Length - 2)
				{
					memoryStream.WriteByte(37);
					iX += 2;
				}
				else
				{
					if (Utilities.IsHexDigit(sInput[iX + 1]) && Utilities.IsHexDigit(sInput[iX + 2]))
					{
						byte value = (byte)((Utilities.HexToByte(sInput[iX + 1]) << 4) + Utilities.HexToByte(sInput[iX + 2]));
						memoryStream.WriteByte(value);
						iX += 3;
					}
					else
					{
						memoryStream.WriteByte(37);
						iX++;
					}
				}
			}
			while (iX < sInput.Length && '%' == sInput[iX]);
			iX--;
			return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
		}
		public static string UrlDecode(string sInput)
		{
			string result;
			if (string.IsNullOrEmpty(sInput))
			{
				result = string.Empty;
			}
			else
			{
				if (sInput.IndexOf('%') < 0)
				{
					result = sInput;
				}
				else
				{
					System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(sInput.Length);
					for (int i = 0; i < sInput.Length; i++)
					{
						if ('%' == sInput[i])
						{
							stringBuilder.Append(Utilities.GetUTF8HexString(sInput, ref i));
						}
						else
						{
							stringBuilder.Append(sInput[i]);
						}
					}
					result = stringBuilder.ToString();
				}
			}
			return result;
		}
		private static string UrlEncodeChars(string str, System.Text.Encoding oEnc)
		{
			string result;
			if (string.IsNullOrEmpty(str))
			{
				result = str;
			}
			else
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				for (int i = 0; i < str.Length; i++)
				{
					char c = str[i];
					if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '.' || c == '(' || c == ')' || c == '*' || c == '\'' || c == '_' || c == '!')
					{
						stringBuilder.Append(c);
					}
					else
					{
						if (c == ' ')
						{
							stringBuilder.Append("+");
						}
						else
						{
							byte[] bytes = oEnc.GetBytes(new char[]
							{
								c
							});
							byte[] array = bytes;
							for (int j = 0; j < array.Length; j++)
							{
								byte b = array[j];
								stringBuilder.Append("%");
								stringBuilder.Append(b.ToString("X2"));
							}
						}
					}
				}
				result = stringBuilder.ToString();
			}
			return result;
		}
		public static string UrlEncode(string sInput)
		{
			return Utilities.UrlEncodeChars(sInput, System.Text.Encoding.UTF8);
		}
		public static string UrlEncode(string sInput, System.Text.Encoding oEnc)
		{
			return Utilities.UrlEncodeChars(sInput, oEnc);
		}
		private static string UrlPathEncodeChars(string str)
		{
			string result;
			if (string.IsNullOrEmpty(str))
			{
				result = str;
			}
			else
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				for (int i = 0; i < str.Length; i++)
				{
					char c = str[i];
					if (c > ' ' && c < '\u007f')
					{
						stringBuilder.Append(c);
					}
					else
					{
						if (c < '!')
						{
							stringBuilder.Append("%");
							stringBuilder.Append(((byte)c).ToString("X2"));
						}
						else
						{
							byte[] bytes = System.Text.Encoding.UTF8.GetBytes(new char[]
							{
								c
							});
							byte[] array = bytes;
							for (int j = 0; j < array.Length; j++)
							{
								byte b = array[j];
								stringBuilder.Append("%");
								stringBuilder.Append(b.ToString("X2"));
							}
						}
					}
				}
				result = stringBuilder.ToString();
			}
			return result;
		}
		public static string UrlPathEncode(string str)
		{
			string result;
			if (string.IsNullOrEmpty(str))
			{
				result = str;
			}
			else
			{
				int num = str.IndexOf('?');
				if (num >= 0)
				{
					result = Utilities.UrlPathEncode(str.Substring(0, num)) + str.Substring(num);
				}
				else
				{
					result = Utilities.UrlPathEncodeChars(str);
				}
			}
			return result;
		}
		[CodeDescription("Tokenize a string into tokens. Delimits on whitespace; \" marks are dropped unless preceded by \\ characters.")]
		public static string[] Parameterize(string sInput)
		{
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
			bool flag = false;
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			int i = 0;
			while (i < sInput.Length)
			{
				char c = sInput[i];
				if (c == '\t')
				{
					goto IL_A4;
				}
				switch (c)
				{
				case ' ':
					goto IL_A4;
				case '"':
					if (i > 0 && sInput[i - 1] == '\\')
					{
						stringBuilder.Remove(stringBuilder.Length - 1, 1);
						stringBuilder.Append('"');
						goto IL_9E;
					}
					flag = !flag;
					goto IL_9E;
				}
				stringBuilder.Append(sInput[i]);
				IL_9E:
				i++;
				continue;
				IL_A4:
				if (flag)
				{
					stringBuilder.Append(sInput[i]);
					goto IL_9E;
				}
				if (stringBuilder.Length > 0 || (i > 0 && sInput[i - 1] == '"'))
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Length = 0;
					goto IL_9E;
				}
				goto IL_9E;
			}
			if (stringBuilder.Length > 0)
			{
				list.Add(stringBuilder.ToString());
			}
			return list.ToArray();
		}
		[CodeDescription("Returns a string representing a Hex view of a byte array. Slow.")]
		public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine)
		{
			return Utilities.ByteArrayToHexView(inArr, iBytesPerLine, inArr.Length, true);
		}
		[CodeDescription("Returns a string representing a Hex view of a byte array. PERF: Slow.")]
		public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine, int iMaxByteCount)
		{
			return Utilities.ByteArrayToHexView(inArr, iBytesPerLine, iMaxByteCount, true);
		}
		[CodeDescription("Returns a string representing a Hex view of a byte array. PERF: Slow.")]
		public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine, int iMaxByteCount, bool bShowASCII)
		{
			return Utilities.ByteArrayToHexView(inArr, 0, iBytesPerLine, iMaxByteCount, bShowASCII);
		}
		[CodeDescription("Returns a string representing a Hex view of a byte array. PERF: Slow.")]
		public static string ByteArrayToHexView(byte[] inArr, int iStartAt, int iBytesPerLine, int iMaxByteCount, bool bShowASCII)
		{
			string result;
			if (inArr == null || inArr.Length == 0)
			{
				result = string.Empty;
			}
			else
			{
				if (iBytesPerLine < 1 || iMaxByteCount < 1)
				{
					result = string.Empty;
				}
				else
				{
					iMaxByteCount = System.Math.Min(iMaxByteCount, inArr.Length);
					System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(iMaxByteCount * 5);
					for (int i = iStartAt; i < iMaxByteCount; i += iBytesPerLine)
					{
						int num = System.Math.Min(iBytesPerLine, iMaxByteCount - i);
						bool flag = num < iBytesPerLine;
						for (int j = 0; j < num; j++)
						{
							stringBuilder.Append(inArr[i + j].ToString("X2"));
							stringBuilder.Append(" ");
						}
						if (flag)
						{
							stringBuilder.Append(new string(' ', 3 * (iBytesPerLine - num)));
						}
						if (bShowASCII)
						{
							stringBuilder.Append(" ");
							for (int k = 0; k < num; k++)
							{
								if (inArr[i + k] < 32)
								{
									stringBuilder.Append(".");
								}
								else
								{
									stringBuilder.Append((char)inArr[i + k]);
								}
							}
							if (flag)
							{
								stringBuilder.Append(new string(' ', iBytesPerLine - num));
							}
						}
						stringBuilder.Append("\r\n");
					}
					result = stringBuilder.ToString();
				}
			}
			return result;
		}
		[CodeDescription("Returns a string representing a Hex stream of a byte array. Slow.")]
		public static string ByteArrayToString(byte[] inArr)
		{
			string result;
			if (inArr == null)
			{
				result = "null";
			}
			else
			{
				if (inArr.Length == 0)
				{
					result = "empty";
				}
				else
				{
					result = System.BitConverter.ToString(inArr).Replace('-', ' ');
				}
			}
			return result;
		}
		internal static string StringToCF_HTML(string inStr)
		{
			string text = "<HTML><HEAD><STYLE>.REQUEST { font: 8pt Courier New; color: blue;} .RESPONSE { font: 8pt Courier New; color: green;}</STYLE></HEAD><BODY>" + inStr + "</BODY></HTML>";
			string text2 = "Version:1.0\r\nStartHTML:{0:00000000}\r\nEndHTML:{1:00000000}\r\nStartFragment:{0:00000000}\r\nEndFragment:{1:00000000}\r\n";
			return string.Format(text2, text2.Length - 16, text.Length + text2.Length - 16) + text;
		}
		[CodeDescription("Returns an integer from the registry, or iDefault if the registry key is missing or cannot be used as an integer.")]
		public static int GetRegistryInt(Microsoft.Win32.RegistryKey oReg, string sName, int iDefault)
		{
			int num = iDefault;
			object value = oReg.GetValue(sName);
			int result;
			if (value is int)
			{
				num = (int)value;
			}
			else
			{
				string text = value as string;
				if (text != null && !int.TryParse(text, out num))
				{
					result = iDefault;
					return result;
				}
			}
			result = num;
			return result;
		}
		[CodeDescription("Save a string to the registry. Correctly handles null Value, saving as String.Empty.")]
		public static void SetRegistryString(Microsoft.Win32.RegistryKey oReg, string sName, string sValue)
		{
			if (sName != null)
			{
				if (sValue == null)
				{
					sValue = string.Empty;
				}
				oReg.SetValue(sName, sValue);
			}
		}
		[CodeDescription("Returns an float from the registry, or flDefault if the registry key is missing or cannot be used as an float.")]
		public static float GetRegistryFloat(Microsoft.Win32.RegistryKey oReg, string sName, float flDefault)
		{
			float result = flDefault;
			object value = oReg.GetValue(sName);
			if (value is int)
			{
				result = (float)value;
			}
			else
			{
				string text = value as string;
				if (text != null && !float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result))
				{
					result = flDefault;
				}
			}
			return result;
		}
		[CodeDescription("Returns an bool from the registry, or bDefault if the registry key is missing or cannot be used as an bool.")]
		public static bool GetRegistryBool(Microsoft.Win32.RegistryKey oReg, string sName, bool bDefault)
		{
			bool result = bDefault;
			object value = oReg.GetValue(sName);
			if (value is int)
			{
				result = (1 == (int)value);
			}
			else
			{
				string text = value as string;
				if (text != null)
				{
					result = "true".OICEquals(text);
				}
			}
			return result;
		}
		internal static string FileExtensionForMIMEType(string sMIME)
		{
			string result;
			if (string.IsNullOrEmpty(sMIME) || sMIME.Length > 255)
			{
				result = ".txt";
			}
			else
			{
				sMIME = sMIME.ToLower();
				string text = sMIME;
				switch (text)
				{
				case "text/css":
					result = ".css";
					return result;
				case "text/html":
					result = ".htm";
					return result;
				case "text/javascript":
				case "application/javascript":
				case "application/x-javascript":
					result = ".js";
					return result;
				case "text/cache-manifest":
					result = ".appcache";
					return result;
				case "image/jpg":
				case "image/jpeg":
					result = ".jpg";
					return result;
				case "image/gif":
					result = ".gif";
					return result;
				case "image/png":
					result = ".png";
					return result;
				case "image/x-icon":
					result = ".ico";
					return result;
				case "text/xml":
					result = ".xml";
					return result;
				case "video/x-flv":
					result = ".flv";
					return result;
				case "video/mp4":
					result = ".mp4";
					return result;
				case "text/plain":
				case "application/octet-stream":
					result = ".txt";
					return result;
				}
				try
				{
					Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(string.Format("\\MIME\\Database\\Content Type\\{0}", sMIME), Microsoft.Win32.RegistryKeyPermissionCheck.ReadSubTree);
					if (registryKey != null)
					{
						string text2 = (string)registryKey.GetValue("Extension");
						registryKey.Close();
						if (!string.IsNullOrEmpty(text2))
						{
							string text3 = text2;
							result = text3;
							return result;
						}
					}
				}
				catch
				{
				}
				if (sMIME.EndsWith("+xml"))
				{
					result = ".xml";
				}
				else
				{
					result = ".txt";
				}
			}
			return result;
		}
		internal static string ContentTypeForFilename(string sFilename)
		{
			string sExtension = string.Empty;
			string result;
			try
			{
				sExtension = System.IO.Path.GetExtension(sFilename);
			}
			catch (System.Exception)
			{
				string text = "application/octet-stream";
				result = text;
				return result;
			}
			string text2 = Utilities.ContentTypeForFileExtension(sExtension);
			if (string.IsNullOrEmpty(text2))
			{
				result = "application/octet-stream";
			}
			else
			{
				result = text2;
			}
			return result;
		}
		internal static string ContentTypeForFileExtension(string sExtension)
		{
			string result;
			if (string.IsNullOrEmpty(sExtension) || sExtension.Length > 255)
			{
				result = null;
			}
			else
			{
				if (sExtension == ".js")
				{
					result = "text/javascript";
				}
				else
				{
					if (sExtension == ".json")
					{
						result = "application/json";
					}
					else
					{
						if (sExtension == ".css")
						{
							result = "text/css";
						}
						else
						{
							if (sExtension == ".htm")
							{
								result = "text/html";
							}
							else
							{
								if (sExtension == ".html")
								{
									result = "text/html";
								}
								else
								{
									if (sExtension == ".appcache")
									{
										result = "text/cache-manifest";
									}
									else
									{
										if (sExtension == ".flv")
										{
											result = "video/x-flv";
										}
										else
										{
											string text = null;
											try
											{
												Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(sExtension, Microsoft.Win32.RegistryKeyPermissionCheck.ReadSubTree);
												if (registryKey != null)
												{
													text = (string)registryKey.GetValue("Content Type");
													if (string.IsNullOrEmpty(text))
													{
														string text2 = (string)registryKey.GetValue("");
														if (!string.IsNullOrEmpty(text2))
														{
															Microsoft.Win32.RegistryKey registryKey2 = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(text2, Microsoft.Win32.RegistryKeyPermissionCheck.ReadSubTree);
															if (registryKey2 != null)
															{
																text = (string)registryKey2.GetValue("Content Type");
																registryKey2.Close();
															}
														}
													}
													registryKey.Close();
												}
											}
											catch (System.Security.SecurityException)
											{
											}
											catch (System.Exception eX)
											{
												OpenWebApplication.ReportException(eX, "Registry Failure");
											}
											result = text;
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
		internal static bool IsChunkedBodyComplete(Session m_session, byte[] oRawBuffer, long iStartAtOffset, long iEndAtOffset, out long outStartOfLatestChunk, out long outEndOfEntity)
		{
			int num = (int)iStartAtOffset;
			outStartOfLatestChunk = (long)num;
			outEndOfEntity = -1L;
			bool result;
			while ((long)num < iEndAtOffset)
			{
				outStartOfLatestChunk = (long)num;
				string text = System.Text.Encoding.ASCII.GetString(oRawBuffer, num, System.Math.Min(64, (int)(iEndAtOffset - (long)num)));
				int num2 = text.IndexOf("\r\n", System.StringComparison.Ordinal);
				if (num2 <= -1)
				{
					result = false;
				}
				else
				{
					num += num2 + 2;
					text = text.Substring(0, num2);
					text = Utilities.TrimAfter(text, ';');
					int num3 = 0;
					if (!Utilities.TryHexParse(text, out num3))
					{
						if (m_session != null)
						{
							SessionFlags flagViolation = (m_session.state <= SessionStates.ReadingRequest) ? SessionFlags.ProtocolViolationInRequest : SessionFlags.ProtocolViolationInResponse;
							OpenWebApplication.HandleHTTPError(m_session, flagViolation, true, true, "Illegal chunked encoding. '" + text + "' is not a hexadecimal number.");
						}
						result = true;
					}
					else
					{
						if (num3 != 0)
						{
							num += num3 + 2;
							continue;
						}
						bool flag = true;
						bool flag2 = false;
						if ((long)(num + 2) > iEndAtOffset)
						{
							result = false;
						}
						else
						{
							int num4 = (int)oRawBuffer[num++];
							while ((long)num <= iEndAtOffset)
							{
								int num5 = num4;
								if (num5 != 10)
								{
									if (num5 == 13)
									{
										flag2 = true;
									}
									else
									{
										flag2 = false;
										flag = false;
									}
								}
								else
								{
									if (flag2)
									{
										if (flag)
										{
											outEndOfEntity = (long)num;
											result = true;
											return result;
										}
										flag = true;
										flag2 = false;
									}
									else
									{
										flag2 = false;
										flag = false;
									}
								}
								num4 = (int)oRawBuffer[num++];
							}
							result = false;
						}
					}
				}
				return result;
			}
			result = false;
			return result;
		}
		internal static bool IsChunkedBodyComplete(Session m_session, System.IO.MemoryStream oData, long iStartAtOffset, out long outStartOfLatestChunk, out long outEndOfEntity)
		{
			return Utilities.IsChunkedBodyComplete(m_session, oData.GetBuffer(), iStartAtOffset, oData.Length, out outStartOfLatestChunk, out outEndOfEntity);
		}
		private static void _WriteChunkSizeToStream(System.IO.MemoryStream oMS, int iLen)
		{
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(iLen.ToString("x"));
			oMS.Write(bytes, 0, bytes.Length);
		}
		private static void _WriteCRLFToStream(System.IO.MemoryStream oMS)
		{
			oMS.WriteByte(13);
			oMS.WriteByte(10);
		}
		public static byte[] doChunk(byte[] writeData, int iSuggestedChunkCount)
		{
			byte[] result;
			if (writeData == null || writeData.Length < 1)
			{
				result = System.Text.Encoding.ASCII.GetBytes("0\r\n\r\n");
			}
			else
			{
				if (iSuggestedChunkCount < 1)
				{
					iSuggestedChunkCount = 1;
				}
				if (iSuggestedChunkCount > writeData.Length)
				{
					iSuggestedChunkCount = writeData.Length;
				}
				System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(writeData.Length + 10 * iSuggestedChunkCount);
				int num = 0;
				do
				{
					int num2 = writeData.Length - num;
					int num3 = num2 / iSuggestedChunkCount;
					num3 = System.Math.Max(1, num3);
					num3 = System.Math.Min(num2, num3);
					Utilities._WriteChunkSizeToStream(memoryStream, num3);
					Utilities._WriteCRLFToStream(memoryStream);
					memoryStream.Write(writeData, num, num3);
					Utilities._WriteCRLFToStream(memoryStream);
					num += num3;
					iSuggestedChunkCount--;
					if (iSuggestedChunkCount < 1)
					{
						iSuggestedChunkCount = 1;
					}
				}
				while (num < writeData.Length);
				Utilities._WriteChunkSizeToStream(memoryStream, 0);
				Utilities._WriteCRLFToStream(memoryStream);
				Utilities._WriteCRLFToStream(memoryStream);
				result = memoryStream.ToArray();
			}
			return result;
		}
		public static byte[] doUnchunk(byte[] writeData)
		{
			byte[] result;
			if (writeData == null || writeData.Length == 0)
			{
				result = Utilities.emptyByteArray;
			}
			else
			{
				System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(writeData.Length);
				int num = 0;
				bool flag = false;
				while (!flag && num <= writeData.Length - 3)
				{
					string text = System.Text.Encoding.ASCII.GetString(writeData, num, System.Math.Min(64, writeData.Length - num));
					int num2 = text.IndexOf("\r\n", System.StringComparison.Ordinal);
					if (num2 <= 0)
					{
						throw new InvalidDataException("HTTP Error: The chunked content is corrupt. Cannot find Chunk-Length in expected location. Offset: " + num.ToString());
					}
					num += num2 + 2;
					text = text.Substring(0, num2);
					text = Utilities.TrimAfter(text, ';');
					int num3;
					if (!Utilities.TryHexParse(text, out num3))
					{
						throw new InvalidDataException("HTTP Error: The chunked content is corrupt. Chunk Length was malformed. Offset: " + num.ToString());
					}
					if (num3 == 0)
					{
						flag = true;
					}
					else
					{
						if (writeData.Length < num3 + num)
						{
							throw new InvalidDataException("HTTP Error: The chunked entity body is corrupt. The final chunk length is greater than the number of bytes remaining.");
						}
						memoryStream.Write(writeData, num, num3);
						num += num3 + 2;
					}
				}
				byte[] array = new byte[memoryStream.Length];
				System.Buffer.BlockCopy(memoryStream.GetBuffer(), 0, array, 0, array.Length);
				result = array;
			}
			return result;
		}
		internal static bool arrayContainsNonText(byte[] arrIn)
		{
			bool result;
			if (arrIn == null)
			{
				result = false;
			}
			else
			{
				for (int i = 0; i < arrIn.Length; i++)
				{
					if (arrIn[i] == 0)
					{
						result = true;
						return result;
					}
				}
				result = false;
			}
			return result;
		}
		public static bool isUnsupportedEncoding(string sTE, string sCE)
		{
			return (!string.IsNullOrEmpty(sTE) && sTE.OICContains("xpress")) || (!string.IsNullOrEmpty(sCE) && sCE.OICContains("xpress"));
		}
		private static void _DecodeInOrder(string sEncodingsInOrder, bool bAllowChunks, ref byte[] arrBody)
		{
			if (!string.IsNullOrEmpty(sEncodingsInOrder))
			{
				string[] array = sEncodingsInOrder.ToLower().Split(new char[]
				{
					','
				});
				int i = array.Length - 1;
				while (i >= 0)
				{
					string text = array[i].Trim();
					string a;
					if ((a = text) != null)
					{
						if (!(a == "gzip"))
						{
							if (!(a == "deflate"))
							{
								if (!(a == "bzip2"))
								{
									if (a == "chunked")
									{
										if (bAllowChunks)
										{
											if (i != array.Length - 1)
											{
											}
											arrBody = Utilities.doUnchunk(arrBody);
										}
									}
								}
								else
								{
									arrBody = Utilities.bzip2Expand(arrBody, true);
								}
							}
							else
							{
								arrBody = Utilities.DeflaterExpand(arrBody, true);
							}
						}
						else
						{
							arrBody = Utilities.GzipExpand(arrBody, true);
						}
					}
					IL_FF:
					i--;
					continue;
					goto IL_FF;
				}
			}
		}
		public static void utilDecodeHTTPBody(HTTPHeaders oHeaders, ref byte[] arrBody)
		{
			if (!Utilities.IsNullOrEmpty(arrBody))
			{
				Utilities._DecodeInOrder(oHeaders["Transfer-Encoding"], true, ref arrBody);
				Utilities._DecodeInOrder(oHeaders["Content-Encoding"], false, ref arrBody);
			}
		}
		public static byte[] ZLibExpand(byte[] compressedData)
		{
			if (compressedData == null || compressedData.Length == 0)
			{
				return Utilities.emptyByteArray;
			}
			throw new System.NotSupportedException("This application was compiled without ZLib support.");
		}
		[CodeDescription("Returns a byte[] containing a gzip-compressed copy of writeData[]")]
		public static byte[] GzipCompress(byte[] writeData)
		{
			byte[] result;
			try
			{
				System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
				using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
				{
					gZipStream.Write(writeData, 0, writeData.Length);
				}
				result = memoryStream.ToArray();
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser("The content could not be compressed.\n\n" + ex.Message, "OpenWeb: GZip failed");
				result = writeData;
			}
			return result;
		}
		public static byte[] GzipExpandInternal(bool bUseXceed, byte[] compressedData)
		{
			byte[] result;
			if (compressedData == null || compressedData.Length == 0)
			{
				result = Utilities.emptyByteArray;
			}
			else
			{
				System.IO.MemoryStream stream = new System.IO.MemoryStream(compressedData);
				System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(compressedData.Length);
				if (bUseXceed)
				{
					throw new System.NotSupportedException("This application was compiled without Xceed support.");
				}
				using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
				{
					byte[] array = new byte[32768];
					int count;
					while ((count = gZipStream.Read(array, 0, array.Length)) > 0)
					{
						memoryStream.Write(array, 0, count);
					}
				}
				result = memoryStream.ToArray();
			}
			return result;
		}
		[CodeDescription("Returns a byte[] containing an un-gzipped copy of compressedData[]")]
		public static byte[] GzipExpand(byte[] compressedData)
		{
			return Utilities.GzipExpand(compressedData, false);
		}
		public static byte[] GzipExpand(byte[] compressedData, bool bThrowErrors)
		{
			byte[] result;
			try
			{
				result = Utilities.GzipExpandInternal(CONFIG.bUseXceedDecompressForGZIP, compressedData);
			}
			catch (System.Exception ex)
			{
				if (bThrowErrors)
				{
					throw new InvalidDataException("The content could not be ungzipped", ex);
				}
				OpenWebApplication.DoNotifyUser("The content could not be decompressed.\n\n" + ex.Message, "OpenWeb: UnGZip failed");
				result = Utilities.emptyByteArray;
			}
			return result;
		}
		[CodeDescription("Returns a byte[] containing a DEFLATE'd copy of writeData[]")]
		public static byte[] DeflaterCompress(byte[] writeData)
		{
			byte[] result;
			if (writeData == null || writeData.Length == 0)
			{
				result = Utilities.emptyByteArray;
			}
			else
			{
				byte[] array;
				try
				{
					System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
					using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
					{
						deflateStream.Write(writeData, 0, writeData.Length);
					}
					array = memoryStream.ToArray();
				}
				catch (System.Exception ex)
				{
					OpenWebApplication.DoNotifyUser("The content could not be compressed.\n\n" + ex.Message, "OpenWeb: Deflation failed");
					array = writeData;
				}
				result = array;
			}
			return result;
		}
		public static byte[] DeflaterExpandInternal(bool bUseXceed, byte[] compressedData)
		{
			byte[] result;
			if (compressedData == null || compressedData.Length == 0)
			{
				result = Utilities.emptyByteArray;
			}
			else
			{
				int num = 0;
				if (compressedData.Length > 2 && compressedData[0] == 120 && compressedData[1] == 156)
				{
					num = 2;
				}
				if (bUseXceed)
				{
					throw new System.NotSupportedException("This application was compiled without Xceed support.");
				}
				System.IO.MemoryStream stream = new System.IO.MemoryStream(compressedData, num, compressedData.Length - num);
				System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(compressedData.Length);
				using (DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
				{
					byte[] array = new byte[32768];
					int count;
					while ((count = deflateStream.Read(array, 0, array.Length)) > 0)
					{
						memoryStream.Write(array, 0, count);
					}
				}
				result = memoryStream.ToArray();
			}
			return result;
		}
		[CodeDescription("Returns a byte[] representing the INFLATE'd representation of compressedData[]")]
		public static byte[] DeflaterExpand(byte[] compressedData)
		{
			return Utilities.DeflaterExpand(compressedData, false);
		}
		public static byte[] DeflaterExpand(byte[] compressedData, bool bThrowErrors)
		{
			byte[] result;
			try
			{
				result = Utilities.DeflaterExpandInternal(CONFIG.bUseXceedDecompressForDeflate, compressedData);
			}
			catch (System.Exception ex)
			{
				if (bThrowErrors)
				{
					throw new InvalidDataException("The content could not be inFlated", ex);
				}
				OpenWebApplication.DoNotifyUser("The content could not be decompressed.\n\n" + ex.Message, "OpenWeb: Inflation failed");
				result = Utilities.emptyByteArray;
			}
			return result;
		}
		[CodeDescription("Returns a byte[] representing the bzip2'd representation of writeData[]")]
		public static byte[] bzip2Compress(byte[] writeData)
		{
			if (writeData == null || writeData.Length == 0)
			{
				return Utilities.emptyByteArray;
			}
			throw new System.NotSupportedException("This application was compiled without BZIP2 support.");
		}
		public static byte[] bzip2Expand(byte[] compressedData)
		{
			return Utilities.bzip2Expand(compressedData, false);
		}
		public static byte[] bzip2Expand(byte[] compressedData, bool bThrowErrors)
		{
			if (compressedData == null || compressedData.Length == 0)
			{
				return Utilities.emptyByteArray;
			}
			throw new System.NotSupportedException("This application was compiled without BZIP2 support.");
		}
		[CodeDescription("Try parsing the string for a Hex-formatted int. If it fails, return false and 0 in iOutput.")]
		public static bool TryHexParse(string sInput, out int iOutput)
		{
			return int.TryParse(sInput, System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out iOutput);
		}
		public static bool areOriginsEquivalent(string sOrigin1, string sOrigin2, int iDefaultPort)
		{
			bool result;
			if (string.Equals(sOrigin1, sOrigin2, System.StringComparison.OrdinalIgnoreCase))
			{
				result = true;
			}
			else
			{
				int num = iDefaultPort;
				string arg;
				Utilities.CrackHostAndPort(sOrigin1, out arg, ref num);
				string inStr = string.Format("{0}:{1}", arg, num);
				num = iDefaultPort;
				Utilities.CrackHostAndPort(sOrigin2, out arg, ref num);
				string toMatch = string.Format("{0}:{1}", arg, num);
				result = inStr.OICEquals(toMatch);
			}
			return result;
		}
		[CodeDescription("Returns false if Hostname contains any dots or colons.")]
		public static bool isPlainHostName(string sHostAndPort)
		{
			int num = 0;
			string text;
			Utilities.CrackHostAndPort(sHostAndPort, out text, ref num);
			char[] anyOf = new char[]
			{
				'.',
				':'
			};
			return text.IndexOfAny(anyOf) < 0;
		}
		[CodeDescription("Returns true if True if the sHostAndPort's host is 127.0.0.1, 'localhost', or ::1. Note that list is not complete.")]
		public static bool isLocalhost(string sHostAndPort)
		{
			int num = 0;
			string sHostname;
			Utilities.CrackHostAndPort(sHostAndPort, out sHostname, ref num);
			return Utilities.isLocalhostname(sHostname);
		}
		[CodeDescription("Returns true if True if the sHostname is 127.0.0.1, 'localhost', or ::1. Note that list is not complete.")]
		public static bool isLocalhostname(string sHostname)
		{
			return "localhost".OICEquals(sHostname) || "127.0.0.1".Equals(sHostname) || "localhost.".OICEquals(sHostname) || "::1".Equals(sHostname);
		}
		[CodeDescription("This function cracks the Host/Port combo, removing IPV6 brackets if needed.")]
		public static void CrackHostAndPort(string sHostPort, out string sHostname, ref int iPort)
		{
			int num = sHostPort.LastIndexOf(':');
			if (num > -1 && num > sHostPort.LastIndexOf(']'))
			{
				if (!int.TryParse(sHostPort.Substring(num + 1), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out iPort))
				{
					iPort = -1;
				}
				sHostname = sHostPort.Substring(0, num);
			}
			else
			{
				sHostname = sHostPort;
			}
			if (sHostname.StartsWith("[", System.StringComparison.Ordinal) && sHostname.EndsWith("]", System.StringComparison.Ordinal))
			{
				sHostname = sHostname.Substring(1, sHostname.Length - 2);
			}
		}
		public static IPEndPoint IPEndPointFromHostPortString(string sHostAndPort)
		{
			IPEndPoint result;
			if (Utilities.IsNullOrWhiteSpace(sHostAndPort))
			{
				result = null;
			}
			else
			{
				sHostAndPort = Utilities.TrimAfter(sHostAndPort, ';');
				IPEndPoint iPEndPoint2;
				try
				{
					int port = 80;
					string sRemoteHost;
					Utilities.CrackHostAndPort(sHostAndPort, out sRemoteHost, ref port);
					IPAddress iPAddress = DNSResolver.GetIPAddress(sRemoteHost, true);
					IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, port);
					iPEndPoint2 = iPEndPoint;
				}
				catch (System.Exception)
				{
					iPEndPoint2 = null;
				}
				result = iPEndPoint2;
			}
			return result;
		}
		public static IPEndPoint[] IPEndPointListFromHostPortString(string sAllHostAndPorts)
		{
			IPEndPoint[] result;
			if (Utilities.IsNullOrWhiteSpace(sAllHostAndPorts))
			{
				result = null;
			}
			else
			{
				string[] array = sAllHostAndPorts.Split(new char[]
				{
					';'
				}, System.StringSplitOptions.RemoveEmptyEntries);
				System.Collections.Generic.List<IPEndPoint> list = new System.Collections.Generic.List<IPEndPoint>();
				string[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					string sHostPort = array2[i];
					try
					{
						int port = 80;
						string sRemoteHost;
						Utilities.CrackHostAndPort(sHostPort, out sRemoteHost, ref port);
						IPAddress[] iPAddressList = DNSResolver.GetIPAddressList(sRemoteHost, true, null);
						IPAddress[] array3 = iPAddressList;
						for (int j = 0; j < array3.Length; j++)
						{
							IPAddress address = array3[j];
							list.Add(new IPEndPoint(address, port));
						}
					}
					catch (System.Exception)
					{
					}
				}
				if (list.Count < 1)
				{
					result = null;
				}
				else
				{
					result = list.ToArray();
				}
			}
			return result;
		}
		[CodeDescription("This function attempts to be a ~fast~ way to return an IP from a hoststring that contains an IP-Literal. ")]
		public static IPAddress IPFromString(string sHost)
		{
			IPAddress result;
			for (int i = 0; i < sHost.Length; i++)
			{
				if (sHost[i] != '.' && sHost[i] != ':' && (sHost[i] < '0' || sHost[i] > '9') && (sHost[i] < 'A' || sHost[i] > 'F') && (sHost[i] < 'a' || sHost[i] > 'f'))
				{
					result = null;
					return result;
				}
			}
			if (sHost.EndsWith("."))
			{
				sHost = Utilities.TrimBeforeLast(sHost, '.');
			}
			IPAddress iPAddress;
			try
			{
				iPAddress = IPAddress.Parse(sHost);
			}
			catch
			{
				iPAddress = null;
			}
			result = iPAddress;
			return result;
		}
		[CodeDescription("ShellExecutes the sURL.")]
		public static bool LaunchHyperlink(string sURL)
		{
			bool result;
			try
			{
				using (Process.Start(sURL))
				{
				}
				result = true;
				return result;
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser("Your web browser is not correctly configured to launch hyperlinks.\n\nTo see this content, visit:\n\t" + sURL + "\n...in your web browser.\n\nError: " + ex.Message, "Error");
			}
			result = false;
			return result;
		}
		internal static bool LaunchBrowser(string sExe, string sParams, string sURL)
		{
			if (!string.IsNullOrEmpty(sParams))
			{
				sParams = sParams.Replace("%U", sURL);
			}
			else
			{
				sParams = sURL;
			}
			return Utilities.RunExecutable(sExe, sParams);
		}
		public static bool RunExecutable(string sExecute, string sParams)
		{
			bool result;
			try
			{
				using (Process.Start(sExecute, sParams))
				{
				}
				result = true;
				return result;
			}
			catch (System.Exception ex)
			{
				if (!(ex is Win32Exception) || 1223 != (ex as Win32Exception).NativeErrorCode)
				{
					OpenWebApplication.DoNotifyUser(string.Format("Failed to execute: {0}\r\n{1}\r\n\r\n{2}\r\n{3}", new object[]
					{
						sExecute,
						string.IsNullOrEmpty(sParams) ? string.Empty : ("with parameters: " + sParams),
						ex.Message,
						ex.StackTrace.ToString()
					}), "ShellExecute Failed");
				}
			}
			result = false;
			return result;
		}
		[CodeDescription("Run an executable and wait for it to exit.")]
		public static bool RunExecutableAndWait(string sExecute, string sParams)
		{
			bool result;
			try
			{
				Process process = new Process();
				process.StartInfo.FileName = sExecute;
				process.StartInfo.Arguments = sParams;
				process.Start();
				process.WaitForExit();
				process.Dispose();
				result = true;
			}
			catch (System.Exception ex)
			{
				if (!(ex is Win32Exception) || 1223 != (ex as Win32Exception).NativeErrorCode)
				{
					OpenWebApplication.DoNotifyUser("OpenWeb Exception thrown: " + ex.ToString() + "\r\n" + ex.StackTrace.ToString(), "ShellExecute Failed");
				}
				result = false;
			}
			return result;
		}
		[CodeDescription("Run an executable, wait for it to exit, and return its output as a string.")]
		public static string GetExecutableOutput(string sExecute, string sParams, out int iExitCode)
		{
			iExitCode = -999;
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			stringBuilder.Append(string.Concat(new string[]
			{
				"Results from ",
				sExecute,
				" ",
				sParams,
				"\r\n\r\n"
			}));
			try
			{
				Process process = new Process();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.FileName = sExecute;
				process.StartInfo.Arguments = sParams;
				process.Start();
				string text;
				while ((text = process.StandardOutput.ReadLine()) != null)
				{
					text = text.TrimEnd(new char[0]);
					if (text.Length > 0)
					{
						stringBuilder.AppendLine(text);
					}
				}
				iExitCode = process.ExitCode;
				process.Dispose();
			}
			catch (System.Exception ex)
			{
				stringBuilder.Append("Exception thrown: " + ex.ToString() + "\r\n" + ex.StackTrace.ToString());
			}
			stringBuilder.Append("-------------------------------------------\r\n");
			return stringBuilder.ToString();
		}
		[CodeDescription("Copy a string to the clipboard, with exception handling.")]
		public static bool CopyToClipboard(string sText)
		{
			DataObject dataObject = new DataObject();
			dataObject.SetData(DataFormats.Text, sText);
			return Utilities.CopyToClipboard(dataObject);
		}
		public static bool CopyToClipboard(DataObject oData)
		{
			bool result;
			try
			{
				Clipboard.SetDataObject(oData, true);
				result = true;
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser("Please disable any clipboard monitoring tools and try again.\n\n" + ex.Message, ".NET Framework Bug");
				result = true;
			}
			return result;
		}
		internal static string RegExEscape(string sString, bool bAddPrefixCaret, bool bAddSuffixDollarSign)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			if (bAddPrefixCaret)
			{
				stringBuilder.Append("^");
			}
			int i = 0;
			while (i < sString.Length)
			{
				char c = sString[i];
				char c2 = c;
				if (c2 <= '?')
				{
					switch (c2)
					{
					case '#':
					case '$':
					case '(':
					case ')':
					case '+':
					case '.':
						goto IL_EF;
					case '%':
					case '&':
					case '\'':
					case ',':
					case '-':
						break;
					case '*':
						stringBuilder.Append('.');
						break;
					default:
						if (c2 == '?')
						{
							goto IL_EF;
						}
						break;
					}
				}
				else
				{
					switch (c2)
					{
					case '[':
					case '\\':
					case '^':
						goto IL_EF;
					case ']':
						break;
					default:
						switch (c2)
						{
						case '{':
						case '|':
							goto IL_EF;
						}
						break;
					}
				}
				IL_E1:
				stringBuilder.Append(c);
				i++;
				continue;
				IL_EF:
				stringBuilder.Append('\\');
				goto IL_E1;
			}
			if (bAddSuffixDollarSign)
			{
				stringBuilder.Append('$');
			}
			return stringBuilder.ToString();
		}
		public static bool HasMagicBytes(byte[] arrData, byte[] arrMagics)
		{
			bool result;
			if (arrData == null)
			{
				result = false;
			}
			else
			{
				if (arrData.Length < arrMagics.Length)
				{
					result = false;
				}
				else
				{
					for (int i = 0; i < arrMagics.Length; i++)
					{
						if (arrData[i] != arrMagics[i])
						{
							result = false;
							return result;
						}
					}
					result = true;
				}
			}
			return result;
		}
		public static bool HasMagicBytes(byte[] arrData, string sMagics)
		{
			return Utilities.HasMagicBytes(arrData, System.Text.Encoding.ASCII.GetBytes(sMagics));
		}
		internal static bool isRPCOverHTTPSMethod(string sMethod)
		{
			return sMethod == "RPC_IN_DATA" || sMethod == "RPC_OUT_DATA";
		}
		internal static bool isHTTP200Array(byte[] arrData)
		{
			return arrData.Length > 12 && arrData[0] == 72 && arrData[1] == 84 && arrData[2] == 84 && arrData[3] == 80 && arrData[4] == 47 && arrData[5] == 49 && arrData[6] == 46 && arrData[9] == 50 && arrData[10] == 48 && arrData[11] == 48;
		}
		internal static bool isHTTP407Array(byte[] arrData)
		{
			return arrData.Length > 12 && arrData[0] == 72 && arrData[1] == 84 && arrData[2] == 84 && arrData[3] == 80 && arrData[4] == 47 && arrData[5] == 49 && arrData[6] == 46 && arrData[9] == 52 && arrData[10] == 48 && arrData[11] == 55;
		}
		public static bool IsBrowserProcessName(string sProcessName)
		{
			return !string.IsNullOrEmpty(sProcessName) && sProcessName.OICStartsWithAny(new string[]
			{
				"ie",
				"chrom",
				"firefox",
				"tbb-",
				"opera",
				"webkit",
				"safari"
			});
		}
		public static string EnsurePathIsAbsolute(string sRootPath, string sFilename)
		{
			try
			{
				if (!System.IO.Path.IsPathRooted(sFilename))
				{
					sFilename = sRootPath + sFilename;
				}
			}
			catch (System.Exception)
			{
			}
			return sFilename;
		}
		internal static string GetFirstLocalResponse(string sFilename)
		{
			sFilename = Utilities.TrimAfter(sFilename, '?');
			try
			{
				if (!System.IO.Path.IsPathRooted(sFilename))
				{
					string str = sFilename;
					sFilename = CONFIG.GetPath("TemplateResponses") + str;
					if (!System.IO.File.Exists(sFilename))
					{
						sFilename = CONFIG.GetPath("Responses") + str;
					}
				}
			}
			catch (System.Exception)
			{
			}
			return sFilename;
		}
		internal static string DescribeException(System.Exception eX)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(512);
			stringBuilder.Append(eX.Message);
			if (eX.InnerException != null)
			{
				stringBuilder.AppendFormat(" < {0}", eX.InnerException.Message);
			}
			return stringBuilder.ToString();
		}
		internal static string DescribeExceptionWithStack(System.Exception eX)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(512);
			stringBuilder.AppendLine(eX.Message);
			stringBuilder.AppendLine(eX.StackTrace);
			if (eX.InnerException != null)
			{
				stringBuilder.AppendFormat(" < {0}", eX.InnerException.Message);
			}
			return stringBuilder.ToString();
		}
		[System.Runtime.InteropServices.DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern ulong GetTickCount64();
		public static ulong GetTickCount()
		{
			ulong result;
			if (System.Environment.OSVersion.Version.Major > 5)
			{
				result = Utilities.GetTickCount64();
			}
			else
			{
				int tickCount = System.Environment.TickCount;
				if (tickCount > 0)
				{
					result = (ulong)((long)tickCount);
				}
				else
				{
					result = (ulong)(2L +(ulong)((long)tickCount)); //todo
				}
			}
			return result;
		}
		[System.Runtime.InteropServices.DllImport("shell32.dll")]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		public static extern bool IsUserAnAdmin();
		internal static object GetOSVerString()
		{
			return System.Environment.OSVersion.VersionString.Replace("Microsoft Windows ", "Win").Replace("Service Pack ", "SP");
		}
		internal static bool IsNullOrWhiteSpace(string sInput)
		{
			return string.IsNullOrWhiteSpace(sInput);
		}
		internal static SslProtocols ParseSSLProtocolString(string sList)
		{
			SslProtocols sslProtocols = SslProtocols.None;
			if (sList.OICContains("ssl2"))
			{
				sslProtocols |= SslProtocols.Ssl2;
			}
			if (sList.OICContains("ssl3"))
			{
				sslProtocols |= SslProtocols.Ssl3;
			}
			if (sList.OICContains("tls1.0"))
			{
				sslProtocols |= SslProtocols.Tls;
			}
			return sslProtocols;
		}
		public static byte[] Dupe(byte[] bIn)
		{
			byte[] result;
			if (bIn == null)
			{
				result = Utilities.emptyByteArray;
			}
			else
			{
				byte[] array = new byte[bIn.Length];
				System.Buffer.BlockCopy(bIn, 0, array, 0, bIn.Length);
				result = array;
			}
			return result;
		}
		public static bool IsNullOrEmpty(byte[] bIn)
		{
			return bIn == null || bIn.Length == 0;
		}
		internal static bool HasHeaders(ServerChatter oSC)
		{
			return oSC != null && null != oSC.headers;
		}
		internal static bool HasHeaders(ClientChatter oCC)
		{
			return oCC != null && null != oCC.headers;
		}
		internal static string GetLocalIPList(bool bLeadingTab)
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(string.Empty);
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			IPAddress[] array = hostAddresses;
			for (int i = 0; i < array.Length; i++)
			{
				IPAddress iPAddress = array[i];
				stringBuilder.AppendFormat("{0}{1}\n", bLeadingTab ? "\t" : string.Empty, iPAddress.ToString());
			}
			return stringBuilder.ToString();
		}
		internal static string GetNetworkInfo()
		{
			string result;
			try
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				long num = 0L;
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				System.Array.Sort<NetworkInterface>(allNetworkInterfaces, (NetworkInterface x, NetworkInterface y) => string.Compare(y.OperationalStatus.ToString(), x.OperationalStatus.ToString()));
				NetworkInterface[] array = allNetworkInterfaces;
				for (int i = 0; i < array.Length; i++)
				{
					NetworkInterface networkInterface = array[i];
					stringBuilder.AppendFormat("{0,32}\t '{1}' Type: {2} @ {3:N0}/sec. Status: {4}\n", new object[]
					{
						networkInterface.Name,
						networkInterface.Description,
						networkInterface.NetworkInterfaceType,
						networkInterface.Speed,
						networkInterface.OperationalStatus.ToString().ToUpperInvariant()
					});
					if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Unknown && !networkInterface.IsReceiveOnly)
					{
						num += networkInterface.GetIPv4Statistics().BytesReceived;
					}
				}
				stringBuilder.AppendFormat("\nTotal bytes received (IPv4): {0:N0}\n", num);
				stringBuilder.AppendFormat("\nLocal Addresses:\n{0}", Utilities.GetLocalIPList(true));
				result = stringBuilder.ToString();
			}
			catch (System.Exception eX)
			{
				result = "Failed to obtain NetworkInterfaces information. " + Utilities.DescribeException(eX);
			}
			return result;
		}
		internal static void PingTarget(string sTarget)
		{
			Ping ping = new Ping();
			ping.PingCompleted += delegate(object oS, PingCompletedEventArgs pcea)
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				if (pcea.Reply == null)
				{
					stringBuilder.AppendFormat("Pinging '{0}' failed: {1}\n", pcea.UserState, pcea.Error.InnerException.ToString());
				}
				else
				{
					stringBuilder.AppendFormat("Pinged '{0}'.\n\tFinal Result:\t{1}\n", pcea.UserState as string, pcea.Reply.Status.ToString());
					if (pcea.Reply.Status == IPStatus.Success)
					{
						stringBuilder.AppendFormat("\tTarget Address:\t{0}\n", pcea.Reply.Address);
						stringBuilder.AppendFormat("\tRoundTrip time:\t{0}", pcea.Reply.RoundtripTime);
					}
				}
			};
			ping.SendAsync(sTarget, 60000, new byte[0], new PingOptions(128, true), sTarget);
		}
		internal static bool IsNotExtension(string sFilename)
		{
			return sFilename.StartsWith("_") || sFilename.OICStartsWithAny(new string[]
			{
				"qwhale.",
				"Be.Windows.Forms.",
				"Telerik.WinControls."
			});
		}
		internal static void CompactLOHIfPossible()
		{
			try
			{
				System.Type typeFromHandle = typeof(System.Runtime.GCSettings);
				System.Reflection.PropertyInfo property = typeFromHandle.GetProperty("LargeObjectHeapCompactionMode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
				if (null != property)
				{
					System.Reflection.MethodInfo setMethod = property.GetSetMethod();
					setMethod.Invoke(null, new object[]
					{
						2
					});
					System.GC.Collect();
				}
			}
			catch (System.Exception eX)
			{
				MessageBox.Show(Utilities.DescribeException(eX));
			}
		}
		[CodeDescription("Save the specified .SAZ session archive")]
		public static bool WriteSessionArchive(string sFilename, Session[] arrSessions, string sPassword, bool bVerboseDialogs)
		{
			bool result;
			if (arrSessions == null || arrSessions.Length < 1)
			{
				if (bVerboseDialogs)
				{
					OpenWebApplication.DoNotifyUser("No sessions were provided to save to the archive.", "WriteSessionArchive - No Input");
				}
				result = false;
			}
			else
			{
				if (OpenWebApplication.oSAZProvider == null)
				{
					throw new System.NotSupportedException("This application was compiled without .SAZ support.");
				}
				bool flag;
				try
				{
					if (System.IO.File.Exists(sFilename))
					{
						System.IO.File.Delete(sFilename);
					}
					ISAZWriter iSAZWriter = OpenWebApplication.oSAZProvider.CreateSAZ(sFilename);
					if (!string.IsNullOrEmpty(sPassword))
					{
						iSAZWriter.SetPassword(sPassword);
					}
					iSAZWriter.Comment = "OpenWeb (v" + Application.ProductVersion + ") Session Archive. See http://OpenWeb2.com";
					int num = 1;
					string sFileNumberFormat = "D" + arrSessions.Length.ToString().Length;
					for (int i = 0; i < arrSessions.Length; i++)
					{
						Session oSession = arrSessions[i];
						Utilities.WriteSessionToSAZ(oSession, iSAZWriter, num, sFileNumberFormat, null, bVerboseDialogs);
						num++;
					}
					iSAZWriter.CompleteArchive();
					flag = true;
				}
				catch (System.Exception ex)
				{
					if (bVerboseDialogs)
					{
						OpenWebApplication.DoNotifyUser("Failed to save Session Archive.\n\n" + ex.Message, "Save Failed");
					}
					flag = false;
				}
				result = flag;
			}
			return result;
		}
		internal static void WriteSessionToSAZ(Session oSession, ISAZWriter oISW, int iFileNumber, string sFileNumberFormat, System.Text.StringBuilder sbHTML, bool bVerboseDialogs)
		{
			string text = "raw\\" + iFileNumber.ToString(sFileNumberFormat);
			string text2 = text + "_c.txt";
			string text3 = text + "_s.txt";
			string text4 = text + "_m.xml";
			try
			{
				oISW.AddFile(text2, delegate(System.IO.Stream oS)
				{
					oSession.WriteRequestToStream(false, true, oS);
				});
			}
			catch (System.Exception eX)
			{
				if (bVerboseDialogs)
				{
					OpenWebApplication.DoNotifyUser("Unable to add " + text2 + "\n\n" + Utilities.DescribeExceptionWithStack(eX), "Archive Failure");
				}
			}
			try
			{
				oISW.AddFile(text3, delegate(System.IO.Stream oS)
				{
					oSession.WriteResponseToStream(oS, false);
				});
			}
			catch (System.Exception eX2)
			{
				if (bVerboseDialogs)
				{
					OpenWebApplication.DoNotifyUser("Unable to add " + text3 + "\n\n" + Utilities.DescribeExceptionWithStack(eX2), "Archive Failure");
				}
			}
			try
			{
				oISW.AddFile(text4, delegate(System.IO.Stream oS)
				{
					oSession.WriteMetadataToStream(oS);
				});
			}
			catch (System.Exception eX3)
			{
				if (bVerboseDialogs)
				{
					OpenWebApplication.DoNotifyUser("Unable to add " + text4 + "\n\n" + Utilities.DescribeExceptionWithStack(eX3), "Archive Failure");
				}
			}
			if (oSession.bHasWebSocketMessages)
			{
				string text5 = text + "_w.txt";
				try
				{
					oISW.AddFile(text5, delegate(System.IO.Stream oS)
					{
						oSession.WriteWebSocketMessagesToStream(oS);
					});
				}
				catch (System.Exception eX4)
				{
					if (bVerboseDialogs)
					{
						OpenWebApplication.DoNotifyUser("Unable to add " + text5 + "\n\n" + Utilities.DescribeExceptionWithStack(eX4), "Archive Failure");
					}
				}
			}
			if (sbHTML != null)
			{
				sbHTML.Append("<tr>");
				sbHTML.AppendFormat("<TD><a href='{0}'>C</a>&nbsp;", text2);
				sbHTML.AppendFormat("<a href='{0}'>S</a>&nbsp;", text3);
				sbHTML.AppendFormat("<a href='{0}'>M</a>", text4);
				if (oSession.bHasWebSocketMessages)
				{
					sbHTML.AppendFormat("&nbsp;<a href='{0}_w.txt'>W</a>", text);
				}
				sbHTML.AppendFormat("</TD>", new object[0]);
				sbHTML.Append("</tr>");
			}
		}
		public static Session[] ReadSessionArchive(string sFilename, bool bVerboseDialogs)
		{
			return Utilities.ReadSessionArchive(sFilename, bVerboseDialogs, string.Empty);
		}
		[CodeDescription("Load the specified .SAZ or .ZIP session archive")]
		public static Session[] ReadSessionArchive(string sFilename, bool bVerboseDialogs, string sContext)
		{
			Session[] result;
			if (!System.IO.File.Exists(sFilename))
			{
				if (bVerboseDialogs)
				{
					OpenWebApplication.DoNotifyUser("File " + sFilename + " does not exist.", "ReadSessionArchive Failed", MessageBoxIcon.Hand);
				}
				result = null;
			}
			else
			{
				if (OpenWebApplication.oSAZProvider == null)
				{
					throw new System.NotSupportedException("This application was compiled without .SAZ support.");
				}
				Application.DoEvents();
				System.Collections.Generic.List<Session> list = new System.Collections.Generic.List<Session>();
				try
				{
					using (System.IO.FileStream fileStream = System.IO.File.Open(sFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
					{
						if (fileStream.Length < 64L || fileStream.ReadByte() != 80 || fileStream.ReadByte() != 75)
						{
							string arg = null;
							if (bVerboseDialogs)
							{
								OpenWebApplication.DoNotifyUser(string.Format("{0} is not a OpenWeb-generated .SAZ archive of Web Sessions.{1}", sFilename, arg), "ReadSessionArchive Failed", MessageBoxIcon.Hand);
							}
							Session[] array = null;
							result = array;
							return result;
						}
					}
					ISAZReader iSAZReader = OpenWebApplication.oSAZProvider.LoadSAZ(sFilename);
					string[] requestFileList = iSAZReader.GetRequestFileList();
					if (requestFileList.Length < 1)
					{
						if (bVerboseDialogs)
						{
							OpenWebApplication.DoNotifyUser("The selected file is not a OpenWeb-generated .SAZ archive of Web Sessions.", "Invalid Archive", MessageBoxIcon.Hand);
						}
						iSAZReader.Close();
						Session[] array = null;
						result = array;
						return result;
					}
					string[] array2 = requestFileList;
					for (int i = 0; i < array2.Length; i++)
					{
						string text = array2[i];
						try
						{
							byte[] fileBytes;
							try
							{
								fileBytes = iSAZReader.GetFileBytes(text);
							}
							catch (System.OperationCanceledException)
							{
								iSAZReader.Close();
								Session[] array = null;
								result = array;
								return result;
							}
							string sFilename2 = text.Replace("_c.txt", "_s.txt");
							byte[] fileBytes2 = iSAZReader.GetFileBytes(sFilename2);
							string sFilename3 = text.Replace("_c.txt", "_m.xml");
							System.IO.Stream fileStream2 = iSAZReader.GetFileStream(sFilename3);
							Session session = new Session(fileBytes, fileBytes2);
							if (fileStream2 != null)
							{
								session.LoadMetadata(fileStream2);
							}
							session.oFlags["x-LoadedFrom"] = text.Replace("_c.txt", "_s.txt");
							session.SetBitFlag(SessionFlags.LoadedFromSAZ, true);
							if (session.isAnyFlagSet(SessionFlags.IsWebSocketTunnel) && !session.HTTPMethodIs("CONNECT"))
							{
								string sFilename4 = text.Replace("_c.txt", "_w.txt");
								System.IO.Stream fileStream3 = iSAZReader.GetFileStream(sFilename4);
								if (fileStream3 != null)
								{
									WebSocket.LoadWebSocketMessagesFromStream(session, fileStream3);
								}
								else
								{
									session.oFlags["X-WS-SAZ"] = "SAZ File did not contain any WebSocket messages.";
								}
							}
							list.Add(session);
						}
						catch (System.Exception ex)
						{
							if (bVerboseDialogs)
							{
								OpenWebApplication.DoNotifyUser(string.Format("Invalid data was present for session [{0}].\n\n{1}\n{2}", Utilities.TrimAfter(text, "_"), Utilities.DescribeException(ex), ex.StackTrace), "Archive Incomplete", MessageBoxIcon.Hand);
							}
						}
					}
					iSAZReader.Close();
				}
				catch (System.Exception eX)
				{
					OpenWebApplication.ReportException(eX);
					Session[] array = null;
					result = array;
					return result;
				}
				result = list.ToArray();
			}
			return result;
		}
	}
}
