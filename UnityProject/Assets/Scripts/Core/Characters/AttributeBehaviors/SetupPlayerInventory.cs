using Systems.Storage;
using UnityEngine;

namespace Core.Characters.AttributeBehaviors
{
	/// <summary>
	/// Using data from a player inventory populator SO, populate the player's inventory using that data.
	/// </summary>
	public class SetupPlayerInventory : CharacterAttributeBehavior
	{
		[SerializeField] private PlayerSlotStoragePopulator populator;

		public override void Run(GameObject characterBody)
		{
			// The ? makes sure to not cause NREs in-case this attribute was wrongly assigned to a player
			// Prefab that does not support or have a dynamic player inventory.
			characterBody.GetComponent<DynamicItemStorage>()?.SetUpFromPopulator(populator);
		}
	}
}