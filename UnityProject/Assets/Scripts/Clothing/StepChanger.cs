using ScriptableObjects.Audio;
using UnityEngine;

namespace Clothing
{
	/// <summary>
	/// Handles the change of StepType when players equip or unequip this item
	/// </summary>
	public class StepChanger : MonoBehaviour, IServerInventoryMove
	{
		[SerializeField]
		[Tooltip("Pack of sounds that this StepChanger will replace")]
		private FloorSounds soundChange;

		[SerializeField][Tooltip("Slot where this StepChanger should take effect.")]
		private NamedSlot slot = NamedSlot.feet;

		[SerializeField]
		[Tooltip("If true, this step changer will have priority over other step changers when putting on")]
		private bool hasPriority;

		private Mind mind;

		private bool IsPuttingOn(InventoryMove info)
		{
			return info.ToSlot != null &&
			       info.ToSlot.NamedSlot == slot &&
			       info.ToRootPlayer;
		}

		private bool IsTakingOff(InventoryMove info)
		{
			return info.FromSlot != null &&
			       info.FromSlot.NamedSlot == slot &&
			       info.FromPlayer;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (soundChange == null) return;

			if (IsPuttingOn(info))
			{
				mind = info.ToPlayer.OrNull()?.PlayerScript.OrNull()?.mind;
				if (mind is null) return;

				//Stepchanger doesn't have priority and mind already has sounds set. Assume they have priority.
				if (hasPriority == false && mind.StepSound) return;
				mind.StepSound = soundChange;
			}

			if (IsTakingOff(info))
			{
				mind = info.FromPlayer.OrNull()?.PlayerScript.OrNull()?.mind;
				if (mind is null) return;

				//Stepchanger doesn't have priority and mind has another sound pack set. Assume they have priority.
				if (hasPriority == false && mind.StepSound != soundChange) return;
				mind.StepSound = null;
			}
		}
	}
}
