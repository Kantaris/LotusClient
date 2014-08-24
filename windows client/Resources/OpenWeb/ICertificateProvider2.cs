using System;
namespace OpenWeb
{
	public interface ICertificateProvider2 : ICertificateProvider
	{
		bool ClearCertificateCache(bool bClearRoot);
	}
}
