using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2.GUI;
using Items;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A server sends the information about all the possible ingredients and tools that may be used for crafting,
	/// 	so client can see craftable recipes in a client's crafting menu.
	/// </summary>
	public class SendPossibleCraftingResources : ServerMessage<SendPossibleCraftingResources.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool IsPlayerAbleToCraft;
			public string JsonedKnownRecipeIndexes;
			public string JsonedPossibleIngredientsIds;
			public string JsonedPossibleToolsIds;
		}

		public override void Process(NetMessage netMessage)
		{
			if (netMessage.IsPlayerAbleToCraft == false)
			{
				CraftingMenu.Instance.RefreshRecipes(
					new List<int>(),
					new List<CraftingIngredient>(),
					new List<ItemAttributesV2>()
				);
			}

			List<int> knownRecipeIndexes
				= JsonConvert.DeserializeObject<List<int>>(netMessage.JsonedKnownRecipeIndexes);
			List<uint> possibleIngredientsIds =
				JsonConvert.DeserializeObject<List<uint>>(netMessage.JsonedPossibleIngredientsIds);
			List<uint> possibleToolsIds =
				JsonConvert.DeserializeObject<List<uint>>(netMessage.JsonedPossibleToolsIds);

			LoadMultipleObjects(possibleIngredientsIds.Concat(possibleToolsIds).ToArray());

			List<CraftingIngredient> possibleIngredients = new List<CraftingIngredient>();
			List<ItemAttributesV2> possibleTools = new List<ItemAttributesV2>();

			for (int i = 0; i < possibleIngredientsIds.Count; i++)
			{
				possibleIngredients.Add(NetworkObjects[i].GetComponent<CraftingIngredient>());
			}

			for (int i = possibleIngredientsIds.Count; i < NetworkObjects.Length; i++)
			{
				possibleTools.Add(NetworkObjects[i].GetComponent<ItemAttributesV2>());
			}

			CraftingMenu.Instance.RefreshRecipes(
				knownRecipeIndexes,
				possibleIngredients,
				possibleTools
			);
		}

		public static void SendTo(
			ConnectedPlayer recipient,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			if (recipient.Script.PlayerCrafting.IsPlayerAbleToCraft() == false)
			{
				// ok there's no need to look at known recipes or possible ingredients or whatever,
				// because player can't craft at all. So we're sending the empty message, where the field
				// isPlayerAbleToCraft will be false.
				SendTo(recipient, new NetMessage());
				return;
			}

			List<int> knownRecipeIndexes = new List<int>();
			List<uint> availableIngredientsIds = new List<uint>();
			List<uint> availableToolsIds = new List<uint>();

			foreach (List<CraftingRecipe> knownRecipesInCategory
				in recipient.Script.PlayerCrafting.KnownRecipesByCategory
			)
			{
				foreach (CraftingRecipe knownRecipe in knownRecipesInCategory)
				{
					if (knownRecipe.IndexInSingleton < 0)
					{
						Logger.LogError(
							"The server tried to send the negative recipe index when the server was trying " +
							$"to tell the client({recipient.Name}) that it knows the recipe({knownRecipe}). " +
							"Perhaps this recipe is missing from the singleton."
						);
						continue;
					}

					if (
						knownRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes()
						|| CraftingRecipeSingleton.Instance.GetRecipeByIndex(knownRecipe.IndexInSingleton)
						!= knownRecipe
					)
					{
						Logger.LogError(
							"The server tried to send the wrong recipe index when the server was trying " +
							$"to tell the client({recipient.Name}) that it knows the recipe({knownRecipe}). " +
							"Perhaps this recipe has wrong indexInSingleton that doesn't match a real index in " +
							"the singleton."
						);
						continue;
					}

					knownRecipeIndexes.Add(knownRecipe.IndexInSingleton);
				}
			}

			foreach (CraftingIngredient possibleIngredient in possibleIngredients)
			{
				if (possibleIngredient.TryGetComponent(out NetworkBehaviour networkBehaviour))
				{
					availableIngredientsIds.Add(networkBehaviour.netId);
				}
			}

			foreach (ItemAttributesV2 possibleTool in possibleTools)
			{
				if (possibleTool.TryGetComponent(out NetworkBehaviour networkBehaviour))
				{
					availableToolsIds.Add(networkBehaviour.netId);
				}
			}

			SendTo(
				recipient,
				new NetMessage
				{
					IsPlayerAbleToCraft = true,
					JsonedKnownRecipeIndexes = JsonConvert.SerializeObject(knownRecipeIndexes),
					JsonedPossibleIngredientsIds = JsonConvert.SerializeObject(availableIngredientsIds),
					JsonedPossibleToolsIds = JsonConvert.SerializeObject(availableToolsIds)
				}
			);
		}
	}
}