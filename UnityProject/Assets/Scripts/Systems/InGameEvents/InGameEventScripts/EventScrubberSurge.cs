using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Chemistry;
using Chemistry.Components;
using Managers;
using Strings;
using ScriptableObjects;
using Objects.Atmospherics;


namespace InGameEvents
{
	public class EventScrubberSurge : EventScriptBase, IServerSpawn
	{
		[Tooltip("A temporary container by which chemicals can be dispersed from.")]
		[SerializeField]
		private GameObject reagentContainer = default;

		[Tooltip("Assign dispersion agents e.g. smoke or foaming agent.")]
		[SerializeField]
		private Reagent[] dispersionAgents = default;

		[Tooltip("Each scrubber will randomly select a delay period before spawning reagents, within this range.")]
		[SerializeField, MinMaxSlider(0f, 100f)]
		private Vector2 spawnDelayRange = new Vector2(3f, 10f);

		private static Reagent[] allReagents;

		public void OnSpawnServer(SpawnInfo info)
		{
			if (allReagents != null && allReagents.Length > 0) return;

			allReagents = ChemistryReagentsSO.Instance.AllChemistryReagents;
		}

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "The scrubber network is experiencing a backpressure surge. Some ejection of contents may occur.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			ReagentContainer container = Instantiate(reagentContainer).GetComponent<ReagentContainer>();

			foreach (var scrubber in FindObjectsOfType<Scrubber>())
			{
				StartCoroutine(SpillAtScrubber(scrubber, container));
			}

			StartCoroutine(DeleteContainer(container));
		}

		private IEnumerator SpillAtScrubber(Scrubber scrubber, ReagentContainer container)
		{
			yield return WaitFor.Seconds(Random.Range(spawnDelayRange.x, spawnDelayRange.y));

			// Check that the scrubber is still fine to reference after this delay.
			if (scrubber == null || scrubber.registerTile == null) yield break;

			var reagentMix = new ReagentMix();
			lock (reagentMix.reagents)
			{
				reagentMix.reagents.m_dict.Add(allReagents.PickRandom(), 75f);
				reagentMix.reagents.m_dict.Add(dispersionAgents.PickRandom(), 25f);
			}
			

			container.Add(reagentMix);
			container.Spill(scrubber.registerTile.WorldPositionServer, 50f);

			// TODO: Play noise.
		}

		private IEnumerator DeleteContainer(ReagentContainer container)
		{
			yield return WaitFor.Seconds(spawnDelayRange.y + 1f);
			Destroy(container.gameObject);
		}
	}
}
