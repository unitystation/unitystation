using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using HealthV2;
using Items.Implants.Organs;
using Systems.Character;
using UnityEngine;
using Util;
using BodyPart = HealthV2.BodyPart;
using Random = UnityEngine.Random;

public class BodyPartMutations : BodyPartFunctionality
{
	public static Dictionary<MutationSO, MutationRoundData> MutationVariants =
		new Dictionary<MutationSO, MutationRoundData>();

	public static Dictionary<PlayerHealthData, MutationRoundData> RaceDataVariants =
		new Dictionary<PlayerHealthData, MutationRoundData>();

	public List<MutationSO> CapableMutations = new List<MutationSO>();
	public List<Mutation> ActiveMutations = new List<Mutation>();

	public int Stability = 0;

	public int SecondsForSpeciesMutation = 150;

	public static MutationRoundData GetMutationRoundData(MutationSO Mutation)
	{
		if (MutationVariants.ContainsKey(Mutation) == false)
		{
			MutationVariants[Mutation] = new MutationRoundData()
			{
				MutationSO = Mutation
			};
			MutationVariants[Mutation].RerollDifficulty();
		}

		MutationVariants[Mutation].CheckValidity();
		return MutationVariants[Mutation];
	}

	public override void Awake()
	{
		base.Awake();
		RelatedPart.OnDamageTaken += OnDMGMutationCheck;
	}


	private void OnDMGMutationCheck(BodyPartDamageData data)
	{
		if (data.DamageAmount <= 0) return;
		if (data.DamageType != DamageType.Clone && data.DamageType != DamageType.Radiation) return;


		data.DamageAmount = Mathf.Clamp(data.DamageAmount, 0, 100);
		//Range = 0 to 100
		//Percentage 100 = 10
		var RNG= Random.Range(0, 1000);
		if ((data.DamageAmount >= RNG) == false) return;

		var available = new List<MutationSO>(CapableMutations);
		foreach (var active in ActiveMutations)
		{
			available.Remove(active.RelatedMutationSO);
		}

		AddMutation(available.PickRandomNonNull());
		//Maybe under undo mutations??
	}


	public static MutationRoundData GetSpeciesRoundData(PlayerHealthData species)
	{
		if (RaceDataVariants.ContainsKey(species) == false)
		{
			RaceDataVariants[species] = new MutationRoundData()
			{
				PlayerHealthData = species
			};
			RaceDataVariants[species].RerollDifficulty();
		}

		RaceDataVariants[species].CheckValidity();
		return RaceDataVariants[species];
	}


	public void MutateCustomisation(string InCustomisationTarget, string CustomisationReplaceWith)
	{
		if (RelatedPart.SetCustomisationData.Contains(InCustomisationTarget))
		{
			//Logger.LogError($"{bodyPart.name} has {InCustomisationTarget} in SetCustomisationData");
			RelatedPart.SetCustomisationString(RelatedPart.SetCustomisationData.Replace(InCustomisationTarget, CustomisationReplaceWith));
			//Logger.LogError($"Changing from {bodyPart.SetCustomisationData} to {newone} ");
		}
	}


	[NaughtyAttributes.Button()]
	public void AddFirstMutation()
	{
		var Mutation = CapableMutations[0];
		AddMutation(Mutation);
	}

	public void AddMutation(MutationSO Mutation)
	{
		if (CapableMutations.Contains(Mutation) == false) return; //TODO Maybe add negative mutation instead

		var Data = GetMutationRoundData(Mutation);

		var ActiveMutation = Mutation.GetMutation(RelatedPart, Mutation);
		ActiveMutation.Stability = Data.Stability;


		ActiveMutations.Add(ActiveMutation);
		ActiveMutation.SetUp();
		CalculateStability();

		RelatedPart.HealthMaster.OrNull()?.BodyPartsChangeMutation();
	}

	public void RemoveMutation(MutationSO Mutation)
	{
		Mutation Target = null;
		foreach (var ActiveMutation in ActiveMutations)
		{
			if (ActiveMutation.RelatedMutationSO == Mutation)
			{
				Target = ActiveMutation;
				break;
			}
		}
		if (Target == null) return;

		ActiveMutations.Remove(Target);
		Target.Remove();
		CalculateStability();
		RelatedPart.HealthMaster.OrNull()?.BodyPartsChangeMutation();
	}

	public List<MutationAndBodyPart> GetAvailableNegativeMutations(List<MutationAndBodyPart> AvailableMutations)
	{
		foreach (var Mutation in CapableMutations)
		{
			if (Mutation == null) continue;
			if (Mutation.Stability > 0)
			{
				bool AlreadyActive = false;
				foreach (var ActiveMutation in ActiveMutations)
				{
					if (ActiveMutation.RelatedMutationSO == Mutation)
					{
						AlreadyActive = true;
						break;
					}
				}

				if (AlreadyActive == false)
				{
					AvailableMutations.Add(new MutationAndBodyPart() {BodyPartMutations = this, MutationSO = Mutation});
				}
			}
		}

		return AvailableMutations;
	}

	public struct MutationAndBodyPart
	{
		public MutationSO MutationSO;
		public BodyPartMutations BodyPartMutations;
	}


	public void CalculateStability()
	{
		int InStability = 0;
		foreach (var ActiveMutation in ActiveMutations)
		{
			InStability += ActiveMutation.Stability;
		}

		Stability = InStability;
	}

	//TODO System for adding and removing body parts
	private IEnumerator ProcessChangeToSpecies(GameObject BodyPart)
	{
		var modifier = (1 + UnityEngine.Random.Range(-0.75f, 0.90f));
		yield return WaitFor.Seconds((SecondsForSpeciesMutation / 4f) * modifier);

		Chat.AddExamineMsgFromServer(RelatedPart.OrNull()?.HealthMaster.OrNull()?.gameObject,
			$" Your {RelatedPart.gameObject.ExpensiveName()} Feels strange");

		yield return WaitFor.Seconds((SecondsForSpeciesMutation / 4f) * modifier);

		Chat.AddExamineMsgFromServer(RelatedPart.OrNull()?.HealthMaster.OrNull()?.gameObject,
			$" Your {RelatedPart.gameObject.ExpensiveName()} Starts to hurt");

		yield return WaitFor.Seconds((SecondsForSpeciesMutation / 4f) * modifier);

		Chat.AddExamineMsgFromServer(RelatedPart.OrNull()?.HealthMaster.OrNull()?.gameObject,
			$" You feel {RelatedPart.gameObject.ExpensiveName()} starting to morph and change");

		yield return WaitFor.Seconds((SecondsForSpeciesMutation / 4f) * modifier);

		var SpawnedBodypart = Spawn.ServerPrefab(BodyPart).GameObject.GetComponent<BodyPart>();

		Chat.AddExamineMsgFromServer(RelatedPart.OrNull()?.HealthMaster.OrNull()?.gameObject,
			$" Your {RelatedPart.gameObject.ExpensiveName()} Morphs into a {SpawnedBodypart.gameObject.ExpensiveName()}");


		bool HasOpenProcedure = Enumerable.OfType<OpenProcedure>(SpawnedBodypart.SurgeryProcedureBase).Any();


		foreach (var itemSlot in RelatedPart.OrganStorage.GetItemSlots())
		{
			if (itemSlot.Item != null)
			{
				if (HasOpenProcedure)
				{
					var toSlot = SpawnedBodypart.OrganStorage;
					Inventory.ServerTransfer(itemSlot, toSlot.GetBestSlotFor(itemSlot.Item));
				}
				else
				{
					RelatedPart.OrganStorage.ServerTryRemove(itemSlot.Item.gameObject, false,
						DroppedAtWorldPositionOrThrowVector: ConverterExtensions.GetRandomRotatedVector2(-0.5f, 0.5f), Throw: true);
				}
			}
		}


		var ContainedIn = RelatedPart.HealthMaster;
		var Region = RelatedPart.BodyPartType;

		var Parent = RelatedPart.ContainedIn;


		RelatedPart.TryRemoveFromBody(CausesBleed: false, Destroy: true, PreventGibb_Death: true);
		//dropping UI slots??
		//Relink fat / stomach??


		if (Parent != null)
		{
			Inventory.ServerAdd(SpawnedBodypart.gameObject,
				Parent.OrganStorage.GetBestSlotFor(SpawnedBodypart.gameObject));
		}
		else
		{
			Inventory.ServerAdd(SpawnedBodypart.gameObject,
				ContainedIn.BodyPartStorage.GetBestSlotFor(SpawnedBodypart.gameObject));
		}


		var ONMutation = SpawnedBodypart.gameObject.GetComponent<BodyPartMutations>();

		if (ONMutation != null)
		{
			foreach (var Mutations in ActiveMutations)
			{
				ONMutation.AddMutation(Mutations.RelatedMutationSO);
			}

			ONMutation.MutateCustomisation(((BodyPartFunctionality)ONMutation).RelatedPart.SetCustomisationData,
				RelatedPart.SetCustomisationData);
		}
	}

	public PlayerHealthData PlayerHealthData;
	public GameObject TOMutateBodyPart;

	[NaughtyAttributes.Button()]
	[ContextMenu("MUTATEBODYPART")]
	public void MutateBodyPart()
	{
		ChangeToSpecies(TOMutateBodyPart);
	}

	public void OnDestroy()
	{
		ActiveMutations.Clear();
	}


	public void ChangeToSpecies(GameObject BodyPart, CharacterSheet characterSheet = null)
	{
		if (this.TryGetComponent<Brain>(out var brain)) return; //Make it a little bit harder to remove from a round
		if (characterSheet != null)
		{
			PerfomChangeToSpecies(BodyPart, characterSheet);
		}
		else
		{
			StartCoroutine(ProcessChangeToSpecies(BodyPart));
		}
	}

	private void SettingUpSubOrgans(BodyPart SpawnedBodypart, ItemStorage bodyPartExampleStorage, bool HasOpenProcedure)
	{
		var usedOrgansInSpawnedPart = new List<GameObject>();

		foreach (var x in bodyPartExampleStorage.Populater.SlotContents)
		{
			if (x.Prefab != null)
			{
				usedOrgansInSpawnedPart.Add(x.Prefab);
			}
		}
		foreach (var x in bodyPartExampleStorage.Populater.DeprecatedContents)
		{
			if (x != null)
			{
				usedOrgansInSpawnedPart.Add(x);
			}
		}

		foreach (var itemSlot in RelatedPart.OrganStorage.GetItemSlots())
		{
			if (itemSlot.Item != null)
			{
				if (HasOpenProcedure)
				{
					var toSlot = SpawnedBodypart.OrganStorage;
					GameObject organContains = null;

					foreach (var organ in usedOrgansInSpawnedPart)
					{
						if (organ != null &&
						organ.GetComponent<PrefabTracker>().ForeverID == itemSlot.Item.gameObject.GetComponent<PrefabTracker>().ForeverID)
						{
							organContains = organ;
							break;
						}
					}

					if (organContains != null)
					{
						Inventory.ServerTransfer(itemSlot, toSlot.GetBestSlotFor(itemSlot.Item));
						usedOrgansInSpawnedPart.Remove(organContains);
					}
					else
					{
						RelatedPart.OrganStorage.ServerTryRemove(itemSlot.Item.gameObject, true);
					}
				}
				else
				{
					RelatedPart.OrganStorage.ServerTryRemove(itemSlot.Item.gameObject, false,
						DroppedAtWorldPositionOrThrowVector: ConverterExtensions.GetRandomRotatedVector2(-0.5f, 0.5f), Throw: true);
				}
			}
		}

		foreach (var toSpawn in usedOrgansInSpawnedPart)
		{
			if (toSpawn != null)
			{
				var bodyPartObject = Spawn.ServerPrefab(toSpawn, spawnManualContents: true).GameObject;
				SpawnedBodypart.OrganStorage.ServerTryAdd(bodyPartObject);
			}
		}
	}

	private GameObject GetBodyPartByType(PlayerHealthData bodyParts)
	{
		switch (RelatedPart.BodyPartType)
		{
			case BodyPartType.Head:
				return bodyParts.Base.Head.Elements[0];
			case BodyPartType.Chest:
				return bodyParts.Base.Torso.Elements[0];
			case BodyPartType.LeftArm:
				return bodyParts.Base.ArmLeft.Elements[0];
			case BodyPartType.RightArm:
				return bodyParts.Base.ArmRight.Elements[0];
			case BodyPartType.LeftLeg:
				return bodyParts.Base.LegLeft.Elements[0];
			case BodyPartType.RightLeg:
				return bodyParts.Base.LegRight.Elements[0];
			default:
				return bodyParts.Base.Head.Elements[0];
		}
	}

	private void PerfomChangeToSpecies(GameObject BodyPart, CharacterSheet characterSheet = null)
	{
		var SpawnedBodypart = Spawn.ServerPrefab(BodyPart).GameObject.GetComponent<BodyPart>();

		ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);

		bool HasOpenProcedure = Enumerable.OfType<OpenProcedure>(SpawnedBodypart.SurgeryProcedureBase).Any();
		PlayerHealthData bodyParts = characterSheet.GetRaceSoNoValidation();

		SettingUpSubOrgans(SpawnedBodypart, GetBodyPartByType(bodyParts).GetComponent<ItemStorage>(), HasOpenProcedure);

		var ContainedIn = RelatedPart.HealthMaster;
		var Region = RelatedPart.BodyPartType;

		var Parent = RelatedPart.ContainedIn;

		RelatedPart.TryRemoveFromBody(CausesBleed: false, Destroy: true, PreventGibb_Death: true);

		//dropping UI slots??
		//Relink fat / stomach??.

		if (Parent != null)
		{
			Inventory.ServerAdd(SpawnedBodypart.gameObject,
				Parent.OrganStorage.GetBestSlotFor(SpawnedBodypart.gameObject));
		}
		else
		{
			Inventory.ServerAdd(SpawnedBodypart.gameObject,
				ContainedIn.BodyPartStorage.GetBestSlotFor(SpawnedBodypart.gameObject));
		}

		SpawnedBodypart.ChangeBodyPartColor(bodyColor);
	}

	public class MutationRoundData
	{
		public string SudokuPuzzle;
		public int Stability;
		public int RoundID;
		public MutationSO MutationSO;
		public PlayerHealthData PlayerHealthData;
		public int ResearchDifficult;


		public class SliderMiniGameData
		{
			public List<SliderParameters> Parameters = new List<SliderParameters>();
		}

		public void CheckValidity()
		{
			if (RoundID != GameManager.RoundID)
			{
				RerollDifficulty();
			}
		}


		public static void PopulateSliderMiniGame(SliderMiniGameData NewSliderMiniGameData, int Difficulty,
			bool CanRequireLocks)
		{
			var NumberOfSliders =
				Mathf.RoundToInt(((Difficulty / 100f) * 9f)); //9f = Max number of sliders
			if (NumberOfSliders is 0 or 1)
			{
				NumberOfSliders = 2;
			}

			NewSliderMiniGameData.Parameters.Clear();
			for (int i = 0; i < NumberOfSliders; i++)
			{
				NewSliderMiniGameData.Parameters.Add(new SliderParameters() {TargetPosition = Random.Range(5, 95)});
			}

			if (CanRequireLocks)
			{
				for (int i = 0; i < NumberOfSliders; i++)
				{
					var Related = NewSliderMiniGameData.Parameters[i];
					for (int j = 0; j < NumberOfSliders; j++)
					{
						if (i == j) continue;
						var RNG = Random.Range(0, 100);
						//75% chance of being connected
						if (RNG < 75)
						{
							var RNG3 = Random.Range(0, 2);

							if (RNG3 == 0)
							{
								Related.Parameters.Add(
									new Tuple<float, int>(Random.Range(0f, 1f) * 1, j));
							}
							else
							{
								Related.Parameters.Add(
									new Tuple<float, int>(Random.Range(0f, 1f) * -1, j));
							}
						}
					}
				}
			}
			else
			{
				var SliderPositions = new List<int>();
				for (int i = 0; i < NumberOfSliders; i++)
				{
					SliderPositions.Add(Random.Range(5, 95));
					NewSliderMiniGameData.Parameters[i].TargetLever = SliderPositions[i];
				}


				for (int i = 0; i < NumberOfSliders; i++)
				{
					var NumberOfEffectingSliders = Random.Range(1, NumberOfSliders) - 1;
					var ToLoop = NewSliderMiniGameData.Parameters.ToList().Shuffle().ToList();

					ToLoop.Remove(NewSliderMiniGameData.Parameters[i]);

					List<Tuple<float, int>> Contributing = new List<Tuple<float, int>>();

					for (int j = 0; j < NumberOfEffectingSliders; j++)
					{
						Contributing.Add(
							new Tuple<float, int>(Random.Range(-1f, 1f),
								NewSliderMiniGameData.Parameters.IndexOf(ToLoop[j])));
					}

					//the slider itself contributes to the overall
					Contributing.Add(new Tuple<float, int>(1, i));

					//so add up all Then take the difference is what you need

					float Position = 0;

					foreach (var Contribute in Contributing)
					{
						Position += Contribute.Item1 * SliderPositions[Contribute.Item2];
					}

					//  work out the difference between the actual required position
					var Difference = NewSliderMiniGameData.Parameters[i].TargetPosition - Position;


					var LastSliderPositionParameter =
						SliderPositions[NewSliderMiniGameData.Parameters.IndexOf(ToLoop[NumberOfEffectingSliders])];


					// Difference = x * LastSliderPositionParameter == 1 = x * 0.5
					// x = Difference / LastSliderPositionParameter == x = 1 / 0.5
					var Multiplier = (float) Difference / (float) LastSliderPositionParameter;

					//Set needed        \/
					Contributing.Add(new Tuple<float, int>(Multiplier,
						NewSliderMiniGameData.Parameters.IndexOf(ToLoop[NumberOfEffectingSliders])));


					foreach (var Contributer in Contributing)
					{
						NewSliderMiniGameData.Parameters[Contributer.Item2].Parameters
							.Add(new Tuple<float, int>(Contributer.Item1, i));
					}
				}
			}
		}

		public void RerollDifficulty()
		{

			this.RoundID = GameManager.RoundID;
			if (MutationSO != null)
			{
				this.ResearchDifficult =
					Mathf.RoundToInt((MutationSO.ResearchDifficult *
					                  Random.Range(0.75f, 1.25f))); //TODO Change to percentage-based system?

				this.ResearchDifficult = Mathf.Clamp(this.ResearchDifficult, 0, 100);

				this.Stability =
					Mathf.RoundToInt((MutationSO.Stability *
					                  Random.Range(0.5f, 1.5f))); //TODO Change to percentage-based system?
			}
			else
			{
				var SGen = new SudokuGenerator();
				SudokuPuzzle = SGen.generate("easy");
			}


		}

		public class SliderParameters
		{
			public int TargetPosition;
			public int TargetLever;
			public List<Tuple<float, int>> Parameters = new List<Tuple<float, int>>();
		}
	}



	/*
	so, body wide system

		Plus and minus points from positive negative,

	If it is negative gives you random negative trays until it's positive ,


	Gameplay loop will be that with multi- slider balance thing,


	Body part functionality???

	Let's say, let's say there was a stomach  script,

	How would you affect the size of stomach from

		Body part functionality??

	Without making a custom one for it

		Body part functionality, -> list possible mutations idk what type?
	-> Instantiates new, give some variables is SO,  does this stuff of it needs to be permanent
*/
}