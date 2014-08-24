using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace OpenWeb
{
	internal class HTTPSServerHello
	{
		private int _HandshakeVersion;
		private int _MessageLen;
		private int _MajorVersion;
		private int _MinorVersion;
		private byte[] _Random;
		private byte[] _SessionID;
		private uint _iCipherSuite;
		private int _iCompression;
		private System.Collections.Generic.List<string> _Extensions;
		public bool bALPNToSPDY
		{
			get;
			set;
		}
		private string CompressionSuite
		{
			get
			{
				string result;
				if (this._iCompression < HTTPSClientHello.HTTPSCompressionSuites.Length)
				{
					result = HTTPSClientHello.HTTPSCompressionSuites[this._iCompression];
				}
				else
				{
					result = string.Format("Unrecognized compression format [0x{0:X2}]", this._iCompression);
				}
				return result;
			}
		}
		internal string CipherSuite
		{
			get
			{
				string result;
				if ((ulong)this._iCipherSuite < (ulong)((long)HTTPSClientHello.SSL3CipherSuites.Length))
				{
					result = HTTPSClientHello.SSL3CipherSuites[(int)((uint)((System.UIntPtr)this._iCipherSuite))];
				}
				else
				{
					if (HTTPSClientHello.dictTLSCipherSuites.ContainsKey(this._iCipherSuite))
					{
						result = HTTPSClientHello.dictTLSCipherSuites[this._iCipherSuite];
					}
					else
					{
						result = string.Format("Unrecognized cipher [0x{0:X4}] - See http://www.iana.org/assignments/tls-parameters/", this._iCipherSuite);
					}
				}
				return result;
			}
		}
		public string SessionID
		{
			get
			{
				string result;
				if (this._SessionID == null)
				{
					result = string.Empty;
				}
				else
				{
					result = Utilities.ByteArrayToString(this._SessionID);
				}
				return result;
			}
		}
		public bool bNPNToSPDY
		{
			get;
			set;
		}
		public override string ToString()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(512);
			if (this._HandshakeVersion == 2)
			{
				stringBuilder.Append("A SSLv2-compatible ServerHello handshake was found. In v2, the ~client~ selects the active cipher after the ServerHello, when sending the Client-Master-Key message. OpenWeb only parses the handshake.\n\n");
			}
			else
			{
				stringBuilder.Append("A SSLv3-compatible ServerHello handshake was found. OpenWeb extracted the parameters below.\n\n");
			}
			stringBuilder.AppendFormat("Version: {0}\n", HTTPSUtilities.HTTPSVersionToString(this._MajorVersion, this._MinorVersion));
			stringBuilder.AppendFormat("SessionID:\t{0}\n", Utilities.ByteArrayToString(this._SessionID));
			if (this._HandshakeVersion == 3)
			{
				stringBuilder.AppendFormat("Random:\t\t{0}\n", Utilities.ByteArrayToString(this._Random));
				stringBuilder.AppendFormat("Cipher:\t\t{0} [0x{1:X4}]\n", this.CipherSuite, this._iCipherSuite);
			}
			stringBuilder.AppendFormat("CompressionSuite:\t{0} [0x{1:X2}]\n", this.CompressionSuite, this._iCompression);
			stringBuilder.AppendFormat("Extensions:\n\t{0}\n", HTTPSServerHello.ExtensionListToString(this._Extensions));
			return stringBuilder.ToString();
		}
		private static string ExtensionListToString(System.Collections.Generic.List<string> slExts)
		{
			string result;
			if (slExts == null || slExts.Count < 1)
			{
				result = "\tnone";
			}
			else
			{
				result = string.Join("\n\t", slExts.ToArray());
			}
			return result;
		}
		private void ParseServerHelloExtension(int iExtType, byte[] arrData)
		{
			if (this._Extensions == null)
			{
				this._Extensions = new System.Collections.Generic.List<string>();
			}
			if (iExtType <= 35)
			{
				switch (iExtType)
				{
				case 0:
					this._Extensions.Add(string.Format("\tserver_name\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 1:
					this._Extensions.Add(string.Format("\tmax_fragment_length\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 2:
					this._Extensions.Add(string.Format("\tclient_certificate_url\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 3:
					this._Extensions.Add(string.Format("\ttrusted_ca_keys\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 4:
					this._Extensions.Add(string.Format("\ttruncated_hmac\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 5:
					this._Extensions.Add(string.Format("\tstatus_request\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 6:
					this._Extensions.Add(string.Format("\tuser_mapping\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 7:
				case 8:
				case 15:
					break;
				case 9:
					this._Extensions.Add(string.Format("\tcert_type\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 10:
					this._Extensions.Add(string.Format("\telliptic_curves\t{0}", HTTPSUtilities.GetECCCurvesAsString(arrData)));
					return;
				case 11:
					this._Extensions.Add(string.Format("\tec_point_formats\t{0}", HTTPSUtilities.GetECCPointFormatsAsString(arrData)));
					return;
				case 12:
					this._Extensions.Add(string.Format("\tsrp_rfc_5054\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 13:
					this._Extensions.Add(string.Format("\tsignature_algorithms\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 14:
					this._Extensions.Add(string.Format("\tuse_srtp\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 16:
				{
					string protocolListAsString = HTTPSUtilities.GetProtocolListAsString(arrData);
					this._Extensions.Add(string.Format("\tALPN\t\t{0}", protocolListAsString));
					if (protocolListAsString.Contains("spdy/"))
					{
						this.bALPNToSPDY = true;
						return;
					}
					return;
				}
				default:
					if (iExtType == 35)
					{
						this._Extensions.Add(string.Format("\tSessionTicket\t{0}", Utilities.ByteArrayToString(arrData)));
						return;
					}
					break;
				}
			}
			else
			{
				if (iExtType != 13172)
				{
					switch (iExtType)
					{
					case 30031:
					case 30032:
						this._Extensions.Add(string.Format("\tchannel_id(GoogleDraft)\t{0}", Utilities.ByteArrayToString(arrData)));
						return;
					default:
						if (iExtType == 65281)
						{
							this._Extensions.Add(string.Format("\trenegotiation_info\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						}
						break;
					}
				}
				else
				{
					string extensionString = HTTPSUtilities.GetExtensionString(arrData);
					this._Extensions.Add(string.Format("\tNextProtocolNegotiation\t{0}", extensionString));
					if (extensionString.Contains("spdy/"))
					{
						this.bNPNToSPDY = true;
						return;
					}
					return;
				}
			}
			this._Extensions.Add(string.Format("\t0x{0:x4}\t\t{1}", iExtType, Utilities.ByteArrayToString(arrData)));
		}
		private void ParseServerHelloExtensions(byte[] arrExtensionsData)
		{
			int i = 0;
			try
			{
				while (i < arrExtensionsData.Length)
				{
					int iExtType = ((int)arrExtensionsData[i] << 8) + (int)arrExtensionsData[i + 1];
					int num = ((int)arrExtensionsData[i + 2] << 8) + (int)arrExtensionsData[i + 3];
					byte[] array = new byte[num];
					System.Buffer.BlockCopy(arrExtensionsData, i + 4, array, 0, array.Length);
					try
					{
						this.ParseServerHelloExtension(iExtType, array);
					}
					catch (System.Exception var_4_44)
					{
					}
					i += 4 + num;
				}
			}
			catch (System.Exception var_5_61)
			{
			}
		}
		internal bool LoadFromStream(System.IO.Stream oNS)
		{
			int num = oNS.ReadByte();
			bool result;
			if (num == 22)
			{
				this._HandshakeVersion = 3;
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				int num2 = oNS.ReadByte() << 8;
				num2 += oNS.ReadByte();
				oNS.ReadByte();
				byte[] array = new byte[3];
				int num3 = oNS.Read(array, 0, array.Length);
				this._MessageLen = ((int)array[0] << 16) + ((int)array[1] << 8) + (int)array[2];
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				this._Random = new byte[32];
				num3 = oNS.Read(this._Random, 0, 32);
				int num4 = oNS.ReadByte();
				this._SessionID = new byte[num4];
				num3 = oNS.Read(this._SessionID, 0, this._SessionID.Length);
				this._iCipherSuite = (uint)((oNS.ReadByte() << 8) + oNS.ReadByte());
				this._iCompression = oNS.ReadByte();
				if (this._MajorVersion < 3 || (this._MajorVersion == 3 && this._MinorVersion < 1))
				{
					result = true;
				}
				else
				{
					array = new byte[2];
					num3 = oNS.Read(array, 0, array.Length);
					if (num3 < 2)
					{
						result = true;
					}
					else
					{
						int num5 = ((int)array[0] << 8) + (int)array[1];
						if (num5 < 1)
						{
							result = true;
						}
						else
						{
							array = new byte[num5];
							num3 = oNS.Read(array, 0, array.Length);
							if (num3 == array.Length)
							{
								this.ParseServerHelloExtensions(array);
							}
							result = true;
						}
					}
				}
			}
			else
			{
				if (num == 21)
				{
					byte[] buffer = new byte[7];
					oNS.Read(buffer, 0, 7);
					result = false;
				}
				else
				{
					this._HandshakeVersion = 2;
					oNS.ReadByte();
					if (128 != (num & 128))
					{
						oNS.ReadByte();
					}
					num = oNS.ReadByte();
					if (num != 4)
					{
						result = false;
					}
					else
					{
						this._SessionID = new byte[1];
						oNS.Read(this._SessionID, 0, 1);
						oNS.ReadByte();
						this._MinorVersion = oNS.ReadByte();
						this._MajorVersion = oNS.ReadByte();
						result = true;
					}
				}
			}
			return result;
		}
	}
}
