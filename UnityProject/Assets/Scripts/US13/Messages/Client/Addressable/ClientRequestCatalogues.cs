using Mirror;
using US13.Managers.AddressableManager;

namespace US13.Messages.Client.Addressable
{
	public class ClientRequestCatalogues : ClientMessage<ClientRequestCatalogues.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			AddressableCatalogueManager.ClientRequestCatalogue(SentByPlayer.GameObject);
		}

		public static NetMessage RequestCatalogue()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
