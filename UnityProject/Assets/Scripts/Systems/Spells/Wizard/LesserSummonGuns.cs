using System.Collections;
using UnityEngine;
using Mirror;
using AddressableReferences;

namespace Systems.Spells.Wizard
{
	public class LesserSummonGuns : Spell
	{
		[SerializeField] private AddressableAudioSource LesserSummonGunsSFX = null;
		
		private const float TIME_BETWEEN_PORTALS = 0.5f;
		private const int PORTAL_HEIGHT = 2;
		private const float PORTAL_OPEN_TIME = 0.8f;
		private const float PORTAL_CLOSE_TIME = 0.5f;
		private const float PORTAL_ACTIVE_TIME = 1;
		private const float GUN_FALLING_TIME = 0.7f;

		[Header("References")]
		[Tooltip("What portals to spawn. The guns will appear to drop out of these.")]
		[SerializeField]
		private GameObject portalPrefab = default;

		[Tooltip("What guns to spawn.")]
		[SerializeField]
		private GameObject gunPrefab = default;

		[Header("Settings")]
		[Tooltip("How many guns to spawn around the caster. Does not include the caster's hand spawn.")]
		[SerializeField]
		private int gunSpawnCount = 4;

		[Tooltip("The maximum radius the guns should be scattered in, centred on the player.")]
		[SerializeField]
		private int scatterRadius = 2;

		private Vector3Int epicentre;

		[SyncVar(hook = nameof(SyncLatestPortal))]
		private GameObject latestPortal;

		[SyncVar(hook = nameof(SyncLatestGun))]
		private GameObject latestGun;

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			epicentre = caster.Script.WorldPos;
			StartCoroutine(SpawnPortals());

			return SpawnGunInHand(caster);
		}

		private IEnumerator SpawnPortals()
		{
			for (int i = 0; i < gunSpawnCount; i++)
			{
				StartCoroutine(SpawnPortal());
				yield return WaitFor.Seconds(TIME_BETWEEN_PORTALS);
			}
		}

		private IEnumerator SpawnPortal()
		{
			Vector3Int randomPos = GetRandomPosition();
			var portalPos = randomPos;
			portalPos.y += PORTAL_HEIGHT; // Spawn portal two tiles above the landing zone.
			StartCoroutine(DelaySoundFX(portalPos));

			var newPortal = Spawn.ServerPrefab(portalPrefab, portalPos).GameObject;
			latestPortal = newPortal;

			yield return WaitFor.Seconds(PORTAL_OPEN_TIME + (PORTAL_ACTIVE_TIME / 2));
			latestGun = Spawn.ServerPrefab(gunPrefab, randomPos).GameObject;

			yield return WaitFor.Seconds(PORTAL_OPEN_TIME + PORTAL_ACTIVE_TIME + PORTAL_CLOSE_TIME + 1); // Wait a good while first.
			Despawn.ServerSingle(newPortal);
		}

		private IEnumerator DelaySoundFX(Vector3Int position)
		{
			yield return WaitFor.Seconds(0.6f);
			SoundManager.PlayNetworkedAtPos(LesserSummonGunsSFX, position);
		}

		private bool SpawnGunInHand(ConnectedPlayer caster)
		{
			SpawnResult result = Spawn.ServerPrefab(gunPrefab, caster.Script.WorldPos);
			if (result.Successful)
			{
				GameObject firstGun = result.GameObject;
				ItemSlot bestSlot = caster.Script.ItemStorage.GetBestHandOrSlotFor(firstGun);
				Inventory.ServerAdd(firstGun, bestSlot);

				return true;
			}

			Logger.LogError($"Failed to spawn {gunPrefab} for {this}!");
			return false;
		}

		#region Sync

		private void SyncLatestPortal(GameObject oldPortal, GameObject newPortal)
		{
			latestPortal = newPortal;
			StartCoroutine(AnimatePortal());
		}

		private void SyncLatestGun(GameObject oldGun, GameObject newGun)
		{
			latestGun = newGun;
			AnimateGun();
		}

		#endregion Sync

		#region Animation

		private IEnumerator AnimatePortal()
		{
			Transform portalTransform = latestPortal.transform;

			portalTransform.localScale = Vector3.zero;
			portalTransform.transform.LeanScale(Vector3.one, PORTAL_OPEN_TIME);

			yield return WaitFor.Seconds(PORTAL_OPEN_TIME + PORTAL_ACTIVE_TIME);

			portalTransform.LeanScale(Vector3.zero, PORTAL_CLOSE_TIME);
		}

		private void AnimateGun()
		{
			Transform latestGunTransform = latestGun.transform.Find("Sprite").transform;

			latestGunTransform.LeanSetLocalPosY(PORTAL_HEIGHT);
			latestGunTransform.localRotation = RandomUtils.RandomRotatation2D();
			latestGunTransform.LeanMoveLocalY(0, GUN_FALLING_TIME);
			latestGunTransform.LeanRotateZ(Random.Range(0, 720), GUN_FALLING_TIME);
		}

		#endregion Animation

		private Vector3Int GetRandomPosition()
		{
			return epicentre + RandomUtils.RandomAnnulusPoint(0, scatterRadius).CutToInt();
		}
	}
}
