using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Try to be alive and not under arrest at the end of the round.
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/NotCuffed")]
	public class NotCuffed: Objective
	{
		protected override void Setup() {}

		protected override bool CheckCompletion()
		{
			DynamicItemStorage dynamicItemStorage = Owner.body.GetComponent<DynamicItemStorage>();

			//for whatever reason this is null, give the guy the greentext
			if (dynamicItemStorage == null) return true;

			foreach (var handCuffs in dynamicItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				if(handCuffs.IsEmpty) continue;

				//If any hands are cuff then we fail
				return false;
			}

			return true;
		}
	}
}