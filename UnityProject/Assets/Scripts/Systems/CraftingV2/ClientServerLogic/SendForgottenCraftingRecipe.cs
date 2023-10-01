using Logs;
using Systems.CraftingV2.GUI;
using Messages.Server;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A server sends to a client information about the forgotten recipe, so the client can remove a necessary
	/// 	recipe button from its crafting menu, and also the client synchronizes its known recipes(removes the recipe
	/// 	from the client's known recipes list).
	/// </summary>
	public class SendForgottenCraftingRecipe : ServerMessage<SendForgottenCraftingRecipe.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int CraftingRecipeIndex;
		}

		public override void Process(NetMessage netMessage)
		{
			// let's synchronize known recipes on the client side.
			PlayerManager.LocalPlayerScript.PlayerCrafting.RemoveRecipeFromKnownRecipes(
				CraftingRecipeSingleton.Instance.GetRecipeByIndex(netMessage.CraftingRecipeIndex)
			);

			// if the player is trying to forget new recipe without initiated CraftingMenu...
			if (CraftingMenu.Instance == null)
			{
				// then there is no need to even update crafting menu
				return;
			}

			// ok, the crafting menu is already initiated, so it's safe to remove buttons from it
			CraftingMenu.Instance.OnPlayerForgotRecipe(
				CraftingRecipeSingleton.Instance.GetRecipeByIndex(netMessage.CraftingRecipeIndex)
			);
		}

		public static void SendTo(PlayerInfo connectedPlayer, CraftingRecipe craftingRecipe)
		{
			if (craftingRecipe.IndexInSingleton < 0)
			{
				Loggy.LogError(
					"The server tried to send the negative recipe index when the server was trying " +
					$"to tell the client({connectedPlayer.Name}) that it had forgot the recipe({craftingRecipe}). " +
					"Perhaps some recipe is missing from the singleton."
				);
				return;
			}

			if (
				craftingRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes()
				|| CraftingRecipeSingleton.Instance.GetRecipeByIndex(craftingRecipe.IndexInSingleton)
				!= craftingRecipe
			)
			{
				Loggy.LogError(
					"The server tried to send the wrong recipe index when the server was trying " +
					$"to tell the client({connectedPlayer.Name}) that it had forgot the recipe({craftingRecipe}). " +
					"Perhaps some recipe has wrong indexInSingleton that doesn't match a real index in " +
					"the singleton."
				);
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