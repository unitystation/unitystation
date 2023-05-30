using System.Collections;
using Systems.Spells.Wizard;
using UnityEngine;
using NaughtyAttributes;
using Managers;
using Strings;

namespace InGameEvents
{
	public class EventPortalStorm : EventScriptBase
	{
		[Tooltip("The portal from which mobs will spawn.")]
		[SerializeField, BoxGroup("References")]
		private GameObject portalPrefab = default;

		[Tooltip("Mobs to spawn will be randomly chosen from this array.")]
		[SerializeField, BoxGroup("References")]
		private GameObject[] mobsToSpawn = default;

		[Tooltip("If the chance for a rare mob succeeds, a random rare mob from this list will be spawned .")]
		[SerializeField, BoxGroup("References")]
		private GameObject[] rareMobsToSpawn = default;

		[Tooltip("The amount of mobs to spawn will be within this range.")]
		[SerializeField, BoxGroup("Settings"), MinMaxSlider(0, 100)]
		private Vector2 mobCount = new Vector2(5, 15);

		[Tooltip("The chance (in percent) for a rare mob to spawn instead of a normal one.")]
		[SerializeField, BoxGroup("Settings"), Range(0, 100)]
		private int rareMobChance = 5;

		private readonly static PortalSpawnInfo portalSettings = new PortalSpawnInfo
		{
			PortalHeight = 0,
			EntityRotate = false,
			PortalOpenTime = 0,
			PortalCloseTime = 10,
			PortalSuspenseTime = 10,
		};

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "Massive bluespace anomaly detected en route to your station. Brace for impact.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);

				_ = SoundManager.PlayNetworked(CommonSounds.Instance.SpanomaliesAnnouncement);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			for (int i = 0; i < Random.Range(mobCount.x, mobCount.y); i++)
			{
				StartCoroutine(SpawnMob());
			}
		}

		private IEnumerator SpawnMob()
		{
			yield return WaitFor.Seconds(Random.Range(0, 5));
			if (rareMobsToSpawn.Length >= 1 && DMMath.Prob(rareMobChance))
			{
				new SpawnByPortal(rareMobsToSpawn.PickRandom(), portalPrefab, RandomUtils.GetRandomPointOnStation(true, true), portalSettings);
			}
			else
			{
				new SpawnByPortal(mobsToSpawn.PickRandom(), portalPrefab, RandomUtils.GetRandomPointOnStation(true, true), portalSettings);
			}
		}
	}
}
