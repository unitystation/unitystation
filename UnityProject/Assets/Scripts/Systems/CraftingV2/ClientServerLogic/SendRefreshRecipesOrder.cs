using System.Collections.Generic;
using Systems.CraftingV2.GUI;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A server sends the information about all the possible reagents that may be used for crafting,
	/// 	so a client can update its recipe button borders.
	/// </summary>
	public class SendRefreshRecipesOrder : ServerMessage<SendRefreshRecipesOrder.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool IsPlayerAbleToCraft;
			public string JsonedPossibleReagents;
		}

		public override void Process(NetMessage netMessage)
		{
			if (netMessage.IsPlayerAbleToCraft == false)
			{
				CraftingMenu.Instance.SetAllRecipesUncraftable();
			}

			List<KeyValuePair<int, float>> possibleReagents =
				JsonConvert.DeserializeObject<List<KeyValuePair<int, float>>>(netMessage.JsonedPossibleReagents);

			CraftingMenu.Instance.RefreshRecipes(
				PlayerManager.LocalPlayerScript.PlayerCrafting.GetPossibleIngredients(NetworkSide.Client),
				PlayerManager.LocalPlayerScript.PlayerCrafting.GetPossibleTools(NetworkSide.Client),
				possibleReagents
			);
		}

		public static void SendTo(ConnectedPlayer recipient)
		{
			if (recipient.Script.PlayerCrafting.IsPlayerAbleToCraft() == false)
			{
				// ok there's no need to look at possible reagents, because player can't craft at all.
				// So we're sending the empty message, where the field isPlayerAbleToCraft will be false.
				SendTo(recipient, new NetMessage());
				return;
			}

			SendTo(
				recipient,
				new NetMessage
				{
					IsPlayerAbleToCraft = true,
					JsonedPossibleReagents = JsonConvert.SerializeObject(
						recipient.Script.PlayerCrafting.GetPossibleReagents()
					)
				}
			);
		}
	}
}