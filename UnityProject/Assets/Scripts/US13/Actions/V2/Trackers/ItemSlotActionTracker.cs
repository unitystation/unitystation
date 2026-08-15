using Logs;
using NaughtyAttributes;
using SecureStuff;
using UnityEngine;
using US13.Systems.Inventory;

namespace US13.Actions.V2.Trackers
{
	public class ItemSlotActionTracker : MonoBehaviour, IActionButtonTracker, IServerInventoryMove
	{
		[field:SerializeField] public SerializableDictionary<ActionButtonData, SerializedAction> ActionData { get; set; }

		[SerializeField] private bool requiresSpecificSlots = false;
		[ShowIf(nameof(requiresSpecificSlots)), SerializeField] private NamedSlot possibleSlots = NamedSlot.eyes;

		public ActionManager TargetActionManager { get; set; }


		private Pickupable pickupable;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			if (pickupable == null)
			{
				Debug.LogError("ItemSlotActionTracker requires a Pickupable component.");
				return;
			}

			// lets the manager know what object this action is on.
			foreach (var action in ActionData.Keys)
			{
				action.ObjectRelatedToThisAction = gameObject;
			}
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.ToRootPlayer == null)
			{
				WhenHolderIsOutOfRange();
			}
			else
			{
				if (pickupable?.ItemSlot?.NamedSlot != null) //TODO someone think of a better system but this works for now ( basically are we on the root level of the inventory )
				{
					WhenHolderIsOutOfRange();
					TargetActionManager = info.ToRootPlayer.PlayerScript.PlayerButtonedActions;
					WhenHolderIsInRange();
				}
				else
				{
					WhenHolderIsOutOfRange();
				}
			}
		}

		public void WhenHolderIsInRange()
		{
			if (TargetActionManager == null)
			{
				Loggy.Error($"Attempted to register actions for {gameObject.name}, but TargetActionManager is null.");
				return;
			}


			//If this item needs to be in a specific slot but isn't, don't register the action
			if (requiresSpecificSlots &&
			    (pickupable.ItemSlot.NamedSlot == null
			     || possibleSlots.HasFlag(pickupable.ItemSlot.NamedSlot) == false)) return;

			foreach (var data in ActionData.Keys)
			{
				TargetActionManager?.RegisterNewAction(data, ActionData[data].Invoke);
			}
		}

		public void WhenHolderIsOutOfRange()
		{
			if (TargetActionManager == null) return;
			foreach (var actionData in ActionData.Keys)
			{
				TargetActionManager?.UnregisterAction(actionData);
			}
			TargetActionManager = null;
		}
	}
}