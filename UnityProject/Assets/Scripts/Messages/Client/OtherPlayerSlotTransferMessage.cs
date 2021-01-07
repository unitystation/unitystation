using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;
public class OtherPlayerSlotTransferMessage : ClientMessage
{
	public uint PlayerStorage;
	public int PlayerSlotIndex;
	public NamedSlot PlayerNamedSlot;
	public uint TargetStorage;
	public int TargetSlotIndex;
	public NamedSlot TargetNamedSlot;

	public override void Process()
	{
		LoadMultipleObjects(new uint[]{PlayerStorage, TargetStorage});
		if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

		var playerSlot = ItemSlot.Get(NetworkObjects[0].GetComponent<ItemStorage>(), PlayerNamedSlot, PlayerSlotIndex);
		var targetSlot = ItemSlot.Get(NetworkObjects[1].GetComponent<ItemStorage>(), TargetNamedSlot, TargetSlotIndex);

		var playerScript = SentByPlayer.Script;
		var playerObject = playerScript.gameObject;
		var targetObject = targetSlot.Player.gameObject;

		if (!Validation(playerSlot, targetSlot, playerScript, targetObject, NetworkSide.Server))
			return;

		int speed;
		if (!targetSlot.IsEmpty)
		{
			Chat.AddActionMsgToChat(playerObject, $"You try to remove {targetObject.ExpensiveName()}'s {targetSlot.ItemObject.ExpensiveName()}...",
				$"{playerObject.ExpensiveName()} tries to remove {targetObject.ExpensiveName()}'s {targetSlot.ItemObject.ExpensiveName()}.");
			speed = 3;
		}
		else
		{
			Chat.AddActionMsgToChat(playerObject, $"You try to put the {playerSlot.ItemObject.ExpensiveName()} on {targetObject.ExpensiveName()}...",
				$"{playerObject.ExpensiveName()} tries to put the {playerSlot.ItemObject.ExpensiveName()} on {targetObject.ExpensiveName()}.");
			speed = 1;
		}

		var progressAction = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.ItemTransfer), FinishTransfer);
		progressAction.ServerStartProgress(targetObject.RegisterTile(), speed, playerObject);


		void FinishTransfer()
		{
			if (!targetSlot.IsEmpty)
			{
				if (playerSlot.IsEmpty)
				{
					Inventory.ServerTransfer(targetSlot, playerSlot);
				}
				else
				{
					Inventory.ServerDrop(targetSlot);
				}
			}
			else
			{
				Inventory.ServerTransfer(playerSlot, targetSlot);
			}
		}
	}

	private static bool Validation(ItemSlot playerSlot, ItemSlot targetSlot, PlayerScript playerScript, GameObject target, NetworkSide networkSide)
	{
		if (!playerSlot.IsEmpty && targetSlot.IsEmpty)
		{
			if (!Validations.CanPutItemToSlot(PlayerManager.LocalPlayerScript, targetSlot, playerSlot.Item, NetworkSide.Client, examineRecipient: PlayerManager.LocalPlayerScript.gameObject))
			{
				return false;
			}
		}
		if (!Validations.CanApply(playerScript, target, networkSide))
		{
			return false;
		}
		return true;
	}

	public static void Send(ItemSlot playerSlot, ItemSlot targetSlot)
	{
		if (!Validation(playerSlot, targetSlot, PlayerManager.LocalPlayerScript, targetSlot.Player.gameObject, NetworkSide.Client))
			return;

		OtherPlayerSlotTransferMessage msg = new OtherPlayerSlotTransferMessage
		{
			PlayerStorage = playerSlot.ItemStorageNetID,
			PlayerSlotIndex = playerSlot.SlotIdentifier.SlotIndex,
			PlayerNamedSlot = playerSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
			TargetStorage = targetSlot.ItemStorageNetID,
			TargetSlotIndex = targetSlot.SlotIdentifier.SlotIndex,
			TargetNamedSlot = targetSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
		};
		msg.Send();
	}
}