using Mirror;
using UnityEngine;

namespace Messages.Client
{
	public class OtherPlayerSlotTransferMessage : ClientMessage<OtherPlayerSlotTransferMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint PlayerStorage;
			public int PlayerSlotIndex;
			public int StorageIndexOnPlayer;
			public NamedSlot PlayerNamedSlot;
			public uint TargetStorage;
			public int TargetSlotIndex;
			public int StorageIndexOnGameObject;
			public NamedSlot TargetNamedSlot;
			public bool IsGhost;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[]{msg.PlayerStorage, msg.TargetStorage});
			if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

			var playerSlot = ItemSlot.Get(NetworkObjects[0].GetComponents<ItemStorage>()[msg.StorageIndexOnPlayer], msg.PlayerNamedSlot, msg.PlayerSlotIndex);
			var targetSlot = ItemSlot.Get(NetworkObjects[1].GetComponents<ItemStorage>()[msg.StorageIndexOnGameObject], msg.TargetNamedSlot, msg.TargetSlotIndex);

			var playerScript = SentByPlayer.Script;
			var playerObject = playerScript.gameObject;
			var targetObject = targetSlot.Player.gameObject;

			if (msg.IsGhost)
			{
				if (playerScript.IsGhost && PlayerList.Instance.IsAdmin(playerScript.connectedPlayer.UserId))
				{
					FinishTransfer();
				}
				return;
			}

			if (!Validation(playerSlot, targetSlot, playerScript, targetObject, NetworkSide.Server, msg.IsGhost))
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

			NetMessage msg = new NetMessage
			{
				PlayerStorage = playerSlot.ItemStorageNetID,
				PlayerSlotIndex = playerSlot.SlotIdentifier.SlotIndex,
				PlayerNamedSlot = playerSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
				TargetStorage = targetSlot.ItemStorageNetID,
				TargetSlotIndex = targetSlot.SlotIdentifier.SlotIndex,
				TargetNamedSlot = targetSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
				IsGhost = isGhost
			};

			msg.StorageIndexOnPlayer = 0;
			foreach (var itemStorage in NetworkIdentity.spawned[playerSlot.ItemStorageNetID].GetComponents<ItemStorage>())
			{
				if (itemStorage == playerSlot.ItemStorage)
				{
					break;
				}

				msg.StorageIndexOnPlayer++;
			}

			msg.StorageIndexOnGameObject = 0;
			foreach (var itemStorage in NetworkIdentity.spawned[targetSlot.ItemStorageNetID].GetComponents<ItemStorage>())
			{
				if (itemStorage == targetSlot.ItemStorage)
				{
					break;
				}

				msg.StorageIndexOnGameObject++;
			}


			Send(msg);
		}
	}
}