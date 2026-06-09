using Mirror;
using US13.Messages.Client;
using Util;

namespace US13.Systems.CraftingV2.ClientServerLogic
{
	public class RequestInitRecipes : ClientMessage<RequestInitRecipes.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{

		}

		public override void Process(NetMessage msg)
		{
			if (SentByPlayer == null) return;

			if (SentByPlayer.Script == null)
			{
				return;
			}

			if (SentByPlayer?.Script.OrNull()?.PlayerCrafting.OrNull()?.KnownRecipesByCategory == null) return;

			SendInitRecipesOrder.SendTo(
				SentByPlayer,
				SentByPlayer.Script.PlayerCrafting.KnownRecipesByCategory, SentByPlayer.Script.PlayerCrafting.gameObject
			);
		}
	}
}