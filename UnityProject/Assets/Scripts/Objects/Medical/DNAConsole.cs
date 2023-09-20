using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Objects.Medical;
using UI.Objects.Medical;
using UnityEngine;

[System.Serializable]
public class DNAMutationData
{
	public string BodyPartSearchString = "Torso"; //IDK Better system

	public List<DNAPayload> Payload = new List<DNAPayload>();

	[System.Serializable]
	public class DNAPayload
	{
		public string CustomisationTarget;
		public string CustomisationReplaceWith;

		public MutationSO TargetMutationSO;

		public MutationSO RemoveTargetMutationSO;

		public PlayerHealthData SpeciesMutateTo;
		public GameObject MutateToBodyPart;
	}

}




public class DNAConsole : MonoBehaviour
{
	public DNAScanner DNAScanner;

	public List<PlayerHealthData> ALLSpecies = new List<PlayerHealthData>();

	public List<PlayerHealthData> UnlockedSpecies = new List<PlayerHealthData>();

	public List<MutationSO> ALLMutations = new List<MutationSO>();

	public List<MutationSO> UnlockedMutations = new List<MutationSO>();

	public List<DNAMutationData> Injecting = new List<DNAMutationData>();


	public GameObject EggPrefab;

	public GameObject InjectorPrefab;


	public GUI_DNAConsole ActiveGUI_DNAConsole; //UpdateMutations



	public int CurrentDNACharge;

	public int RequiredDNASamples = 3;


	[RightClickMethod()]
	[NaughtyAttributes.Button()]
	public void Inject()
	{
		DNAScanner.occupant.InjectDna(Injecting);
	}


	public void AddMutationOfficial(MutationSO MutationSO)
	{
		UnlockedMutations.Add(MutationSO);
		if (ActiveGUI_DNAConsole != null)
		{
			ActiveGUI_DNAConsole.UpdateMutations();
		}

	}

	public bool AddAmber()
	{
		CurrentDNACharge++;

		if (ActiveGUI_DNAConsole != null)
		{
			ActiveGUI_DNAConsole.MutationUnlockMiniGame.UpdateIndicator(); //TODO Doesn't work?!?!?!
		}

		return true;

	}

	/*
	 ok, Species changing equals unlocks a puzzle,
	 target body part , mutate into x Body part in Species

	 For adding or removing requires unlock from upgrades, then it is just
	 Target X body part , tell it to make new body part and that becomes new organ
	 or Target X body part to Tell it Remove itself


	Customisation editing

	Target body part, change XY


	Mutation modification
	Target body part, change XY After unlocking it



	*/

}
