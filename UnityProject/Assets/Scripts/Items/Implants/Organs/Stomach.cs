using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using Chemistry.Components;

namespace HealthV2
{
	public class Stomach : Organ
	{
		public ReagentContainer StomachContents;

		public float DigesterAmountPerSecond = 1;

		public List<BodyFat> BodyFats = new List<BodyFat>();

		public BodyFat BodyFatToInstantiate;

		private System.Random random = new System.Random();

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();

			StomachContents = GetComponentInChildren<ReagentContainer>();

			foreach (Reagent reagent in StomachContents.CurrentReagentMix.reagents.Keys)
			{
				ReagentVomit rvomit = reagent.reagentVomit;
				int rand = random.Next(0, 10000);
				if (rvomit != null && rand < rvomit.vomitchance && !StomachContents.IsEmpty)
				{
					Vomit(reagent);
				}
			}

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
				StartCoroutine(DelayAddFat());
			}
		}

		private IEnumerator DelayAddFat()
		{
			yield return null;
			var Parent = RelatedPart.GetParent();
			var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject).GameObject.GetComponent<BodyFat>();
			BodyFats.Add(Added);
			Parent.Storage.ServerTryAdd(Added.gameObject);
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			base.RemovedFromBody(livingHealthMasterBase);
			BodyFats.Clear();

		}

		public void Vomit (Reagent reagent)
		{
			Vector3Int worldPos = RelatedPart.HealthMaster.ObjectBehaviour.AssumedWorldPositionServer();
			
			float takeAmount = 5;
			if (StomachContents.ReagentMixTotal < 5)
			{
				takeAmount = StomachContents.ReagentMixTotal;
			}
			if (StomachContents.ReagentMixTotal <= 0)
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.GetComponent<RegisterPlayer>().gameObject, "You dry heave.", "What a nerd, this guy dry heaved.");
				return;
			}
			
			var takeMix = StomachContents.TakeReagents(takeAmount);

			if (reagent.reagentVomit.vomitblood)
			{
				if (RelatedPart.BloodContainer.ReagentMixTotal < 5)
				{
					takeAmount = StomachContents.ReagentMixTotal;
				}
				takeMix = RelatedPart.BloodContainer.TakeReagents(takeAmount);
				RelatedPart.BloodContainer.CurrentReagentMix.RemoveVolume(takeAmount);
			}
			else
			{
				StomachContents.CurrentReagentMix.RemoveVolume(takeAmount);
			}
			EffectsFactory.VomitSplat(worldPos, takeMix, reagent.reagentVomit.vomitblood);
			float runSpeed = RelatedPart.HealthMaster.GetComponent<RegisterPlayer>().GetComponent<PlayerScript>().playerMove.RunSpeed;
			RelatedPart.HealthMaster.GetComponent<RegisterPlayer>().GetComponent<PlayerScript>().playerMove.RunSpeed = RelatedPart.HealthMaster.GetComponent<RegisterPlayer>().GetComponent<PlayerScript>().playerMove.WalkSpeed;

			StartCoroutine(ReturnSpeed(runSpeed));

			RelatedPart.HealDamage(null, random.Next(0,5), DamageType.Tox);
			Chat.AddActionMsgToChat(RelatedPart.HealthMaster.GetComponent<RegisterPlayer>().gameObject, "You vomit.", "What a nerd, this guy vomitted.");
		}

		IEnumerator ReturnSpeed(float runSpeed)
		{
			yield return new WaitForSeconds(5);

			RelatedPart.HealthMaster.GetComponent<RegisterPlayer>().GetComponent<PlayerScript>().playerMove.RunSpeed = runSpeed;
		}
	}
}
