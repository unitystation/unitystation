using Messages.Client;
using Messages.Server;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	public class RequestPossibleCraftingResources : ClientMessage<RequestPossibleCraftingResources.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			SendPossibleCraftingResources.SendTo(
				SentByPlayer,
				SentByPlayer.Script.PlayerCrafting.GetPossibleIngredients(),
				SentByPlayer.Script.PlayerCrafting.GetPossibleTools()
			);
		}

		public static void Send()
		{
			Send(new NetMessage());
		}
	}
}