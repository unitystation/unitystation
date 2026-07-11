using System.Collections.Generic;
using Logs;
using UnityEngine;
using US13.HealthV2.Living;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic.Spawned;
using Random = UnityEngine.Random;

namespace US13.UI.Objects.Medical.genetics
{
	public class MutationUnlockMiniGame : MonoBehaviour
	{

		public GUI_DNAConsole GUI_DNAConsole;

		public BodyPartMutations.MutationRoundData.SliderMiniGameData  CurrentlySelected;

		public MutationMiniGameList MutationMiniGameList;

		public NetSlider Indicator;

		public List<MutationMiniGameElement> OrderedElements = new List<MutationMiniGameElement>();


		public void ShowSolution()
		{
			for (int i = 0; i < CurrentlySelected.Parameters.Count; i++)
			{
				var Element = OrderedElements[i]; //was: MutationMiniGameList.Entries[i] as MutationMiniGameElement
				Element.SliderLever.MasterSetValue(CurrentlySelected.Parameters[i].TargetLever.ToString());
				Element.MainSliderChangeMaster(CurrentlySelected.Parameters[i].TargetLever / 100f);
			}
		}

		public void GenerateForMutation(MutationSO Mutation)
		{
			ClearSelection();
		}

		public void GenerateForSliderMiniGameData(BodyPartMutations.MutationRoundData.SliderMiniGameData SliderMiniGameData)
		{
			OrderedElements.Clear();
			CurrentlySelected = SliderMiniGameData;
			for (int i = 0; i < SliderMiniGameData.Parameters.Count; i++)
			{
				var Element = MutationMiniGameList.AddElement(SliderMiniGameData.Parameters[i], this);
				Element.SliderIndex = i;
				OrderedElements.Add(Element);
			}
		}


		public void Start()
		{
			if (!GUI_DNAConsole.IsMasterTab) return;
			UpdateIndicator();
			GenerateNewPuzzle();
		}


		public void ClearSelection()
		{
			CurrentlySelected = null;
			var Entries= MutationMiniGameList.Entries.ToArray();
			foreach (var Entrie in Entries)
			{
				MutationMiniGameList.MasterRemoveItem(Entrie);
			}
		}

		public void ServerTryUnlock()
		{

			if (CurrentlySelected == null) return;

			bool Satisfies = true;
			foreach (var Slide in MutationMiniGameList.Entries)
			{
				var OtherElement = Slide as MutationMiniGameElement;
				if (OtherElement.SatisfiesTarget() == false)
				{
					Satisfies = false;
				}

			}

			if (Satisfies)
			{
				GUI_DNAConsole.DNAConsole.CurrentDNACharge += MutationMiniGameList.Entries.Count;
				UpdateIndicator();
				GenerateNewPuzzle();
			}
		}

		public void GenerateNewPuzzle()
		{
			ClearSelection();
			var data = new BodyPartMutations.MutationRoundData.SliderMiniGameData();
			BodyPartMutations.MutationRoundData.PopulateSliderMiniGame(data, Random.Range(25, 66), false);

			BodyPartMutations.MutationRoundData.DumpSliderMiniGameData("POST-GENERATE", data);

			GenerateForSliderMiniGameData(data);
		}

		public void UpdateIndicator()
		{
			var TargetValue = (float) Mathf.Min(GUI_DNAConsole.DNAConsole.CurrentDNACharge, GUI_DNAConsole.DNAConsole.RequiredDNASamples)  /
			                  (float) GUI_DNAConsole.DNAConsole.RequiredDNASamples;

			Indicator.SetValue(((int) (TargetValue *100)).ToString());
		}

		public void TryGenerateEgg()
		{
			if (GUI_DNAConsole.DNAConsole.CurrentDNACharge >= GUI_DNAConsole.DNAConsole.RequiredDNASamples)
			{
				GUI_DNAConsole.DNAConsole.CurrentDNACharge -= GUI_DNAConsole.DNAConsole.RequiredDNASamples;
				UpdateIndicator();
				GUI_DNAConsole.GenerateEgg();
			}
		}
	}
}
