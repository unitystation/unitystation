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
			ItemStorage itemStorage = Owner.body.GetComponent<ItemStorage>();

			//for whatever reason this is null, give the guy the greentext
			if (itemStorage == null)
			{
				return true;
			}

			return itemStorage.GetNamedItemSlot(NamedSlot.handcuffs).IsEmpty;
		}
	}
}