using System.Collections.Generic;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using Util;

namespace US13.Items.Implants.Organs
{
	[System.Serializable]
	public class Stomach : BodyPartBehavior
	{
		public ReagentContainer StomachContents;

		public float DigesterAmountPerSecond = 1;

		public float StomachIsConsideredFullWhenSpareCapacityIsLessThan = 15f;

		[UnityEngine.SerializeReference] public List<BodyFat> BodyFats = new List<BodyFat>();

		public GameObject BodyFatToInstantiate;

		[UnityEngine.SerializeReference] public ReagentCirculatedComponent _ReagentCirculatedComponent;
		[UnityEngine.SerializeReference] public HungerComponent HungerComponent;


		public override void OnInitialize()
		{
			base.OnInitialize();
			_ReagentCirculatedComponent = GetBodyPartBehavior<ReagentCirculatedComponent>();
			HungerComponent = GetBodyPartBehavior<HungerComponent>();
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			if (!StomachContents) return;
			//BloodContainer

			float ToDigest = DigesterAmountPerSecond * RelatedPart.TotalModified;
			if (StomachContents.Total < ToDigest)
			{
				ToDigest = StomachContents.Total;
			}

			var Digesting = StomachContents.TakeReagents(ToDigest);

			_ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(Digesting);

			if (CanAddFat())
			{
				AddFat();
				if (RelatedPart.HealthMaster.gameObject)
				{
					Chat.AddExamineMsg(RelatedPart.HealthMaster.gameObject,
						"You feel like you've gained a little weight.");
				}
			}
		}

		public bool CanAddFat()
		{
			if (BodyFats.Count == 0) return true;
			//var allFatFull = BodyFats.All(x => x.IsFull); //linq bad
			bool allFatFull = true;
			foreach (var fat in BodyFats)
			{
				if (fat.IsFull) continue;
				allFatFull = false;
				break;
			}

			return allFatFull && StomachContents.Total > 0;
		}

		public void AddFat()
		{
			var added = Spawn.ServerPrefab(BodyFatToInstantiate, spawnManualContents: true).GameObject
				.GetBodyPartBehavior<BodyFat>();
			BodyFats.Add(added);
			added.RelatedStomach = this;
			added.SetAbsorbedAmount(0);
			RelatedPart.ContainedIn.OrganStorage.ServerTryAdd(added.gameObject);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			base.OnRemovedFromBody(livingHealth);
			BodyFats.Clear();
		}
	}
}
