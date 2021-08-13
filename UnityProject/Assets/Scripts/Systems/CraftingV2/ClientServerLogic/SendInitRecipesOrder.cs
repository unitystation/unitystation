using System.Collections.Generic;
using Messages.Server;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	public class SendInitRecipesOrder : ServerMessage<SendInitRecipesOrder.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<int> ServerSideKnownRecipeIds;
		}

		public override void Process(NetMessage netMessage)
		{
			List<CraftingRecipe> serverSideKnownRecipes = new List<CraftingRecipe>();
			foreach (int serverSideKnownRecipeId in netMessage.ServerSideKnownRecipeIds)
			{
				serverSideKnownRecipes.Add(
					CraftingRecipeSingleton.Instance.GetRecipeByIndex(serverSideKnownRecipeId)
				);
			}

			PlayerManager.LocalPlayerScript.PlayerCrafting.InitRecipes(serverSideKnownRecipes);
		}

		public static void SendTo(ConnectedPlayer recipient, List<List<CraftingRecipe>> serverSideKnownRecipes)
		{
			List<int> serverSideKnownRecipeIds = new List<int>();
			foreach (List<CraftingRecipe> recipesInCategory in serverSideKnownRecipes)
			{
				foreach (CraftingRecipe craftingRecipe in recipesInCategory)
				{
					if (craftingRecipe.IndexInSingleton < 0)
					{
						Logger.LogError(
							"The server tried to send the negative recipe index when the server was initiating " +
							$"the recipes of the player: {recipient.Name}. The recipe: {craftingRecipe}. " +
							"Perhaps this recipe is missing from the singleton."
						);
						return;
					}

					if (
						craftingRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes()
					    || CraftingRecipeSingleton.Instance.GetRecipeByIndex(craftingRecipe.IndexInSingleton)
							!= craftingRecipe
					)
					{
						Logger.LogError(
							"The server tried to send the wrong recipe index when the server was initiating " +
							$"the recipes of the player: {recipient.Name}. The recipe: {craftingRecipe}. " +
							"Perhaps this recipe has wrong indexInSingleton that doesn't match a real index in " +
							"the singleton."
						);
						return;
					}

					serverSideKnownRecipeIds.Add(craftingRecipe.IndexInSingleton);
				}
			}

			SendTo(recipient, new NetMessage {ServerSideKnownRecipeIds = serverSideKnownRecipeIds});
		}
	}
}