using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Newtonsoft.Json;
using UI.Core.Net.Elements.Dynamic.Spawned;
using UI.Core.NetUI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.Objects.Medical
{
	public class GUI_DNAConsole : NetTab
	{
		public NetTMPInputField target;

		public NetTMPInputField find;
		public NetTMPInputField replace;

		public DNAStrandList DNAStrandList;

		public Transform Viewport;

		public List<PlayerHealthData> DEBUG_Species = new List<PlayerHealthData>();

		public List<MutationSO> DEBUG_Mutations = new List<MutationSO>();


		public DNASpeciesList DNASpeciesList;

		public DNAConsole DNAConsole;

		public NetPageSwitcher NetPageSwitcher;

		public NetCountdownTimer netCountdownTimer;

		public EmptyItemList MutationChoice;

		public MutationUnlockMiniGame MutationUnlockMiniGame;

		public Dictionary<MutationSO, MutationChooseElement> LoadedMutationSO =
			new Dictionary<MutationSO, MutationChooseElement>();

		public bool RemovingMutation = false;

		public void SetRemovingMutation(bool newValue)
		{
			RemovingMutation = newValue;
		}


		public void Close()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void GenerateDNA()
		{
			var DNAMutationData = new DNAMutationData();

			DNAMutationData.BodyPartSearchString = target.Value;


			for (int i = 0; i < Viewport.childCount; i++)
			{
				var Element = Viewport.GetChild(i).GetComponent<DNAStrandElement>();
				if (Element.gameObject.activeSelf == false) continue;
				var payload = Element.Payload;
				DNAMutationData.Payload.Add(payload);
			}

			var  Injector =  Spawn.ServerPrefab(DNAConsole.InjectorPrefab, DNAConsole.gameObject.AssumedWorldPosServer()).GameObject;

			var InjectorData = Injector.GetComponent<MutationInjector>();
			InjectorData.DNAPayload.Add(DNAMutationData);
		}

		public void GenerateElementCustomisation()
		{
			var payload = new DNAMutationData.DNAPayload();

			payload.CustomisationTarget = find.Value;
			payload.CustomisationReplaceWith = replace.Value;

			if (DNAStrandList.HasEntryInArea(DNAStrandElement.Location.BodyPartTarget)) return;
			DNAStrandList.AddElement(payload, "", DNAStrandElement.Location.BodyPartTarget);
		}

		public void GenerateEgg()
		{

			var positionToSpawn = this.DNAConsole.gameObject;
			if (DNAConsole.DNAScanner != null)
			{
				positionToSpawn = DNAConsole.DNAScanner.gameObject;
			}

			var  egg =  Spawn.ServerPrefab(DNAConsole.EggPrefab, positionToSpawn.AssumedWorldPosServer()).GameObject;
			var available = DNAConsole.ALLMutations.Except(DNAConsole.UnlockedMutations).ToList();

			if (available.Count == 0)
			{
				Loggy.LogError("no mutations available for egg");
			}

			var RNGamount = Random.Range(4, 6);

			var chosen = new List<MutationSO>();
			available = available.Shuffle().ToList();
			for (int i = 0; i < RNGamount; i++)
			{
				if (available.Count != 0)
				{
					var choice = available[0];
					available.RemoveAt(0);
					chosen.Add(choice);
				}
			}

			egg.GetComponent<GeneticEggLogic>().CarryingMutations = chosen;

		}

		public void GenerateMutationTarget(MutationSO Mutation)
		{
			var payload = new DNAMutationData.DNAPayload();
			if (RemovingMutation)
			{
				payload.RemoveTargetMutationSO = Mutation;
			}
			else
			{
				payload.TargetMutationSO = Mutation;
			}

			if (DNAStrandList.HasEntryInArea(DNAStrandElement.Location.BodyPartTarget)) return;
			DNAStrandList.AddElement(payload, "", DNAStrandElement.Location.BodyPartTarget);
		}

		public void GenerateElementTarget()
		{
			var payload = new DNAMutationData.DNAPayload();
			if (DNAStrandList.HasEntryInArea(DNAStrandElement.Location.BodyPartTarget)) return;
			DNAStrandList.AddElement(payload, target.Value, DNAStrandElement.Location.BodyPartTarget);
		}

		public void GenerateSpeciesTarget(GameObject BodyPart, PlayerHealthData SpeciesMutateTo)
		{
			var payload = new DNAMutationData.DNAPayload();
			payload.SpeciesMutateTo = SpeciesMutateTo;
			payload.MutateToBodyPart = BodyPart;
			if (DNAStrandList.HasEntryInArea(DNAStrandElement.Location.BodyPartTarget)) return;
			DNAStrandList.AddElement(payload, "", DNAStrandElement.Location.BodyPartTarget);
		}


		public void Start()
		{
			DNAConsole = Provider.GetComponent<DNAConsole>();
			if (IsMasterTab)
			{
				DNAConsole.ActiveGUI_DNAConsole = this;
			}


			foreach (var Species in DNAConsole.UnlockedSpecies)
			{
				DNASpeciesList.AddElement(Species , this);
			}

			foreach (var mutation in DNAConsole.UnlockedMutations)
			{
				AddMutation(mutation);
			}

		}

		public void AddMutation(MutationSO Mutation)
		{
			var MutationElement =  MutationChoice.AddItem() as MutationChooseElement;
			MutationElement.SetValues(Mutation, this);
			LoadedMutationSO[Mutation] = MutationElement;
		}


		public void UpdateMutations()
		{
			foreach (var mutation in DNAConsole.UnlockedMutations)
			{
				if (LoadedMutationSO.ContainsKey(mutation) == false)
				{
					AddMutation(mutation);
				}
			}
		}

		public void AddSpecies(PlayerHealthData species)
		{
			DNAConsole.UnlockedSpecies.Add(species);
			DNASpeciesList.AddElement(species , this);
		}


		public void SwitchToMutationGame()
		{
			NetPageSwitcher.SetActivePage(1);
		}

		public void SwitchToVirusBuilder()
		{
			NetPageSwitcher.SetActivePage(0);
		}

		public void SwitchToSpeciesUnlocker()
		{
			NetPageSwitcher.SetActivePage(2);
		}
	}
}

