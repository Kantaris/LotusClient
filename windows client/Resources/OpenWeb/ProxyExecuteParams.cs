using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
namespace OpenWeb
{
	internal class ProxyExecuteParams
	{
		public Socket oSocket;
		public X509Certificate2 oServerCert;
		public System.DateTime dtConnectionAccepted;
		public ProxyExecuteParams(Socket oS, X509Certificate2 oC)
		{
			this.dtConnectionAccepted = System.DateTime.Now;
			this.oSocket = oS;
			this.oServerCert = oC;
		}
	}
}
