using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	public class RequestInitRecipes : ClientMessage<RequestInitRecipes.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{

		}

		public override void Process(NetMessage msg)
		{
			SendInitRecipesOrder.SendTo(
				SentByPlayer,
				SentByPlayer.Script.PlayerCrafting.KnownRecipesByCategory
			);
		}
	}
}