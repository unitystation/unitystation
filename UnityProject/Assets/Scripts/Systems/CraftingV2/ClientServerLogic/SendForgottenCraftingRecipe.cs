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
			PlayerManager.LocalPlayerScript.PlayerCrafting.UnsafelyAddRecipeToKnownRecipes(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.CraftingRecipeIndex]
			);

			// if the player is trying to learn new recipe without initiated CraftingMenu...
			if (CraftingMenu.Instance == null)
			{
				// ...then we will add a new recipe button when the player will have opened the crafting menu
				// (in other words when the CraftingMenu.Awake() method will be called)
				return;
			}

			// ok, the crafting menu is already initiated, so it's safe to remove buttons from it
			CraftingMenu.Instance.OnPlayerForgotRecipe(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.CraftingRecipeIndex]
			);
		}

		public static void SendTo(ConnectedPlayer connectedPlayer, CraftingRecipe craftingRecipe)
		{
			int craftingRecipeIndexToSend = CraftingRecipeSingleton
				.Instance
				.StoredCraftingRecipes
				.IndexOf(craftingRecipe);

			// is the recipe missing from the recipes singleton?
			if (craftingRecipeIndexToSend < 0)
			{
				Logger.LogError($"The server tried to send the negative recipe index when {connectedPlayer.Name} " +
				                $"had tried to forget this recipe: {craftingRecipe}. " +
				                "Perhaps some recipe is missing from the singleton.");
				return;
			}

			SendTo(
				connectedPlayer, new NetMessage
				{
					CraftingRecipeIndex = craftingRecipeIndexToSend
				}
			);
		}
	}
}