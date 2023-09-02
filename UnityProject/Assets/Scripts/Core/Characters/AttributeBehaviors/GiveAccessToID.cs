using System.Collections.Generic;
using Logs;
using Systems.Clearance;
using UnityEngine;

namespace Core.Characters.AttributeBehaviors
{
	/// <summary>
	/// Behavior responsible for setting up access on player's IDs if found.
	/// TODO: Should this be renamed to SetupIDs instead?
	/// </summary>
	public class GiveAccessToID : CharacterAttributeBehavior
	{
		[SerializeField] private List<Clearance> clearance = new();
		[SerializeField] private List<Clearance> lowPopClearance = new();
		[SerializeField] private bool useCharacterSettingsName = true;

		public override void Run(GameObject characterBody)
		{
			var inventory = characterBody.GetComponent<DynamicItemStorage>();
			if (inventory == null)
			{
				Loggy.LogWarning("[Attributes/Behaviors/GiveAccessToID] - " +
				                  "Attempted to access player inventory but could not find it!");
				return;
			}

			var ids = inventory.GetNamedItemSlots(NamedSlot.id);
			foreach (var slot in ids)
			{
				if(slot.IsEmpty) continue;
				if(slot.ItemObject.TryGetComponent<IDCard>(out var idCard) == false) continue;

				idCard.ClearanceSource.ServerSetClearance(clearance);
				idCard.ClearanceSource.ServerSetLowPopClearance(lowPopClearance);
				if(useCharacterSettingsName)
					idCard.ServerSetRegisteredName(gameObject.GetComponent<PlayerScript>().characterSettings.Name);
			}
		}
	}
}