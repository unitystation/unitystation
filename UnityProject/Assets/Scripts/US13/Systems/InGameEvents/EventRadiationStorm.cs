using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Strings;
using Util;

namespace US13.Systems.InGameEvents
{
	public class EventRadiationStorm : EventScriptBase
	{
		[Tooltip("The radiation storm object to randomly spawn around the station.")]
		[SerializeField, BoxGroup("References")]
		private GameObject radiationSourcePrefab = default;

		[Tooltip("The number of radiation storm objects to spawn (randomly selected within the given range).")]
		[SerializeField, BoxGroup("Settings"), MinMaxSlider(0, 100)]
		private Vector2 numberOfStorms = new Vector2(10, 20);

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "Ionizing electromagnetic emissions detected near the station. Avoid areas of high radiation.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);

				_ = SoundManager.PlayNetworked(CommonSounds.Instance.RadiationAnnouncement);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			for (int i = 0; i < Random.Range(numberOfStorms.x, numberOfStorms.y); i++)
			{
				StartCoroutine(SpawnRadiationObject());
			}
		}

		private IEnumerator SpawnRadiationObject()
		{
			yield return WaitFor.Seconds(Random.Range(0, 5));
			Spawn.ServerPrefab(radiationSourcePrefab, RandomUtils.GetRandomPointOnStation(true, true));
		}
	}
}
