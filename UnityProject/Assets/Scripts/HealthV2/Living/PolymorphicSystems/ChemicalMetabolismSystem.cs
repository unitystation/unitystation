using System.Collections.Generic;
using HealthV2.Living.CirculatorySystem;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	public class ChemicalMetabolismSystem : HealthSystemBase, IAreaReactionBase
	{
		public Dictionary<MetabolismReaction, List<MetabolismComponent>> PrecalculatedMetabolismReactions =
			new Dictionary<MetabolismReaction, List<MetabolismComponent>>();

		public List<MetabolismReaction> MetabolismReactions { get; } = new();

		public List<MetabolismReaction> ALLMetabolismReactions = new List<MetabolismReaction>(); //TOOD Move somewhere static maybe

		public List<MetabolismComponent> MetabolismComponents = new List<MetabolismComponent>();

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

			PlayerHealthData RaceBodypart = Base.InitialSpecies;
			var InternalTotalBloodThroughput = 0f;

			foreach (var bodyPart in MetabolismComponents)
			{
				InternalTotalBloodThroughput += bodyPart.BloodThroughput;
			}
			if (InternalTotalBloodThroughput == 0) return;

			var InternalMetabolismFlowPerOne = RaceBodypart.Base.InternalMetabolismPerSecond / InternalTotalBloodThroughput;

			foreach (var bodyPart in MetabolismComponents)
			{
				bodyPart.ReagentMetabolism = InternalMetabolismFlowPerOne;
			}



			var ExternalTotalBloodThroughput = 0f;

			foreach (var bodyPart in MetabolismComponents)
			{
				if (bodyPart.RelatedPart.DamageContributesToOverallHealth == false) continue;
				ExternalTotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var MetabolismFlowPerOne =  RaceBodypart.Base.ExternalMetabolismPerSecond / ExternalTotalBloodThroughput;

			foreach (var bodyPart in MetabolismComponents)
			{
				if (bodyPart.RelatedPart.DamageContributesToOverallHealth == false) continue;
				bodyPart.ReagentMetabolism = MetabolismFlowPerOne;
			}
		}



		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<MetabolismComponent>();
			if (component != null)
			{
				if (MetabolismComponents.Contains(component) == false)
				{
					MetabolismComponents.Add(component);
					BodyPartListChange();
				}
			}
		}

		public override void BodyPartRemoved(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<MetabolismComponent>();
			if (component != null)
			{
				if (MetabolismComponents.Contains(component))
				{
					MetabolismComponents.Remove(component);
				}

				BodyPartListChange();
			}
		}

		public void BodyPartListChange()
		{
			PrecalculatedMetabolismReactions.Clear();

			foreach (var MR in ALLMetabolismReactions)
			{
				foreach (var bodyPart in MetabolismComponents)
				{

					if (bodyPart.RelatedPart.ItemAttributes.HasAllTraits(MR.InternalAllRequired) &&
					    bodyPart.RelatedPart.ItemAttributes.HasAnyTrait(MR.InternalBlacklist) == false)
					{
						if (PrecalculatedMetabolismReactions.ContainsKey(MR) == false)
						{
							PrecalculatedMetabolismReactions[MR] = new List<MetabolismComponent>();
						}

						PrecalculatedMetabolismReactions[MR].Add(bodyPart);
					}
				}
			}
		}

		public override void SystemUpdate()
		{
			MetaboliseReactions();
		}

		public void MetaboliseReactions()
		{
			MetabolismReactions.Clear();

			foreach (var Reaction in PrecalculatedMetabolismReactions)
			{
				Reaction.Key.Apply(this, reagentPoolSystem.BloodPool);
			}

			foreach (var Reaction in MetabolismReactions)
			{
				float ProcessingAmount = 0;
				foreach (var metabolismComponent in PrecalculatedMetabolismReactions[Reaction]) //TODO maybe lag? Alternative?
				{
					ProcessingAmount += metabolismComponent.ReagentMetabolism * metabolismComponent.BloodThroughput *
					                    metabolismComponent.CurrentBloodSaturation *
					                    Mathf.Max(0.10f, metabolismComponent.RelatedPart.TotalModified);
				}

				if (ProcessingAmount == 0) continue;

				//Reaction.React(PrecalculatedMetabolismReactions[Reaction], _reagentPoolSystem.BloodPool, ProcessingAmount);
			}
		}

		public override HealthSystemBase CloneThisSystem()
		{
			return new ChemicalMetabolismSystem()
			{
				ALLMetabolismReactions = ALLMetabolismReactions
			};
		}
	}
}