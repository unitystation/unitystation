using System.Collections.Generic;
using Items;
using Logs;
using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A client asks a server to craft a client's selected recipe. This ClientMessage is designed to handle it.
	/// </summary>
	public class RequestStartCraftingAction : ClientMessage<RequestStartCraftingAction.NetMessage>
	{
		private CraftingActionParameters craftingActionParameters = new CraftingActionParameters(
			true,
			FeedbackType.GiveAllFeedback
		);

		public struct NetMessage : NetworkMessage
		{
			// recipe index in the recipes singleton
			public int CraftingRecipeIndex;
			public bool IsRecipeIndexWrong;
		}

		public override void Process(NetMessage netMessage)
		{
			if (
				Cooldowns.TryStartServer(
					SentByPlayer.Script,
					CommonCooldowns.Instance.Interaction
				) == false
			)
			{
				return;
			}

			if (netMessage.CraftingRecipeIndex < 0)
			{
				Loggy.LogError(
					$"Received the negative recipe index when {SentByPlayer.Name} " +
					"had tried to craft something. Perhaps some recipe is missing from the singleton."
				);
				return;
			}

			if (netMessage.IsRecipeIndexWrong)
			{
				Loggy.LogError(
					$"Received the wrong recipe index when {SentByPlayer.Name} had tried to craft something. " +
					"Perhaps some recipe has wrong indexInSingleton that doesn't match a real index in the singleton."
				);
				return;
			}

			// at the moment we already know that there are enough ingredients and
			// tools(checked on the client side), so we'll ignore them.
			SentByPlayer.Script.PlayerCrafting.TryToStartCrafting(
				CraftingRecipeSingleton.Instance.GetRecipeByIndex(netMessage.CraftingRecipeIndex),
				null,
				null,
				SentByPlayer.Script.PlayerCrafting.GetReagentContainers(),
				craftingActionParameters
			);
		}

		public static void Send(CraftingRecipe craftingRecipe)
		{
			if (
				Cooldowns.TryStartClient(
					PlayerManager.LocalPlayerScript,
					CommonCooldowns.Instance.Interaction
				) == false
			)
			{
				return;
			}

			// if sending a wrong recipe index...
			if (
				craftingRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes()
			    || CraftingRecipeSingleton.Instance.GetRecipeByIndex(craftingRecipe.IndexInSingleton)
			    != craftingRecipe
			)
			{
				Send(new NetMessage
				{
					CraftingRecipeIndex = craftingRecipe.IndexInSingleton,
					IsRecipeIndexWrong = true
				});
			}

			Send(new NetMessage
			{
				CraftingRecipeIndex = craftingRecipe.IndexInSingleton,
				IsRecipeIndexWrong = false
			});
		}
	}
}