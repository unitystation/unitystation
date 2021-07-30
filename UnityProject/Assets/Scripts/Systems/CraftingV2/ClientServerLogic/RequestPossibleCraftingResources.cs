using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A client asks a server to give the client available(possible) ingredients.
	/// 	This ClientMessage is designed to handle it.
	/// </summary>
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