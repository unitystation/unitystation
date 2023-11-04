using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Chemistry;
using Chemistry.Components;
using Managers;
using Strings;
using ScriptableObjects;
using Objects.Atmospherics;
using UnityEngine.Serialization;


namespace InGameEvents
{
	public class EventScrubberSurge : EventScriptBase
	{
		//TODO some time
		//5% chance to make the janitor antagonist When the scrubber Surge  event happens
		//like a special antag "you can't deal with this crap anymore! make a mess!!!"

		private System.Random RNG = new System.Random();

		[Tooltip("A temporary container by which chemicals can be dispersed from.")]
		[SerializeField]
		private GameObject reagentContainer = default;

		[Tooltip("Assign dispersion agents e.g. smoke or foaming agent.")]
		[SerializeField]
		private List<Reagent> dispersionAgents = default;

		[FormerlySerializedAs("RareDispenseProbability")]
		[Tooltip("The  probability that a rare dispersionAgents will be chosen")]
		[SerializeField]
		private float rareDispenseProbability = 0.005f;

		[FormerlySerializedAs("RareDispersionAgents")]
		[Tooltip("Assign dispersion agents e.g. smoke or foaming agent That was one according to the Rare probability.")]
		[SerializeField]
		private List<Reagent> rareDispersionAgents = default;

		[Tooltip("Each scrubber will randomly select a delay period before spawning reagents, within this range.")]
		[SerializeField, MinMaxSlider(0f, 100f)]
		private Vector2 spawnDelayRange = new Vector2(3f, 10f);


		public bool ShouldDispenseRareDispersionAgents()
		{
			float randomValue = (float)RNG.NextDouble(); // Generates a random value between 0 and 1
			return randomValue <= rareDispenseProbability;
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
				if (ShouldDispenseRareDispersionAgents())
				{
					reagentMix.reagents.m_dict.Add(rareDispersionAgents.PickRandom(), 25f);
				}
				else
				{
					reagentMix.reagents.m_dict.Add(dispersionAgents.PickRandom(), 25f);
				}

				reagentMix.reagents.m_dict.Add(ChemistryReagentsSO.Instance.AllChemistryReagents.PickRandom(), 75f);
			}


			container.Add(reagentMix, false);
			container.Spill(scrubber.registerTile.WorldPositionServer, container.MaxCapacity);

			// TODO: Play noise.
		}

		private IEnumerator DeleteContainer(ReagentContainer container)
		{
			yield return WaitFor.Seconds(spawnDelayRange.y + 1f);
			Destroy(container.gameObject);
		}
	}
}
