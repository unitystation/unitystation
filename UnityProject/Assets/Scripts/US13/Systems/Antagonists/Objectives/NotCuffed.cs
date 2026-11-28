using Logs;
using UnityEngine;
using US13.Systems.Inventory;

namespace US13.Systems.Antagonists.Objectives
{
	/// <summary>
	/// Try to be alive and not under arrest at the end of the round.
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/NotCuffed")]
	public class NotCuffed: Objective
	{
		protected override void Setup() { }

		protected override bool CheckCompletion()
		{
			if (Owner == null)
			{
				Loggy.Error("[Objective/NotCuffed] - No owner found! Giving free objective.");
				return true;
			}

			if (Owner.Body == null) return false;
			if (Owner.Body.IsDeadOrGhost) return false;

			//for whatever reason this is null, give the guy the greentext
			if (Owner.Body.TryGetComponent<DynamicItemStorage>(out var dynamicItemStorage) == false) return true;

			foreach (var handCuffs in dynamicItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				if (handCuffs.IsEmpty) continue;

				//If any hands are cuff then we fail
				return false;
			}

			return true;
		}
	}
}