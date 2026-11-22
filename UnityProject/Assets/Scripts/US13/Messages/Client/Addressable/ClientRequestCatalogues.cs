using Mirror;
using US13.Managers.AddressableManager;

namespace US13.Messages.Client.Addressable
{
	public class ClientRequestCatalogues : ClientMessage<ClientRequestCatalogues.NetMessage>
	{
		private static readonly System.Collections.Generic.Dictionary<uint, float> requestTimes =
			new System.Collections.Generic.Dictionary<uint, float>();

		public static void Forget(uint netId) => requestTimes.Remove(netId);

		public struct NetMessage : NetworkMessage
		{
		}

		public override void Process(NetMessage msg)
		{
			if (SentByPlayer?.GameObject == null) return;
			var identity = SentByPlayer.GameObject.GetComponent<NetworkIdentity>();
			if (identity == null) return;
			float now = UnityEngine.Time.realtimeSinceStartup;
			if (requestTimes.TryGetValue(identity.netId, out float last) && now - last < 10f) return;
			requestTimes[identity.netId] = now;
			AddressableCatalogueManager.ClientRequestCatalogue(SentByPlayer.GameObject);
		}

		public static NetMessage RequestCatalogue()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}

	public class ClientAudioContentStatus : ClientMessage<ClientAudioContentStatus.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int Revision;
			public bool Succeeded;
		}

		public static event System.Action<NetworkConnection, int, bool> Received;

		public override void Process(NetMessage message) =>
			Received?.Invoke(SentBy, message.Revision, message.Succeeded);
	}
}
