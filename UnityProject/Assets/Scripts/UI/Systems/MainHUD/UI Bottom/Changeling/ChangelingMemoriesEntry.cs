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
		private TMP_Text nameText = null;

		[SerializeField]
		private TMP_Text descriptionText = null;

		[SerializeField]
		private Image image = null;


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
			nameText.text = $"Memories of {data.MemoriesName}.";

			descriptionText.text = $"{data.MemoriesName} was {OccupationList.Instance.Get(data.MemoriesJob).DisplayName}.\n" +
			$"Specie was {data.MemoriesSpecies}\n gender was {data.MemoriesGender}\n" +
			$"And the memories contains";

			if (data.MemoriesObjectives == "")
			{
				descriptionText.text += $" nothing worth.";
			} else
			{
				descriptionText.text = $"\n{data.MemoriesObjectives}";
			}
			image.sprite = OccupationList.Instance.Get(data.MemoriesJob).PreviewSprite;
		}
	}
}