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

		[Tooltip(" How much does medicine get metabolised by body parts That are internal and don't contribute to  overall health ")]
		public float InternalMetabolismPerSecond  = 1f;

		[Tooltip(" How much does medicine get metabolised by body parts that contribute to overall health ")]
		public float ExternalMetabolismPerSecond = 2f;
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
			var internalTotalBloodThroughput = 0f;

			foreach (var bodyPart in MetabolismComponents)
			{
				internalTotalBloodThroughput += bodyPart.BloodThroughput;
			}


			if (internalTotalBloodThroughput.Approx(0)) return;

			var internalMetabolismFlowPerOne = InternalMetabolismPerSecond / internalTotalBloodThroughput;

			foreach (var bodyPart in MetabolismComponents)
			{
				bodyPart.ReagentMetabolism = internalMetabolismFlowPerOne;
			}



			var externalTotalBloodThroughput = 0f;

			foreach (var bodyPart in MetabolismComponents)
			{
				if (bodyPart.RelatedPart.DamageContributesToOverallHealth == false) continue;
				externalTotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var metabolismFlowPerOne =  ExternalMetabolismPerSecond / externalTotalBloodThroughput;

			foreach (var bodyPart in MetabolismComponents)
			{
				if (bodyPart.RelatedPart.DamageContributesToOverallHealth == false) continue;
				bodyPart.ReagentMetabolism = metabolismFlowPerOne;
			}
			BodyPartListChange();
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
					                    metabolismComponent.CurrentBloodSaturation;
				}

				if (ProcessingAmount == 0) continue;

				Reaction.React(PrecalculatedMetabolismReactions[Reaction], _reagentPoolSystem.BloodPool, ProcessingAmount);
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