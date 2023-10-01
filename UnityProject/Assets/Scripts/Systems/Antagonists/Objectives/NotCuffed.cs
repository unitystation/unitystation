using Logs;
using UnityEngine;

namespace Antagonists
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
				Loggy.LogError("[Objective/NotCuffed] - No owner found! Giving free objective.");
				return true;
			}
			//for whatever reason this is null, give the guy the greentext
			if (Owner.Body == null || Owner.Body.TryGetComponent<DynamicItemStorage>(out var dynamicItemStorage) == false) return true;

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