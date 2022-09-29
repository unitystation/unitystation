using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Core.Net.Elements.Dynamic.Spawned;
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

		public DNASpeciesList DNASpeciesList;


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


		public void Awake()
		{
			foreach (var Species in DEBUG_Species)
			{
				DNASpeciesList.AddElement(Species);
			}
		}
	}
}

