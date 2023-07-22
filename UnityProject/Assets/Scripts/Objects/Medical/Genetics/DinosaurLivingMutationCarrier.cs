using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Systems.MobAIs;
using UnityEngine;

public class DinosaurLivingMutationCarrier : NetworkBehaviour
{
	public List<MutationSO> CarryingMutations;

	public List<GrowthStage> GrowingStages;

	[SyncVar(hook = nameof(SynchroniseSize))]
	public int StageSynchronise = 0;

	public bool HungryAndWantsToGrow = false; //Sounds like some type of ad , single dinosaurs In your area Hungary for and want to meet up


	[System.Serializable]

	public class GrowthStage
	{
		public float SpriteSizeScale = 1;
		public float FoodRefreshTime = 60;
	}


	//bunch of sprites for different dinosaurs




	public void Start()
	{
		SynchroniseSize(StageSynchronise, 0);
		this.GetComponent<MobExplore>().FoodEatenEvent += EatFood;
		StartCoroutine(BecomeHungry());
	}

	public void SynchroniseSize(int old, int NewStage)
	{
		StageSynchronise = NewStage;
		var NewSize = GrowingStages[NewStage].SpriteSizeScale;
		this.gameObject.transform.localScale = new Vector3(NewSize, NewSize, NewSize);
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
