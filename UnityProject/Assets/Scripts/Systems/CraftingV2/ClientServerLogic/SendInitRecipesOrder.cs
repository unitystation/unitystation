using System.Collections.Generic;
using Logs;
using Messages.Server;
using Mirror;
using Player;
using UnityEngine;

namespace Systems.CraftingV2.ClientServerLogic
{
	public class SendInitRecipesOrder : ServerMessage<SendInitRecipesOrder.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Body;
			public List<int> ServerSideKnownRecipeIds;
		}

		public override void Process(NetMessage netMessage)
		{

			LoadNetworkObject(netMessage.Body);
			List<CraftingRecipe> serverSideKnownRecipes = new List<CraftingRecipe>();
			foreach (int serverSideKnownRecipeId in netMessage.ServerSideKnownRecipeIds)
			{
				serverSideKnownRecipes.Add(
					CraftingRecipeSingleton.Instance.GetRecipeByIndex(serverSideKnownRecipeId)
				);
			}

			NetworkObject.GetComponent<PlayerCrafting>().InitRecipes(serverSideKnownRecipes);
		}

		public static void SendTo(PlayerInfo recipient, List<List<CraftingRecipe>> serverSideKnownRecipes, GameObject Body)
		{
			List<int> serverSideKnownRecipeIds = new List<int>();
			foreach (List<CraftingRecipe> recipesInCategory in serverSideKnownRecipes)
			{
				foreach (CraftingRecipe craftingRecipe in recipesInCategory)
				{
					if (craftingRecipe.IndexInSingleton < 0)
					{
						Loggy.LogError(
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
						Loggy.LogError(
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

			SendTo(recipient, new NetMessage {ServerSideKnownRecipeIds = serverSideKnownRecipeIds, Body =  Body.NetId()});
		}
	}
}