using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2.GUI;
using Items;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using Player;

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
			public uint RecipientId;
			public string JsonedPossibleIngredientsIds;
			public string JsonedPossibleToolsIds;
		}

		public override void Process(NetMessage netMessage)
		{
			if (!LoadNetworkObject(netMessage.RecipientId))
			{
				return;
			}

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
				NetworkObject.GetComponent<PlayerCrafting>(),
				possibleIngredients,
				possibleTools
			);
		}

		public static void SendTo(
			ConnectedPlayer connectedPlayer,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			List<uint> availableIngredientsIds = new List<uint>();
			List<uint> availableToolsIds = new List<uint>();

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
				connectedPlayer,
				new NetMessage
				{
					RecipientId = connectedPlayer.Script.netId,
					JsonedPossibleIngredientsIds = JsonConvert.SerializeObject(availableIngredientsIds),
					JsonedPossibleToolsIds = JsonConvert.SerializeObject(availableToolsIds)
				}
			);
		}
	}
}