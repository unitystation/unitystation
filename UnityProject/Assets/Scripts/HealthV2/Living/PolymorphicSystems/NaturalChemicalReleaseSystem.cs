using System.Collections.Generic;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	public class NaturalChemicalReleaseSystem : HealthSystemBase
	{
		public Dictionary<Reagent, ReagentWithBodyParts> Toxicity = new Dictionary<Reagent, ReagentWithBodyParts>();

		public List<NaturalChemicalReleaseComponent> BodyParts = new List<NaturalChemicalReleaseComponent>();

		public float TotalToxinGenerationPerSecond = 0.1f;

		[Tooltip("What reagent does this expel as waste?, Sets all the body parts that don't have a set NaturalToxinReagent")]
		public Reagent BodyNaturalToxinReagent;

		public class ReagentWithBodyParts
		{
			public float Percentage;
			public float TotalNeeded;
			public List<NaturalChemicalReleaseComponent> RelatedBodyParts = new List<NaturalChemicalReleaseComponent>();
			public Dictionary<Reagent, ReagentWithBodyParts> ReplacesWith = new Dictionary<Reagent, ReagentWithBodyParts>();
		}

		private ReagentPoolSystem reagentPoolSystem
		{
			get
			{
				if (_reagentPoolSystem == null)
				{
					_reagentPoolSystem = Base.reagentPoolSystem;
				}

				return _reagentPoolSystem;
			}
		}

		private ReagentPoolSystem _reagentPoolSystem;

		public override void StartFresh()
		{
			foreach (var bodyPart in BodyParts)
			{
				if (bodyPart.NaturalToxinReagent == null)
				{
					bodyPart.NaturalToxinReagent = BodyNaturalToxinReagent;
				}
			}

			InitialiseToxGeneration();

		}

		public void InitialiseToxGeneration()
		{

			var TotalBloodThroughput = 0f;

			foreach (var bodyPart in BodyParts)
			{
				TotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var ToxinFlowPerOne = TotalToxinGenerationPerSecond / TotalBloodThroughput;

			foreach (var bodyPart in BodyParts)
			{
				bodyPart.ToxinGeneration = ToxinFlowPerOne;
			}
			BodyPartListChange();
		}

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<NaturalChemicalReleaseComponent>();
			if (component != null)
			{
				if (BodyParts.Contains(component) == false)
				{
					BodyParts.Add(component);
					BodyPartListChange();
				}
			}
		}

		public override void BodyPartRemoved(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<NaturalChemicalReleaseComponent>();
			if (component != null)
			{
				if (BodyParts.Contains(component))
				{
					BodyParts.Remove(component);
				}

				BodyPartListChange();
			}
		}


		public void BodyPartListChange()
		{
			Toxicity.Clear();

			foreach (var bodyPart in BodyParts)
			{
				if (Toxicity.ContainsKey(bodyPart.NaturalToxinReagent) == false)
				{
					Toxicity[bodyPart.NaturalToxinReagent] = new ReagentWithBodyParts();
				}

				Toxicity[bodyPart.NaturalToxinReagent].RelatedBodyParts.Add(bodyPart);
				Toxicity[bodyPart.NaturalToxinReagent].TotalNeeded += bodyPart.ToxinGeneration * bodyPart.BloodThroughput;
			}
		}

		public override void SystemUpdate()
		{
			float HeartEfficiency = 0;
			if (reagentPoolSystem == null) return;

			foreach (var Heart in reagentPoolSystem.PumpingDevices)
			{
				HeartEfficiency += Heart.CalculateHeartbeat();
			}

			ToxinGeneration(HeartEfficiency);
		}

		public void ToxinGeneration(float HeartEfficiency)
		{
			float Multiplier = HeartEfficiency;
			if (HeartEfficiency == 0)
			{
				return;
			}

			foreach (var KVP in Toxicity)
			{
				reagentPoolSystem.BloodPool.Add(KVP.Key, KVP.Value.TotalNeeded * Multiplier);
			}
		}


		public override HealthSystemBase CloneThisSystem()
		{
			return new NaturalChemicalReleaseSystem();
		}
	}
}