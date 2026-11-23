using Mirror;

namespace US13.Core.SecureStuff.CustomAudio
{
	public static class CustomAudioChunkMessage
	{
		public struct NetMessage : NetworkMessage
		{
			public string Id;
			public string Token;
			public long Offset;
			public byte[] Data;
			public bool Error;
		}
		public static void RegisterHandler() => NetworkClient.RegisterHandler<NetMessage>(CustomAudioTransfer.Receive, true);
		public static void SendTo(NetworkConnection connection, NetMessage message) => connection.Send(message);
	}
}
