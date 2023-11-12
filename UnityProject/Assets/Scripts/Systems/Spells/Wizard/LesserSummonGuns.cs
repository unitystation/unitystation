using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using AddressableReferences;
using Logs;

namespace Systems.Spells.Wizard
{
	/// <summary>
	/// A wizard spell that spawns several guns via portals around the caster.
	/// </summary>
	public class LesserSummonGuns : Spell
	{
		[Tooltip("What portals to spawn. The guns will appear to drop out of these.")]
		[SerializeField, BoxGroup("References")]
		private GameObject portalPrefab = default;
		[Tooltip("What guns to spawn.")]
		[SerializeField, BoxGroup("References")]
		private GameObject gunPrefab = default;
		[SerializeField, BoxGroup("References")]
		private AddressableAudioSource LesserSummonGunsSFX = null;

		[Tooltip("How many guns to spawn around the caster. Does not include the caster's hand spawn.")]
		[SerializeField, BoxGroup("Settings")]
		private int gunSpawnCount = 4;
		[Tooltip("The maximum radius the guns should be scattered in, centred on the player.")]
		[SerializeField, BoxGroup("Settings")]
		private int scatterRadius = 2;
		[SerializeField, BoxGroup("Settings"), Range(0, 1)]
		private float timeBetweenPortals = 0.3f;

		public override bool CastSpellServer(PlayerInfo caster)
		{
			StartCoroutine(SpawnPortals(caster.Script.WorldPos));

			return SpawnGunInHand(caster);
		}

		private IEnumerator SpawnPortals(Vector3Int centrepoint)
		{
			for (int i = 0; i < gunSpawnCount; i++)
			{
				Vector3Int spawnPosition = GetRandomPosition(centrepoint);
				new SpawnByPortal(gunPrefab, portalPrefab, spawnPosition);
				// TODO: consider using OnObjectSpawn to trigger sound if sound aligns with animation nicely.
				StartCoroutine(DelaySoundFX(spawnPosition));
				yield return WaitFor.Seconds(timeBetweenPortals);
			}
		}

		private IEnumerator DelaySoundFX(Vector3Int position)
		{
			yield return WaitFor.Seconds(0.6f); // TODO: likely needs tweaking; wait for sounds to work again.
			SoundManager.PlayNetworkedAtPos(LesserSummonGunsSFX, position);
		}

		private bool SpawnGunInHand(PlayerInfo caster)
		{
			SpawnResult result = Spawn.ServerPrefab(gunPrefab, caster.Script.WorldPos);
			if (result.Successful)
			{
				GameObject gun = result.GameObject;
				ItemSlot bestSlot = caster.Script.DynamicItemStorage.GetBestHandOrSlotFor(gun);
				Inventory.ServerAdd(gun, bestSlot);

				return true;
			}

			Loggy.LogError($"Failed to spawn {gunPrefab} for {this}!", Category.Spells);
			return false;
		}

		private Vector3Int GetRandomPosition(Vector3Int centrepoint)
		{
			return centrepoint + RandomUtils.RandomAnnulusPoint(0, scatterRadius).CutToInt();
		}
	}
}
