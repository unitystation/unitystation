using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using Chemistry.Components;

namespace HealthV2
{
	public class Stomach : BodyPartFunctionality
	{
		public ReagentContainer StomachContents;

		public float DigesterAmountPerSecond = 1;

		public List<BodyFat> BodyFats = new List<BodyFat>();

		public BodyFat BodyFatToInstantiate;

		public bool InitialFatSpawned = false;
		public Reagent Salt;

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

				RelatedPart.BloodContainer.Add(Digesting);
			}

			if (StomachContents.CurrentReagentMix.MajorMixReagent == Salt && StomachContents.CurrentReagentMix.Total > 15)
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject,
					"<color=red>You hold your chest as you feel your heart giving up!</color>",
					$"<color=red>{RelatedPart.HealthMaster.playerScript.visibleName} holds " +
					$"{RelatedPart.HealthMaster.playerScript.characterSettings.TheirPronoun(RelatedPart.HealthMaster.playerScript)} chest in shock before falling to the ground!</color>");
				RelatedPart.HealthMaster.Death();
			}

			if (StomachContents.SpareCapacity < 15f) //Magic number
			{
				RelatedPart.HungerState = HungerState.Full;
			}
			else
			{
				RelatedPart.HungerState = HungerState.Normal;
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
				var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject).GameObject.GetComponent<BodyFat>();
				Added.SetAbsorbedAmount(0);
				Added.RelatedStomach = this;
				BodyFats.Add(Added);
				RelatedPart.OrganStorage.ServerTryAdd(Added.gameObject);
			}
		}

		public override void HealthMasterSet(LivingHealthMasterBase livingHealth)
		{
			if (InitialFatSpawned == false)
			{
				var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject).GameObject.GetComponent<BodyFat>();
				BodyFats.Add(Added);
				Added.RelatedStomach = this;
				RelatedPart.OrganStorage.ServerTryAdd(Added.gameObject);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.RemovedFromBody(livingHealth);
			BodyFats.Clear();
		}
	}
}