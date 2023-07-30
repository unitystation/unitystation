using Systems.Antagonists;
using TMPro;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;
using UnityEngine.UI;

namespace Changeling
{
	public class ChangelingAbilityEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text nameText = null;

		[SerializeField]
		private TMP_Text descriptionText = null;

		[SerializeField]
		private TMP_Text gpCost = null;

		[SerializeField]
		private TMP_Text cpCost = null;

		[SerializeField]
		private Image image = null;

		[SerializeField]
		private GameObject buyButton = null;

		private UiChangelingStore ui;
		private ChangelingData data;
		private ChangelingMain changelingMain;

		public void Init(UiChangelingStore changelingUI, ChangelingData dataToView, ChangelingMain changeling)
		{
			ui = changelingUI;
			data = dataToView;
			changelingMain = changeling;
			Refresh();
		}

		public void OnBuy()
		{
			buyButton.SetActive(false);
			ui.AddAbility(data);
		}

		public void Refresh()
		{
			nameText.text = data.Name;
			descriptionText.text = data.DescriptionStore;
			image.sprite = data.Sprites[0].Variance[0].Frames[0].sprite;
			gpCost.text = $"GP: {data.AbilityEPCost}";
			cpCost.text = $"CP: {data.AbilityChemCost}";
			buyButton.SetActive(!changelingMain.AbilitiesNowData.Contains(data));
		}
	}
}