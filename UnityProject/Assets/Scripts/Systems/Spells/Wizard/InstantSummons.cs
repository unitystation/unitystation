using System.Collections;
using UnityEngine;

namespace Systems.Spells.Wizard
{
	public class InstantSummons : Spell
	{
		private ConnectedPlayer caster;
		private Pickupable markedItem;
		private bool isSet = false;

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			this.caster = caster;

			if (isSet && markedItem == null)
			{
				Chat.AddExamineMsgFromServer(caster, $"You sense your marked item has been destroyed!");
				return isSet = false;
			}

			if (isSet)
			{
				return TrySummon();
			}
			else
			{
				TryAddMark();
				return false;
			}
		}

		private bool TrySummon()
		{
			// backpack, box, player etc, or the item itself if not in storage
			GameObject objectToSummon = GetItemOrRootStorage(markedItem.gameObject);

			// check to make sure not already on player
			if (objectToSummon == caster.GameObject)
			{
				RemoveMark(markedItem.gameObject);
				return false; // Summon not triggered; don't go on cooldown.
			}

			// crate, closet, morgue etc
			GameObject rootContainer = GetRootContainer(objectToSummon);
			if (rootContainer != null)
			{
				objectToSummon = rootContainer;
			}

			SummonObject(objectToSummon);
			return true;
		}

		private void SummonObject(GameObject summonedObject)
		{
			string summonedName = summonedObject.ExpensiveName();
			Chat.AddActionMsgToChat(summonedObject,
				"<color=red>You feel a magical force transposing you!</color>",
				$"<color=red>The {summonedName} suddenly disappears!</color>");

			TeleportObjectToPosition(summonedObject, caster.Script.WorldPos);

			if (summonedObject.TryGetComponent<Pickupable>(out var pickupable))
			{
				ItemSlot slot = caster.Script.DynamicItemStorage.GetBestHandOrSlotFor(summonedObject);
				Inventory.ServerAdd(pickupable, slot);

				Chat.AddActionMsgToChat(caster.GameObject, $"The {summonedName} appears in your hand!",
					$"<color=red>A {summonedName} suddenly appears in {caster.Script.visibleName}'s hand!</color>");
			}
			else
			{
				string message = $"<color=red>The {summonedName} suddenly appears!</color>";
				Chat.AddActionMsgToChat(caster.GameObject, message, message);
			}
		}

		private void TryAddMark()
		{
			DynamicItemStorage playerStorage = caster.Script.DynamicItemStorage;

			ItemSlot activeHand = playerStorage.GetActiveHandSlot();
			if (activeHand.IsOccupied)
			{
				AddMark(activeHand.Item);
				return;
			}

			foreach (var leftHand in playerStorage.GetNamedItemSlots(NamedSlot.leftHand))
			{
				if (leftHand != activeHand && leftHand.IsOccupied)
				{
					AddMark(leftHand.Item);
					return;
				}
			}

			foreach (var rightHand in playerStorage.GetNamedItemSlots(NamedSlot.rightHand))
			{
				if (rightHand != activeHand && rightHand.IsOccupied)
				{
					AddMark(rightHand.Item);
					return;
				}
			}

			Chat.AddExamineMsgFromServer(caster, "You aren't holding anything that can be marked for recall!");
		}

		private void AddMark(Pickupable item)
		{
			markedItem = item;
			isSet = true;
			Chat.AddExamineMsgFromServer(caster, $"You mark the {item.gameObject.ExpensiveName()} for recall.");
		}

		private void RemoveMark(GameObject item)
		{
			markedItem = null;
			isSet = false;
			Chat.AddExamineMsgFromServer(caster,
				$"You remove the mark on the {item.ExpensiveName()} to use elsewhere.");
		}

		#region Generic Helpers

		private GameObject GetItemOrRootStorage(GameObject item)
		{
			if (item.TryGetComponent<Pickupable>(out var pickupable) && pickupable.ItemSlot != null)
			{
				var RootStorage = pickupable.ItemSlot.GetRootStorageOrPlayer();
				return RootStorage.gameObject;
			}

			return item;
		}

		private GameObject GetRootContainer(GameObject childItem)
		{
			ObjectBehaviour objBehaviour = childItem.GetComponent<ObjectBehaviour>();
			int i = 0;
			while (i < 10 && objBehaviour != null && objBehaviour.parentContainer != null)
			{
				if (objBehaviour.parentContainer.TryGetComponent<ObjectBehaviour>(out var newBehaviour))
				{
					objBehaviour = newBehaviour;
				}

				i++;
			}

			return objBehaviour == null ? null : objBehaviour.gameObject;
		}

		private void TeleportObjectToPosition(GameObject teleportingObject, Vector3 worldPosition)
		{
			if (teleportingObject.TryGetComponent<CustomNetTransform>(out var netTransform))
			{
				netTransform.AppearAtPositionServer(worldPosition);
			}
			else if (teleportingObject.TryGetComponent<PlayerSync>(out var playerSync))
			{
				playerSync.AppearAtPositionServer(worldPosition);
			}
			else
			{
				teleportingObject.transform.position = worldPosition;
			}
		}

		#endregion
	}
}