using Messages.Client;
using Mirror;

namespace Systems.CraftingV2
{
	public class RequestCraftingAction : ClientMessage<RequestCraftingAction.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int craftingRecipeIndex;
		}

		public override void Process(NetMessage netMessage)
		{
			if (netMessage.craftingRecipeIndex == NetId.Invalid)
			{
				Logger.LogError($"Received invalid recipe index when {SentByPlayer.Name} " +
				                "had tried to craft something.");
				return;
			}

			if (netMessage.craftingRecipeIndex < 0)
			{
				Logger.LogError($"Received negative recipe index when {SentByPlayer.Name} " +
				                "had tried to craft something. Perhaps some recipe is missing from the singleton.");
				return;
			}

			SentByPlayer.Script.PlayerCrafting.TryToStartCrafting(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.craftingRecipeIndex]
			);
		}

		public static void Send(CraftingRecipe craftingRecipe)
		{
			Send(new NetMessage
			{
				craftingRecipeIndex = CraftingRecipeSingleton.Instance.StoredCraftingRecipes.IndexOf(craftingRecipe)
			});
		}
	}
}