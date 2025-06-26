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

			pickupable.OnInventoryMoveServerEvent.AddListener(OnInventoryMovementCheck);
		}

		private void OnDestroy()
		{
			if (pickupable != null)
			{
				pickupable.OnInventoryMoveServerEvent.RemoveListener(OnInventoryMovementCheck);
			}
		}

		private void OnInventoryMovementCheck(GameObject inventory)
		{
			if (inventory == null) return;
			if (inventory.TryGetComponent<ActionManager>(out ActionManager actionManager))
			{
				TargetActionManager = actionManager;
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
			foreach (var actionData in ActionData.Keys)
			{
				TargetActionManager.UnregisterAction(actionData);
			}
			TargetActionManager = null;
		}
	}
}