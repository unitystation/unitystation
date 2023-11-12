using Logs;
using Messages.Client;
using Mirror;
using Player;

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
				Loggy.LogError($"{SentByPlayer.Username} has null script and asked for recipes");
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