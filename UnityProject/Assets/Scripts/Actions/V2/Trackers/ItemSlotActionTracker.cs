using System;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;

namespace Actions.V2.Trackers
{
	public class ItemSlotActionTracker : MonoBehaviour, IActionButtonTracker
	{
		[field:SerializeField] public SerializableDictionary<ActionButtonData, SerializedAction> ActionData { get; set; }
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

			pickupable.OnItemSlotChanged.CachedAction += OnInventoryMovementCheck;
		}

		private void OnDestroy()
		{
			if (pickupable != null)
			{
				pickupable.OnItemSlotChanged.CachedAction -= OnInventoryMovementCheck;
			}
		}

		private void OnInventoryMovementCheck()
		{
			if (pickupable.ItemSlot == null)
			{
				WhenHolderIsOutOfRange();
				return;
			}
			if (pickupable.ItemSlot.Player != null)
			{
				WhenHolderIsOutOfRange();
				TargetActionManager = pickupable.ItemSlot.Player.PlayerScript.PlayerButtonedActions;
				WhenHolderIsInRange();
			}
			else
			{
				WhenHolderIsOutOfRange();
			}
		}

		public void WhenHolderIsInRange()
		{
			foreach (var data in ActionData.Keys)
			{
				TargetActionManager.RegisterNewAction(data, ActionData[data].Invoke);
			}
		}

		public void WhenHolderIsOutOfRange()
		{
			if (TargetActionManager == null) return;
			foreach (var actionData in ActionData.Keys)
			{
				TargetActionManager.UnregisterAction(actionData);
			}
			TargetActionManager = null;
		}
	}
}