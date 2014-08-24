using System;
using System.Security.Cryptography.X509Certificates;
namespace OpenWeb
{
	public interface ICertificateProvider3 : ICertificateProvider2, ICertificateProvider
	{
		bool CacheCertificateForHost(string sHost, X509Certificate2 oCert);
	}
}
