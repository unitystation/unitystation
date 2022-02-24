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
			if (SentByPlayer == null) return;

			if (SentByPlayer.Script == null)
			{
				Logger.LogError($"{SentByPlayer.Username} has null script and asked for recipes");
				return;
			}

			SendInitRecipesOrder.SendTo(
				SentByPlayer,
				SentByPlayer.Script.PlayerCrafting.KnownRecipesByCategory
			);
		}
	}
}