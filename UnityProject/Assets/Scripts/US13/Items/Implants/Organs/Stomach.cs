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
	public class Stomach : BodyPartFunctionality
	{
		public ReagentContainer StomachContents;

		public float DigesterAmountPerSecond = 1;

		public float StomachIsConsideredFullWhenSpareCapacityIsLessThan = 15f;

		public List<BodyFat> BodyFats = new List<BodyFat>();

		public BodyFat BodyFatToInstantiate;

		public ReagentCirculatedComponent _ReagentCirculatedComponent;
		public HungerComponent HungerComponent;


		public override void Awake()
		{
			base.Awake();
			_ReagentCirculatedComponent = this.GetCachedComponent<ReagentCirculatedComponent>();
			HungerComponent = this.GetCachedComponent<HungerComponent>();
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			if (!StomachContents) return;
			//BloodContainer
			if (StomachContents.ReagentMixTotal > 0)
			{
				float ToDigest = DigesterAmountPerSecond * RelatedPart.TotalModified;
				if (StomachContents.ReagentMixTotal < ToDigest)
				{
					ToDigest = StomachContents.ReagentMixTotal;
				}
				var Digesting = StomachContents.TakeReagents(ToDigest);

				_ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(Digesting);
			}
			else
			{
				HungerComponent.HungerState = HungerState.Starving;
			}

			if (StomachContents.SpareCapacity < StomachIsConsideredFullWhenSpareCapacityIsLessThan)
			{
				HungerComponent.HungerState = HungerState.Full;
			}
			else
			{
				HungerComponent.HungerState = HungerState.Normal;
			}

			if (CanAddFat())
			{
				AddFat();
				if (RelatedPart.HealthMaster.gameObject)
				{
					Chat.AddExamineMsg(RelatedPart.HealthMaster.gameObject, "You feel like you've gained a little weight.");
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

			return allFatFull && StomachContents.ReagentMixTotal > 0;
		}

		public void AddFat()
		{
			var added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject, spawnManualContents: true).GameObject.GetComponent<BodyFat>();
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