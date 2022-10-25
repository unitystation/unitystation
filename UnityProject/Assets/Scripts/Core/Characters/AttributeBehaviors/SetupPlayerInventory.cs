using Systems.Storage;
using UnityEngine;

namespace Core.Characters.AttributeBehaviors
{
	public class SetupPlayerInventory : CharacterAttributeBehavior
	{
		[SerializeField] private PlayerSlotStoragePopulator populator;

		public override void Run(PlayerScript script)
		{
			script.GetComponent<DynamicItemStorage>()?.SetUpFromPopulator(populator);
		}
	}
}