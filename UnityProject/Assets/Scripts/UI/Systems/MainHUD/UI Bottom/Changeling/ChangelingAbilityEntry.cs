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
		private TMP_Text alienNameText = null;

		[SerializeField]
		private TMP_Text alienDescriptionText = null;

		[SerializeField]
		private TMP_Text gpCost = null;

		[SerializeField]
		private TMP_Text cpCost = null;

		[SerializeField]
		private Image alienImage = null;

		[SerializeField]
		private GameObject buyButton = null;

		private UI_ChangelingStore ui;
		private ChangelingData data;
		private ChangelingMain changelingMain;

		public void Init(UI_ChangelingStore changelingUI, ChangelingData dataToView, ChangelingMain changeling)
		{
			ui = changelingUI;
			data = dataToView;
			changelingMain = changeling;
			Refresh();
		}

		public void OnEvolve()
		{
			buyButton.SetActive(false);
			ui.AddAbility(data);
		}

		public void Refresh()
		{
			alienNameText.text = data.Name;
			alienDescriptionText.text = data.DescriptionStore;
			alienImage.sprite = data.Sprites[0].Variance[0].Frames[0].sprite;
			gpCost.text = $"GP: {data.AbilityEPCost}";
			cpCost.text = $"CP: {data.AbilityChemCost}";
			buyButton.SetActive(!changelingMain.AbilitiesNowData.Contains(data));
		}
	}
}