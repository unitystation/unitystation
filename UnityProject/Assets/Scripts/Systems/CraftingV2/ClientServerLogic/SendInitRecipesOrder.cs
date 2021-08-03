using System.Collections.Generic;
using Systems.CraftingV2.GUI;
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
					CraftingRecipeSingleton.Instance.StoredCraftingRecipes[serverSideKnownRecipeId]
				);
			}
			CraftingMenu.Instance.InitRecipes(serverSideKnownRecipes);
		}

		public static void SendTo(ConnectedPlayer recipient, List<List<CraftingRecipe>> serverSideKnownRecipes)
		{
			List<int> serverSideKnownRecipeIds = new List<int>();
			foreach (List<CraftingRecipe> recipesInCategory in serverSideKnownRecipes)
			{
				foreach (CraftingRecipe craftingRecipe in recipesInCategory)
				{
					serverSideKnownRecipeIds.Add(
						CraftingRecipeSingleton.Instance.StoredCraftingRecipes.IndexOf(craftingRecipe)
					);
				}
			}

			SendTo(recipient, new NetMessage {ServerSideKnownRecipeIds = serverSideKnownRecipeIds});
		}
	}
}