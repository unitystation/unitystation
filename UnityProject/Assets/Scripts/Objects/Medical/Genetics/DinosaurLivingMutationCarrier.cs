using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Systems.MobAIs;
using UnityEngine;

public class DinosaurLivingMutationCarrier : NetworkBehaviour
{
	public List<MutationSO> CarryingMutations;
	public float DifficultyLevel;

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

	//TODO DNAConsole.UnlockedMutations.Add UI Update


	//cargo amber ore
	//	UI for processing amber or/doing mini puzzle, print egg

	//	egg logic, TODO Spawning in different levels of dinosaur
	//bunch of different prefabs for dangerous dinosaurs

	//bunch of sprites for different dinosaurs

	//dinosaur gets spawned depending on difficulty of each mutation (E.G T Rex if it's got loads of epic mutations) only correlates 75% of time, just a troll
	//Feeding mechanics
	//growth stages


	//that's what needs to be done

	//Chasmosaurus?

	// 300
	// 200 = Tarbosaurus, Brtachiosaurus,  >
	// 100 = Stegosaurus,Triceratops, school >
	// 0 = raptor, Dimetrodon angelensis, troodon >

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
