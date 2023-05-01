using System.Collections.Generic;
using Chemistry.Components;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;

namespace Items.Implants.Organs
{
	public class Stomach : BodyPartFunctionality
	{
		public ReagentContainer StomachContents;

		public float DigesterAmountPerSecond = 1;

		public List<BodyFat> BodyFats = new List<BodyFat>();

		public BodyFat BodyFatToInstantiate;

		public bool InitialFatSpawned = false;

		public ReagentCirculatedComponent _ReagentCirculatedComponent;
		public HungerComponent HungerComponent;


		public override void Awake()
		{
			base.Awake();
			_ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
			HungerComponent = this.GetComponentCustom<HungerComponent>();
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();

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

			if (StomachContents.SpareCapacity < 15f) //Magic number
			{
				HungerComponent.HungerState = HungerState.Full;
			}
			else
			{
				HungerComponent.HungerState = HungerState.Normal;
			}

			bool AllFat = true;
			foreach (var Fat in BodyFats)
			{

				if (Fat.IsFull == false)
				{
					AllFat = false;
					break;
				}
			}

			if (AllFat)
			{
				var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject, spawnManualContents: true).GameObject.GetComponent<BodyFat>();
				Added.SetAbsorbedAmount(0);
				Added.RelatedStomach = this;
				BodyFats.Add(Added);
				RelatedPart.OrganStorage.ServerTryAdd(Added.gameObject);
			}
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			AddFat();
		}

		public void AddFat()
		{
			if (InitialFatSpawned == false)
			{
				InitialFatSpawned = true;
				var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject).GameObject.GetComponent<BodyFat>();
				BodyFats.Add(Added);
				Added.RelatedStomach = this;
				RelatedPart.ContainedIn.OrganStorage.ServerTryAdd(Added.gameObject);
			}
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.OnRemovedFromBody(livingHealth);
			BodyFats.Clear();
		}
	}
}