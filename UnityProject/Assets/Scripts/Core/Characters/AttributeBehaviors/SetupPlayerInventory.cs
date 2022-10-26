using Systems.Storage;
using UnityEngine;

namespace Core.Characters.AttributeBehaviors
{
	public class SetupPlayerInventory : CharacterAttributeBehavior
	{
		[SerializeField] private PlayerSlotStoragePopulator populator;

		public override void Run(GameObject characterBody)
		{
			characterBody.GetComponent<DynamicItemStorage>()?.SetUpFromPopulator(populator);
		}
	}
}