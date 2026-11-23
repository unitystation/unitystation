using Mirror;

namespace US13.Core.SecureStuff.CustomAudio
{
	public static class CustomAudioRequestMessage
	{
		public struct NetMessage : NetworkMessage
		{
			public string Id;
			public string Token;
			public long Offset;
		}

		public static void RegisterHandler()
		{
			NetworkServer.RegisterHandler<NetMessage>((connection, message) =>
					CustomAudioTransfer.Serve(connection, message.Id, message.Token, message.Offset), requireAuthentication: true
				);
		}

		public static void Send(NetMessage message) => NetworkClient.Send(message);
	}
}
