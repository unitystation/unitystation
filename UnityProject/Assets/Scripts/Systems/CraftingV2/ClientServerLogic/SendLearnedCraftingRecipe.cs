using Logs;
using Systems.CraftingV2.GUI;
using Messages.Server;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A server sends information to a client about the learned recipe, so client can add a new recipe button to
	/// 	a client's crafting menu, and also the client synchronizes its known recipes(adds the new recipe to its
	/// 	known recipes list).
	/// </summary>
	public class SendLearnedCraftingRecipe : ServerMessage<SendLearnedCraftingRecipe.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int CraftingRecipeIndex;
		}

		public override void Process(NetMessage netMessage)
		{
			// let's synchronize known recipes on the client side
			PlayerManager.LocalPlayerScript.PlayerCrafting.UnsafelyAddRecipeToKnownRecipes(
				CraftingRecipeSingleton.Instance.GetRecipeByIndex(netMessage.CraftingRecipeIndex)
			);

			// if the player is trying to learn new recipe without initiated CraftingMenu...
			if (CraftingMenu.Instance == null)
			{
				// ...then we will add a new recipe button when the player will have opened the crafting menu
				return;
			}

			// ok, the crafting menu is already initiated, so it's safe to add new buttons to it
			CraftingMenu.Instance.OnPlayerLearnedRecipe(
				CraftingRecipeSingleton.Instance.GetRecipeByIndex(netMessage.CraftingRecipeIndex)
			);
		}

		public static void SendTo(PlayerInfo connectedPlayer, CraftingRecipe craftingRecipe)
		{
			if (craftingRecipe.IndexInSingleton < 0)
			{
				Loggy.LogError($"The server tried to send the negative recipe index when {connectedPlayer.Name} " +
				                $"had tried to learn this recipe: {craftingRecipe}. " +
				                "Perhaps some recipe is missing from the singleton.");
				return;
			}

			if (
				craftingRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes()
				|| CraftingRecipeSingleton.Instance.GetRecipeByIndex(craftingRecipe.IndexInSingleton)
				!= craftingRecipe
			)
			{
				Loggy.LogError($"The server tried to send the wrong recipe index when {connectedPlayer.Name} " +
				                $"had tried to learn this recipe: {craftingRecipe}. " +
				                "Perhaps some recipe has wrong indexInSingleton that doesn't match a real index in " +
				                "the singleton.");
				return;
			}

			SendTo(
				connectedPlayer, new NetMessage
				{
					CraftingRecipeIndex = craftingRecipe.IndexInSingleton
				}
			);
		}
	}
}