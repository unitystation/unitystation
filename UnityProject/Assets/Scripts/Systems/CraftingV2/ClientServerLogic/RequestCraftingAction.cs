using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A client asks a server to craft a client's selected recipe. This ClientMessage is designed to handle it.
	/// </summary>
	public class RequestCraftingAction : ClientMessage<RequestCraftingAction.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// recipe index in the recipes singleton
			public int CraftingRecipeIndex;
			public bool IsRecipeIndexWrong;
		}

		public override void Process(NetMessage netMessage)
		{
			if (netMessage.CraftingRecipeIndex < 0)
			{
				Logger.LogError($"Received the negative recipe index when {SentByPlayer.Name} " +
				                "had tried to craft something. Perhaps some recipe is missing from the singleton.");
				return;
			}

			if (netMessage.IsRecipeIndexWrong)
			{
				Logger.LogError(
					$"Received the wrong recipe index when {SentByPlayer.Name} had tried to craft something. " +
					"Perhaps some recipe has wrong indexInSingleton that doesn't match a real index in " +
					"the singleton."
				);
				return;
			}

			SentByPlayer.Script.PlayerCrafting.TryToStartCrafting(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.CraftingRecipeIndex]
			);
		}

		public static void Send(CraftingRecipe craftingRecipe)
		{
			bool sendingWrongRecipeIndex =
				craftingRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.StoredCraftingRecipes.Count
				|| CraftingRecipeSingleton.Instance.StoredCraftingRecipes[craftingRecipe.IndexInSingleton]
					!= craftingRecipe;
			Send(new NetMessage
			{
				CraftingRecipeIndex = craftingRecipe.IndexInSingleton,
				IsRecipeIndexWrong = sendingWrongRecipeIndex
			});
		}
	}
}