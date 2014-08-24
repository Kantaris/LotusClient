using System;
using System.Security.Cryptography;
using System.Text;
namespace OpenWeb
{
	internal class WebSocketUtility
	{
		internal static string ComputeAcceptKey(string sSecWebSocketKeyFromClient)
		{
			System.Security.Cryptography.SHA1 sHA = System.Security.Cryptography.SHA1.Create();
			string result = System.Convert.ToBase64String(sHA.ComputeHash(System.Text.Encoding.ASCII.GetBytes(sSecWebSocketKeyFromClient + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
			sHA.Clear();
			return result;
		}
	}
}
