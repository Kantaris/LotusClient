using System;
namespace OpenWeb
{
	[System.Flags]
	public enum SessionFlags
	{
		None = 0,
		IsHTTPS = 1,
		IsFTP = 2,
		Ignored = 4,
		ClientPipeReused = 8,
		ServerPipeReused = 16,
		RequestStreamed = 32,
		ResponseStreamed = 64,
		RequestGeneratedByOpenWeb = 128,
		ResponseGeneratedByOpenWeb = 256,
		LoadedFromSAZ = 512,
		ImportedFromOtherTool = 1024,
		SentToGateway = 2048,
		IsBlindTunnel = 4096,
		IsDecryptingTunnel = 8192,
		ServedFromCache = 16384,
		ProtocolViolationInRequest = 32768,
		ProtocolViolationInResponse = 65536,
		ResponseBodyDropped = 131072,
		IsWebSocketTunnel = 262144,
		SentToSOCKSGateway = 524288
	}
}
