using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;
public class OtherPlayerSlotTransferMessage : ClientMessage
{
	public class OtherPlayerSlotTransferMessageNetMessage : NetworkMessage
	{
		public uint PlayerStorage;
		public int PlayerSlotIndex;
		public NamedSlot PlayerNamedSlot;
		public uint TargetStorage;
		public int TargetSlotIndex;
		public NamedSlot TargetNamedSlot;
		public bool IsGhost;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as OtherPlayerSlotTransferMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadMultipleObjects(new uint[]{newMsg.PlayerStorage, newMsg.TargetStorage});
		if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

		var playerSlot = ItemSlot.Get(NetworkObjects[0].GetComponent<ItemStorage>(), newMsg.PlayerNamedSlot, newMsg.PlayerSlotIndex);
		var targetSlot = ItemSlot.Get(NetworkObjects[1].GetComponent<ItemStorage>(), newMsg.TargetNamedSlot, newMsg.TargetSlotIndex);

		var playerScript = SentByPlayer.Script;
		var playerObject = playerScript.gameObject;
		var targetObject = targetSlot.Player.gameObject;

		if (newMsg.IsGhost)
		{
			if (playerScript.IsGhost && PlayerList.Instance.IsAdmin(playerScript.connectedPlayer.UserId))
			{
				FinishTransfer();
			}
			return;
		}

		if (!Validation(playerSlot, targetSlot, playerScript, targetObject, NetworkSide.Server, newMsg.IsGhost))
			return;

		int speed;
		if (!targetSlot.IsEmpty)
		{
			Chat.AddActionMsgToChat(playerObject, $"You try to remove {targetObject.ExpensiveName()}'s {targetSlot.ItemObject.ExpensiveName()}...",
				$"{playerObject.ExpensiveName()} tries to remove {targetObject.ExpensiveName()}'s {targetSlot.ItemObject.ExpensiveName()}.");
			speed = 3;
		}
		else if (playerSlot.IsOccupied)
		{
			Chat.AddActionMsgToChat(playerObject, $"You try to put the {playerSlot.ItemObject.ExpensiveName()} on {targetObject.ExpensiveName()}...",
				$"{playerObject.ExpensiveName()} tries to put the {playerSlot.ItemObject.ExpensiveName()} on {targetObject.ExpensiveName()}.");
			speed = 1;
		}
		else return;

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

	private static bool Validation(ItemSlot playerSlot, ItemSlot targetSlot, PlayerScript playerScript, GameObject target, NetworkSide networkSide, bool isGhost)
	{
		if (!playerSlot.IsEmpty && targetSlot.IsEmpty)
		{
			if(!Validations.CanFit(targetSlot, playerSlot.Item, NetworkSide.Client, examineRecipient: playerScript.gameObject))
			{
				return false;
			}
		}
		if (!isGhost && !Validations.CanApply(playerScript, target, networkSide))
		{
			return false;
		}
		return true;
	}

	public static void Send(ItemSlot playerSlot, ItemSlot targetSlot, bool isGhost)
	{
		if (!Validation(playerSlot, targetSlot, PlayerManager.LocalPlayerScript, targetSlot.Player.gameObject, NetworkSide.Client, isGhost))
			return;

		OtherPlayerSlotTransferMessageNetMessage msg = new OtherPlayerSlotTransferMessageNetMessage
		{
			PlayerStorage = playerSlot.ItemStorageNetID,
			PlayerSlotIndex = playerSlot.SlotIdentifier.SlotIndex,
			PlayerNamedSlot = playerSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
			TargetStorage = targetSlot.ItemStorageNetID,
			TargetSlotIndex = targetSlot.SlotIdentifier.SlotIndex,
			TargetNamedSlot = targetSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
			IsGhost = isGhost
		};
		new OtherPlayerSlotTransferMessage().Send(msg);
	}
}