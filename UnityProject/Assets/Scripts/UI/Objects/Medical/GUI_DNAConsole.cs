using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Core.Net.Elements.Dynamic.Spawned;
using UI.Core.NetUI;
using UnityEngine;

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

			Logger.LogError(JsonConvert.SerializeObject(DNAMutationData));

		}

		public void GenerateElementCustomisation()
		{
			var payload = new DNAMutationData.DNAPayload();

			payload.CustomisationTarget = find.Value;
			payload.CustomisationReplaceWith = replace.Value;

			DNAStrandList.AddElement(payload, "", DNAStrandElement.Location.CustomisationMutation);
		}


		public void GenerateElementTarget()
		{
			var payload = new DNAMutationData.DNAPayload();
			DNAStrandList.AddElement(payload, target.Value, DNAStrandElement.Location.BodyPartTarget);
		}

		public void GenerateSpeciesTarget(GameObject BodyPart, PlayerHealthData SpeciesMutateTo)
		{
			var payload = new DNAMutationData.DNAPayload();
			payload.SpeciesMutateTo = SpeciesMutateTo;
			payload.MutateToBodyPart = BodyPart;

			DNAStrandList.AddElement(payload, target.Value, DNAStrandElement.Location.SpeciesMutation);
		}


		public void Start()
		{
			DNAConsole = Provider.GetComponent<DNAConsole>();

			foreach (var Species in DEBUG_Species)
			{
				DNASpeciesList.AddElement(Species , this);
			}

			foreach (var mutation in DNAConsole.UnlockedMutations)
			{

				var payload = new DNAMutationData.DNAPayload();
				payload.TargetMutationSO = mutation;
				DNAStrandList.AddElement(payload, "", DNAStrandElement.Location.Mutation);
			}

		}

		public void AddMutation(MutationSO Mutation)
		{
			DNAConsole.UnlockedMutations.Add(Mutation);
			var payload = new DNAMutationData.DNAPayload();
			payload.TargetMutationSO = Mutation;
			DNAStrandList.AddElement(payload, "", DNAStrandElement.Location.Mutation);
		}

		public void SwitchToMutationGame()
		{
			NetPageSwitcher.SetActivePage(1);
		}

		public void SwitchToVirusBuilder()
		{
			NetPageSwitcher.SetActivePage(0);
		}
	}
}

