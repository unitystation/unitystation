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

		[Tooltip("The amount of mobs to spawn will be within this range.")]
		[SerializeField, BoxGroup("Settings"), MinMaxSlider(0, 100)]
		private Vector2 mobCount = new Vector2(5, 15);

		private readonly static PortalSpawnInfo portalSettings = new PortalSpawnInfo
		{
			PortalHeight = 0,
			EntityRotate = false,
		};

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "Massive bluespace anomaly detected en route to your station. Brace for impact.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			// TODO: play Portal Storm sound.

			for (int i = 0; i < Random.Range(mobCount.x, mobCount.y); i++)
			{
				StartCoroutine(SpawnMob());
			}
		}

		private IEnumerator SpawnMob()
		{
			yield return WaitFor.Seconds(Random.Range(0, 5));
			new SpawnByPortal(mobsToSpawn.PickRandom(), portalPrefab, RandomUtils.GetRandomPointOnStation(true, true), portalSettings);
		}
	}
}
