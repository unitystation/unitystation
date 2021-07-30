using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	public class RequestCraftingAction : ClientMessage<RequestCraftingAction.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int CraftingRecipeIndex;
		}

		public override void Process(NetMessage netMessage)
		{
			if (netMessage.CraftingRecipeIndex < 0)
			{
				Logger.LogError($"Received the negative recipe index when {SentByPlayer.Name} " +
				                "had tried to craft something. Perhaps some recipe is missing from the singleton.");
				return;
			}

			SentByPlayer.Script.PlayerCrafting.TryToStartCrafting(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.CraftingRecipeIndex]
			);
		}

		public static void Send(CraftingRecipe craftingRecipe)
		{
			Send(new NetMessage
			{
				CraftingRecipeIndex = CraftingRecipeSingleton.Instance.StoredCraftingRecipes.IndexOf(craftingRecipe)
			});
		}
	}
}