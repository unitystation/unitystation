using System.Collections;
using UnityEngine;

namespace Systems.Spells.Wizard
{
	public class LesserSummonGuns : Spell
	{
		[Tooltip("What gun to spawn.")]
		[SerializeField]
		private GameObject gunPrefab = default;

		[Tooltip("How many guns to spawn around the caster. Does not include the caster's hand spawn.")]
		[SerializeField]
		private int gunSpawnCount = 3;

		[Tooltip("The maximum radius the guns should be scattered in, centred on the player.")]
		[SerializeField]
		private int scatterRadius = 3;

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			SpawnResult result = Spawn.ServerPrefab(
					gunPrefab, caster.Script.WorldPos,
					localRotation: RandomUtils.RandomRotatation2D(), count: gunSpawnCount + 1, scatterRadius: scatterRadius);

			if (result.Successful == false)
			{
				Logger.LogError($"Failed to spawn {gunPrefab} for {this}!");
				return false;
			}

			GameObject firstGun = result.GameObject;
			ItemSlot bestSlot = caster.Script.ItemStorage.GetBestHandOrSlotFor(firstGun);
			Inventory.ServerAdd(firstGun, bestSlot);

			return true;
		}
	}
}
