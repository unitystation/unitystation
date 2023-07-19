using Systems.Antagonists;
using TMPro;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;
using UnityEngine.UI;


namespace Changeling
{
	public class ChangelingMemoriesEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text alienNameText = null;

		[SerializeField]
		private TMP_Text alienDescriptionText = null;

		[SerializeField]
		private Image alienImage = null;


		private UI_ChangelingMemories ui;
		private ChangelingMemories data;
		private ChangelingMain changelingMain;

		public void Init(UI_ChangelingMemories changelingUI, ChangelingMemories dataToView, ChangelingMain changeling)
		{
			ui = changelingUI;
			data = dataToView;
			changelingMain = changeling;
			Refresh();
		}

		public void Refresh()
		{
			alienNameText.text = $"Memories of {data.MemoriesName}.";

			alienDescriptionText.text = $"{data.MemoriesName} was {OccupationList.Instance.Get(data.MemoriesJob).DisplayName}.\n" +
			$"Specie was {data.MemoriesSpecies} and gender was {data.MemoriesGender}\n" +
			$"And the memories contains";

			if (data.MemoriesObjectives == "")
			{
				alienDescriptionText.text += $" nothing worth.";
			} else
			{
				alienDescriptionText.text = $"\n{data.MemoriesObjectives}";
			}
			alienImage.sprite = OccupationList.Instance.Get(data.MemoriesJob).PreviewSprite;
		}
	}
}