using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using Chemistry.Components;

namespace HealthV2
{
	public class Stomach : BodyPartModification
	{
		[NonSerialized] public ReagentContainer StomachContents;

		public float DigesterAmountPerSecond = 1;

		public List<BodyFat> BodyFats = new List<BodyFat>();

		public BodyFat BodyFatToInstantiate;

		private System.Random random;

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();

			StomachContents = GetComponentInChildren<ReagentContainer>();

			foreach (Reagent reagent in StomachContents.CurrentReagentMix.reagents.Keys)
			{
				ReagentVomit rvomit = reagent.reagentVomit;
				int rand = random.Next(0, 10000);
				Vector3Int worldPos = RelatedPart.HealthMaster.ObjectBehaviour.AssumedWorldPositionServer();
				if (rvomit != null && rand < rvomit.vomitchance && !StomachContents.IsEmpty)
				{
					float takeAmount = 5;
					if (StomachContents.ReagentMixTotal < 5)
					{
						takeAmount = StomachContents.ReagentMixTotal;
					}
					var takeMix = StomachContents.TakeReagents(takeAmount);
					EffectsFactory.VomitSplat(worldPos, takeMix, rvomit.vomitblood);
					StomachContents.CurrentReagentMix.RemoveVolume(takeAmount);
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
	}
}