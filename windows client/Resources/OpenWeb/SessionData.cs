using System;
using System.IO;
using System.Runtime.Serialization;
namespace OpenWeb
{
	[System.Serializable]
	public class SessionData : System.Runtime.Serialization.ISerializable
	{
		public byte[] arrRequest;
		public byte[] arrResponse;
		public byte[] arrMetadata;
		public byte[] arrWebSocketMessages;
		public SessionData(Session oS)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			oS.WriteRequestToStream(false, true, memoryStream);
			this.arrRequest = memoryStream.ToArray();
			memoryStream = new System.IO.MemoryStream();
			oS.WriteResponseToStream(memoryStream, false);
			this.arrResponse = memoryStream.ToArray();
			memoryStream = new System.IO.MemoryStream();
			oS.WriteMetadataToStream(memoryStream);
			this.arrMetadata = memoryStream.ToArray();
			memoryStream = new System.IO.MemoryStream();
			oS.WriteWebSocketMessagesToStream(memoryStream);
			this.arrWebSocketMessages = memoryStream.ToArray();
		}
		public SessionData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext ctxt)
		{
			this.arrRequest = (byte[])info.GetValue("Request", typeof(byte[]));
			this.arrResponse = (byte[])info.GetValue("Response", typeof(byte[]));
			this.arrMetadata = (byte[])info.GetValue("Metadata", typeof(byte[]));
			this.arrWebSocketMessages = (byte[])info.GetValue("WSMsgs", typeof(byte[]));
		}
		public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext ctxt)
		{
			info.AddValue("Request", this.arrRequest);
			info.AddValue("Response", this.arrResponse);
			info.AddValue("Metadata", this.arrMetadata);
			info.AddValue("WSMsgs", this.arrWebSocketMessages);
		}
	}
}
