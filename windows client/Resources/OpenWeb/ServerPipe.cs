using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace OpenWeb
{
	public class ServerPipe : BasePipe
	{
		internal static int _timeoutSendInitial = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.send.initial", -1);
		internal static int _timeoutSendReused = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.send.reuse", -1);
		internal static int _timeoutReceiveInitial = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.receive.initial", -1);
		internal static int _timeoutReceiveReused = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.timeouts.serverpipe.receive.reuse", -1);
		private static StringCollection slAcceptableBadCertificates;
		private PipeReusePolicy _reusePolicy;
		internal System.DateTime dtConnected;
		internal ulong ulLastPooled;
		protected bool _bIsConnectedToGateway;
		private bool _bIsConnectedViaSOCKS;
		protected string _sPoolKey;
		private int _iMarriedToPID;
		private bool _isAuthenticated;
		private string _ServerCertChain;
		public PipeReusePolicy ReusePolicy
		{
			get
			{
				return this._reusePolicy;
			}
			set
			{
				this._reusePolicy = value;
			}
		}
		internal bool isClientCertAttached
		{
			get
			{
				return this._httpsStream != null && this._httpsStream.IsMutuallyAuthenticated;
			}
		}
		internal bool isAuthenticated
		{
			get
			{
				return this._isAuthenticated;
			}
		}
		public bool isConnectedToGateway
		{
			get
			{
				return this._bIsConnectedToGateway;
			}
		}
		public bool isConnectedViaSOCKS
		{
			get
			{
				return this._bIsConnectedViaSOCKS;
			}
			set
			{
				this._bIsConnectedViaSOCKS = value;
			}
		}
		public string sPoolKey
		{
			get
			{
				return this._sPoolKey;
			}
			private set
			{
				if (CONFIG.bDebugSpew && !string.IsNullOrEmpty(this._sPoolKey) && this._sPoolKey != value)
				{
				}
				this._sPoolKey = value;
			}
		}
		public IPEndPoint RemoteEndPoint
		{
			get
			{
				IPEndPoint result;
				if (this._baseSocket == null)
				{
					result = null;
				}
				else
				{
					IPEndPoint iPEndPoint;
					try
					{
						iPEndPoint = (this._baseSocket.RemoteEndPoint as IPEndPoint);
					}
					catch (System.Exception)
					{
						iPEndPoint = null;
					}
					result = iPEndPoint;
				}
				return result;
			}
		}
		internal ServerPipe(Socket oSocket, string sName, bool bConnectedToGateway, string sPoolingKey) : base(oSocket, sName)
		{
			this.dtConnected = System.DateTime.Now;
			this._bIsConnectedToGateway = bConnectedToGateway;
			this.sPoolKey = sPoolingKey;
		}
		internal void MarkAsAuthenticated(int clientPID)
		{
			this._isAuthenticated = true;
			int num = OpenWebApplication.Prefs.GetInt32Pref("OpenWeb.network.auth.reusemode", 0);
			if (num == 0 && clientPID == 0)
			{
				num = 1;
			}
			if (num == 0)
			{
				this.ReusePolicy = PipeReusePolicy.MarriedToClientProcess;
				this._iMarriedToPID = clientPID;
				this.sPoolKey = string.Format("PID{0}*{1}", clientPID, this.sPoolKey);
			}
			else
			{
				if (num == 1)
				{
					this.ReusePolicy = PipeReusePolicy.MarriedToClientPipe;
				}
			}
		}
		internal void setTimeouts()
		{
			try
			{
				int num = (this.iUseCount < 2u) ? ServerPipe._timeoutReceiveInitial : ServerPipe._timeoutReceiveReused;
				int num2 = (this.iUseCount < 2u) ? ServerPipe._timeoutSendInitial : ServerPipe._timeoutSendReused;
				if (num > 0)
				{
					this._baseSocket.ReceiveTimeout = num;
				}
				if (num2 > 0)
				{
					this._baseSocket.SendTimeout = num2;
				}
			}
			catch
			{
			}
		}
		public override string ToString()
		{
			return string.Format("{0}[Key: {1}; UseCnt: {2} [{3}]; {4}; {5} (:{6} to {7}:{8} {9}) {10}]", new object[]
			{
				this._sPipeName,
				this._sPoolKey,
				this.iUseCount,
				string.Empty,
				base.bIsSecured ? "Secure" : "PlainText",
				this._isAuthenticated ? "Authenticated" : "Anonymous",
				base.LocalPort,
				base.Address,
				base.Port,
				this.isConnectedToGateway ? "Gateway" : "Direct",
				this._reusePolicy
			});
		}
		private static string SummarizeCert(X509Certificate2 oCert)
		{
			string result;
			if (!string.IsNullOrEmpty(oCert.FriendlyName))
			{
				result = oCert.FriendlyName;
			}
			else
			{
				string subject = oCert.Subject;
				if (string.IsNullOrEmpty(subject))
				{
					result = string.Empty;
				}
				else
				{
					if (subject.Contains("CN="))
					{
						result = Utilities.TrimAfter(Utilities.TrimBefore(subject, "CN="), ",");
					}
					else
					{
						if (subject.Contains("O="))
						{
							result = Utilities.TrimAfter(Utilities.TrimBefore(subject, "O="), ",");
						}
						else
						{
							result = subject;
						}
					}
				}
			}
			return result;
		}
		internal string GetServerCertCN()
		{
			string result;
			if (this._httpsStream == null)
			{
				result = null;
			}
			else
			{
				if (this._httpsStream.RemoteCertificate == null)
				{
					result = null;
				}
				else
				{
					string subject = this._httpsStream.RemoteCertificate.Subject;
					if (subject.Contains("CN="))
					{
						result = Utilities.TrimAfter(Utilities.TrimBefore(subject, "CN="), ",");
					}
					else
					{
						result = subject;
					}
				}
			}
			return result;
		}
		internal string GetServerCertChain()
		{
			string result;
			if (this._ServerCertChain != null)
			{
				result = this._ServerCertChain;
			}
			else
			{
				if (this._httpsStream == null)
				{
					result = string.Empty;
				}
				else
				{
					string text;
					try
					{
						X509Certificate2 x509Certificate = new X509Certificate2(this._httpsStream.RemoteCertificate);
						if (x509Certificate == null)
						{
							text = string.Empty;
						}
						else
						{
							System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
							X509Chain x509Chain = new X509Chain();
							x509Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
							x509Chain.Build(x509Certificate);
							for (int i = x509Chain.ChainElements.Count - 1; i >= 1; i--)
							{
								stringBuilder.Append(ServerPipe.SummarizeCert(x509Chain.ChainElements[i].Certificate));
								stringBuilder.Append(" > ");
							}
							if (x509Chain.ChainElements.Count > 0)
							{
								stringBuilder.AppendFormat("{0} [{1}]", ServerPipe.SummarizeCert(x509Chain.ChainElements[0].Certificate), x509Chain.ChainElements[0].Certificate.SerialNumber);
							}
							this._ServerCertChain = stringBuilder.ToString();
							text = stringBuilder.ToString();
						}
					}
					catch (System.Exception ex)
					{
						text = ex.Message;
					}
					result = text;
				}
			}
			return result;
		}
		public string DescribeConnectionSecurity()
		{
			string result;
			if (this._httpsStream != null)
			{
				string value = string.Empty;
				if (this._httpsStream.IsMutuallyAuthenticated)
				{
					value = "== Client Certificate ==========\nUnknown.\n";
				}
				if (this._httpsStream.LocalCertificate != null)
				{
					value = "\n== Client Certificate ==========\n" + this._httpsStream.LocalCertificate.ToString(true) + "\n";
				}
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(2048);
				stringBuilder.AppendFormat("Secure Protocol: {0}\n", this._httpsStream.SslProtocol.ToString());
				stringBuilder.AppendFormat("Cipher: {0} {1}bits\n", this._httpsStream.CipherAlgorithm.ToString(), this._httpsStream.CipherStrength);
				stringBuilder.AppendFormat("Hash Algorithm: {0} {1}bits\n", this._httpsStream.HashAlgorithm.ToString(), this._httpsStream.HashStrength);
				string text = this._httpsStream.KeyExchangeAlgorithm.ToString();
				if (text == "44550")
				{
					text = "ECDHE_RSA (0xae06)";
				}
				stringBuilder.AppendFormat("Key Exchange: {0} {1}bits\n", text, this._httpsStream.KeyExchangeStrength);
				stringBuilder.Append(value);
				stringBuilder.AppendLine("\n== Server Certificate ==========");
				stringBuilder.AppendLine(this._httpsStream.RemoteCertificate.ToString(true));
				if (OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.https.storeservercertchain", false))
				{
					stringBuilder.AppendFormat("[Chain]\n {0}\n", this.GetServerCertChain());
				}
				result = stringBuilder.ToString();
			}
			else
			{
				result = "No connection security";
			}
			return result;
		}
		internal string GetConnectionCipherInfo()
		{
			string result;
			if (this._httpsStream == null)
			{
				result = "<none>";
			}
			else
			{
				result = string.Format("{0} {1}bits", this._httpsStream.CipherAlgorithm.ToString(), this._httpsStream.CipherStrength);
			}
			return result;
		}
		internal TransportContext _GetTransportContext()
		{
			TransportContext result;
			if (this._httpsStream != null)
			{
				result = this._httpsStream.TransportContext;
			}
			else
			{
				result = null;
			}
			return result;
		}
		private static bool ConfirmServerCertificate(Session oS, string sExpectedCN, System.Security.Cryptography.X509Certificates.X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			CertificateValidity certificateValidity = CertificateValidity.Default;
			OpenWebApplication.CheckOverrideCertificatePolicy(oS, sExpectedCN, certificate, chain, sslPolicyErrors, ref certificateValidity);
			bool result;
			if (certificateValidity == CertificateValidity.ForceInvalid)
			{
				result = false;
			}
			else
			{
				if (certificateValidity == CertificateValidity.ForceValid)
				{
					result = true;
				}
				else
				{
					if ((certificateValidity != CertificateValidity.ConfirmWithUser && (sslPolicyErrors == SslPolicyErrors.None || CONFIG.IgnoreServerCertErrors)) || oS.oFlags.ContainsKey("X-IgnoreCertErrors"))
					{
						result = true;
					}
					else
					{
						if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch && oS.oFlags.ContainsKey("X-IgnoreCertCNMismatch"))
						{
							sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
							if (sslPolicyErrors == SslPolicyErrors.None)
							{
								result = true;
								return result;
							}
						}
						result = false;
					}
				}
			}
			return result;
		}
		private static System.Security.Cryptography.X509Certificates.X509Certificate _GetDefaultCertificate()
		{
			System.Security.Cryptography.X509Certificates.X509Certificate result;
			if (OpenWebApplication.oDefaultClientCertificate != null)
			{
				result = OpenWebApplication.oDefaultClientCertificate;
			}
			else
			{
				System.Security.Cryptography.X509Certificates.X509Certificate x509Certificate = null;
				if (System.IO.File.Exists(CONFIG.GetPath("DefaultClientCertificate")))
				{
					x509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromCertFile(CONFIG.GetPath("DefaultClientCertificate"));
					if (x509Certificate != null && OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.https.cacheclientcert", true))
					{
						OpenWebApplication.oDefaultClientCertificate = x509Certificate;
					}
				}
				result = x509Certificate;
			}
			return result;
		}
		private System.Security.Cryptography.X509Certificates.X509Certificate AttachClientCertificate(Session oS, object sender, string targetHost, X509CertificateCollection localCertificates, System.Security.Cryptography.X509Certificates.X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			if (CONFIG.bDebugSpew)
			{
			}
			System.Security.Cryptography.X509Certificates.X509Certificate result;
			if (localCertificates.Count > 0)
			{
				this.MarkAsAuthenticated(oS.LocalProcessID);
				oS.oFlags["x-client-cert"] = localCertificates[0].Subject + " Serial#" + localCertificates[0].GetSerialNumberString();
				result = localCertificates[0];
			}
			else
			{
				if (OpenWebApplication.ClientCertificateProvider != null)
				{
					System.Security.Cryptography.X509Certificates.X509Certificate x509Certificate = OpenWebApplication.ClientCertificateProvider(oS, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
					if (x509Certificate != null)
					{
						if (CONFIG.bDebugSpew)
						{
							Trace.WriteLine(string.Format("Session #{0} Attaching client certificate '{1}' when connecting to host '{2}'", oS.id, x509Certificate.Subject, targetHost));
						}
						this.MarkAsAuthenticated(oS.LocalProcessID);
						oS.oFlags["x-client-cert"] = x509Certificate.Subject + " Serial#" + x509Certificate.GetSerialNumberString();
					}
					result = x509Certificate;
				}
				else
				{
					bool flag = remoteCertificate != null || acceptableIssuers.Length > 0;
					System.Security.Cryptography.X509Certificates.X509Certificate x509Certificate2 = ServerPipe._GetDefaultCertificate();
					if (x509Certificate2 != null)
					{
						if (flag)
						{
							this.MarkAsAuthenticated(oS.LocalProcessID);
						}
						oS.oFlags["x-client-cert"] = x509Certificate2.Subject + " Serial#" + x509Certificate2.GetSerialNumberString();
						result = x509Certificate2;
					}
					else
					{
						if (flag)
						{
							if (CONFIG.bShowDefaultClientCertificateNeededPrompt && OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.https.clientcertificate.ephemeral.prompt-for-missing", true))
							{
								OpenWebApplication.Prefs.SetBoolPref("OpenWeb.network.https.clientcertificate.ephemeral.prompt-for-missing", false);
								OpenWebApplication.DoNotifyUser("The server [" + targetHost + "] requests a client certificate.\nPlease save a client certificate using the filename:\n\n" + CONFIG.GetPath("DefaultClientCertificate"), "Client Certificate Requested");
							}
						}
						result = null;
					}
				}
			}
			return result;
		}
		internal bool SecureExistingConnection(Session oS, string sCertCN, string sClientCertificateFilename, ref int iHandshakeTime)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			bool result;
			try
			{
				this.sPoolKey = this.sPoolKey.Replace("->http/", "->https/");
				if (this.sPoolKey.EndsWith("->*"))
				{
					this.sPoolKey = this.sPoolKey.Replace("->*", string.Format("->https/{0}:{1}", oS.hostname, oS.port));
				}
				X509CertificateCollection certificateCollectionFromFile = ServerPipe.GetCertificateCollectionFromFile(sClientCertificateFilename);
				this._httpsStream = new SslStream(new NetworkStream(this._baseSocket, false), false, (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => ServerPipe.ConfirmServerCertificate(oS, sCertCN, certificate, chain, sslPolicyErrors), (object sender, string targetHost, X509CertificateCollection localCertificates, System.Security.Cryptography.X509Certificates.X509Certificate remoteCertificate, string[] acceptableIssuers) => this.AttachClientCertificate(oS, sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers));
				SslProtocols enabledSslProtocols = CONFIG.oAcceptedServerHTTPSProtocols;
				if (oS.oFlags.ContainsKey("x-OverrideSslProtocols"))
				{
					enabledSslProtocols = Utilities.ParseSSLProtocolString(oS.oFlags["x-OverrideSslProtocols"]);
				}
				this._httpsStream.AuthenticateAsClient(sCertCN, certificateCollectionFromFile, enabledSslProtocols, OpenWebApplication.Prefs.GetBoolPref("OpenWeb.network.https.checkcertificaterevocation", false));
				iHandshakeTime = (int)stopwatch.ElapsedMilliseconds;
			}
			catch (System.Exception ex)
			{
				iHandshakeTime = (int)stopwatch.ElapsedMilliseconds;
				OpenWebApplication.DebugSpew(ex.StackTrace + "\n" + ex.Message);
				string s = string.Format("OpenWeb.network.https> Failed to secure existing connection for {0}. {1}{2}", sCertCN, ex.Message, (ex.InnerException != null) ? (" InnerException: " + ex.InnerException.ToString()) : ".");
				if (oS.responseBodyBytes == null || oS.responseBodyBytes.Length == 0)
				{
					oS.responseBodyBytes = System.Text.Encoding.UTF8.GetBytes(s);
				}
				result = false;
				return result;
			}
			result = true;
			return result;
		}
		private static X509CertificateCollection GetCertificateCollectionFromFile(string sClientCertificateFilename)
		{
			X509CertificateCollection x509CertificateCollection = null;
			if (!string.IsNullOrEmpty(sClientCertificateFilename))
			{
				sClientCertificateFilename = Utilities.EnsurePathIsAbsolute(CONFIG.GetPath("Root"), sClientCertificateFilename);
				if (System.IO.File.Exists(sClientCertificateFilename))
				{
					x509CertificateCollection = new X509CertificateCollection();
					x509CertificateCollection.Add(System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromCertFile(sClientCertificateFilename));
				}
			}
			return x509CertificateCollection;
		}
	}
}
