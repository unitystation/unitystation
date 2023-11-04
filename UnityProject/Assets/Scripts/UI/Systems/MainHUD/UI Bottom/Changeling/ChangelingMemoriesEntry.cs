using Logs;
using TMPro;
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


		private UiChangelingMemories ui;
		private ChangelingMemories data;
		private ChangelingMain changelingMain;

		public void Init(UiChangelingMemories changelingUI, ChangelingMemories dataToView, ChangelingMain changeling)
		{
			ui = changelingUI;
			data = dataToView;
			changelingMain = changeling;
			Refresh();
		}

		public void Refresh()
		{
			nameText.text = $"Memories of {data.MemoriesName}.";

			descriptionText.text = $"{data.MemoriesName} was {data.MemoriesJob}.\n" +
			$"Specie was {data.MemoriesSpecies}\n" +
			$"Gender was {data.MemoriesGender}\n" +
			$"Pronoun was {data.MemoriesPronoun.ToString().Replace('_', ' ')}\n" +
			$"And the memories contains";

			if (data.MemoriesObjectives == "")
			{
				descriptionText.text += $" nothing worth.";
			} else
			{
				descriptionText.text += $"\n{data.MemoriesObjectives}";
			}
			try
			{
				image.sprite = OccupationList.Instance.Get(data.MemoriesJob).PreviewSprite;
			} catch
			{
				Loggy.LogError("[ChangelingMemoriesEntry/Refresh] Can`t pick preview sprite", Category.Changeling);
				image.gameObject.SetActive(false);
			}
		}
	}
}