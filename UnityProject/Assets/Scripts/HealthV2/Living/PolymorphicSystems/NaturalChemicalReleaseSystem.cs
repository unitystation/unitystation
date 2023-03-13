using System.Collections.Generic;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;

namespace HealthV2.Living.PolymorphicSystems
{
	public class NaturalChemicalReleaseSystem : HealthSystemBase
	{
		public Dictionary<Reagent, ReagentWithBodyParts> Toxicity = new Dictionary<Reagent, ReagentWithBodyParts>();

		public List<NaturalChemicalReleaseComponent> BodyParts = new List<NaturalChemicalReleaseComponent>();

		public class ReagentWithBodyParts
		{
			public float Percentage;
			public float TotalNeeded;
			public List<NaturalChemicalReleaseComponent> RelatedBodyParts = new List<NaturalChemicalReleaseComponent>();
			public Dictionary<Reagent, ReagentWithBodyParts> ReplacesWith = new Dictionary<Reagent, ReagentWithBodyParts>();
		}

		private ReagentPoolSystem _reagentPoolSystem;

		public override void InIt()
		{
			_reagentPoolSystem = Base.reagentPoolSystem; //idk Shouldn't change
		}

		public override void StartFresh()
		{
			foreach (var bodyPart in BodyParts)
			{
				if (bodyPart.NaturalToxinReagent == null)
				{
					bodyPart.NaturalToxinReagent = Base.InitialSpecies.Base.BodyNaturalToxinReagent;
				}
			}

			InitialiseToxGeneration();

		}

		public void InitialiseToxGeneration()
		{

			float TotalToxinGenerationPerSecond = Base.InitialSpecies.Base.TotalToxinGenerationPerSecond;



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
		}

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<NaturalChemicalReleaseComponent>();
			if (component != null)
			{
				BodyParts.Add(component);
				BodyPartListChange();
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
			foreach (var Heart in _reagentPoolSystem.PumpingDevices)
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
				Multiplier = 0.0025f;
			}

			foreach (var KVP in Toxicity)
			{
				_reagentPoolSystem.BloodPool.Add(KVP.Key, KVP.Value.TotalNeeded * Multiplier);
			}
		}


		public override HealthSystemBase CloneThisSystem()
		{
			return new NaturalChemicalReleaseSystem();
		}
	}
}