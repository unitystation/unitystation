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
			// if the player is trying to learn new recipe without initiated CraftingMenu...
			if (CraftingMenu.Instance == null)
			{
				// ...then we will add a new recipe button when the player will have opened the crafting menu
				// (in other words when the CraftingMenu.Awake() method will be called)
				return;
			}

			// ok, the crafting menu is already initiated, so it's safe to add new buttons to it
			CraftingMenu.Instance.OnPlayerLearnedRecipe(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.CraftingRecipeIndex]
			);
		}

		public static void SendTo(ConnectedPlayer connectedPlayer, CraftingRecipe craftingRecipe)
		{
			int craftingRecipeIndexToSend = CraftingRecipeSingleton
				.Instance
				.StoredCraftingRecipes
				.IndexOf(craftingRecipe);

			if (craftingRecipeIndexToSend < 0)
			{
				Logger.LogError($"The server tried to send the negative recipe index when {connectedPlayer.Name} " +
				                $"had tried to learn this recipe: {craftingRecipe}. " +
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