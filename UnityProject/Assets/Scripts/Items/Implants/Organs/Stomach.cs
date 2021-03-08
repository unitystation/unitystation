using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using Chemistry.Components;

namespace HealthV2
{
	public class Stomach : BodyPart
	{
		public ReagentContainer StomachContents = null;

		public float DigesterAmountPerSecond = 1;

		public List<BodyFat> BodyFats = new List<BodyFat>();

		public BodyFat BodyFatToInstantiate;

		public override void ImplantPeriodicUpdate(LivingHealthMasterBase healthMaster)
		{
			base.ImplantPeriodicUpdate(healthMaster);
			//BloodContainer
			if (StomachContents.ReagentMixTotal > 0)
			{
				float ToDigest = DigesterAmountPerSecond * TotalModified;
				if (StomachContents.ReagentMixTotal < ToDigest)
				{
					ToDigest = StomachContents.ReagentMixTotal;
				}
				var Digesting = StomachContents.TakeReagents(ToDigest);

				BloodContainer.Add(Digesting);

				//healthMaster.NutrimentLevel += Digesting[Nutriment];
				//What to do with non Digesting content, put back in stomach?

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
				var Parent = GetParent();
				var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject).GameObject.GetComponent<BodyFat>();
				BodyFats.Add(Added);
				Parent.storage.ServerTryAdd(Added.gameObject);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			base.RemovedFromBody(livingHealthMasterBase);
			BodyFats.Clear();

		}

		public override void HealthMasterSet()
		{
			base.HealthMasterSet();
			var Parent = GetParent();
			var Added = Spawn.ServerPrefab(BodyFatToInstantiate.gameObject).GameObject.GetComponent<BodyFat>();
			BodyFats.Add(Added);
			Parent.storage.ServerTryAdd(Added.gameObject);
		}
	}
}