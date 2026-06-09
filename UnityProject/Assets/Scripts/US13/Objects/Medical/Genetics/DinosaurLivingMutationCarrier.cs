using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Transform;
using US13.HealthV2.Living;
using US13.NPC.AI;

namespace US13.Objects.Medical.Genetics
{
	public class DinosaurLivingMutationCarrier : NetworkBehaviour
	{
		public List<MutationSO> CarryingMutations;

		public List<GrowthStage> GrowingStages;

		[SyncVar(hook = nameof(SynchroniseSize))]
		public int StageSynchronise = 0;

		public bool HungryAndWantsToGrow = false; //Sounds like some type of ad , single dinosaurs In your area Hungary for and want to meet up


		public ScaleSync ScaleSync;

		[System.Serializable]

		public class GrowthStage
		{
			public float SpriteSizeScale = 1;
			public float FoodRefreshTime = 60;
		}


		//bunch of sprites for different dinosaurs

		public void Awake()
		{
			ScaleSync = this.GetComponent<ScaleSync>();
		}


		public void Start()
		{
			if (isServer)
			{
				SynchroniseSize(StageSynchronise, 0);
			}

			this.GetComponent<MobExplore>().FoodEatenEvent += EatFood;
			StartCoroutine(BecomeHungry());
		}

		public void SynchroniseSize(int old, int NewStage)
		{
			StageSynchronise = NewStage;
			var NewSize = GrowingStages[NewStage].SpriteSizeScale;
			if (isServer)
			{
				ScaleSync.SetScale( new Vector3(NewSize, NewSize, NewSize));

			}
		}


		private IEnumerator BecomeHungry()
		{
			yield return WaitFor.Seconds(GrowingStages[StageSynchronise].FoodRefreshTime);
			HungryAndWantsToGrow = true;
		}

		public void EatFood()
		{
			if (HungryAndWantsToGrow)
			{
				if ((StageSynchronise+1) < GrowingStages.Count)
				{
					int newStageSynchronise = StageSynchronise;
					newStageSynchronise++;
					SynchroniseSize(StageSynchronise, newStageSynchronise);
				}

				HungryAndWantsToGrow = false;
				StartCoroutine(BecomeHungry());
			}

		}

	}
}
