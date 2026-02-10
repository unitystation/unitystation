using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Systems.Antagonists.Antags.Changeling;
using US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility;

namespace US13.UI.Systems.MainHUD.UI_Bottom.Changeling
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

		private UiChangelingStore storeUi;
		private ChangelingBaseAbility data;
		private ChangelingMain changelingMain;

		public void Init(UiChangelingStore changelingUI, ChangelingBaseAbility dataToView, ChangelingMain changeling)
		{
			storeUi = changelingUI;
			data = dataToView;
			changelingMain = changeling;
			Refresh();
		}

		public void OnBuy()
		{
			storeUi.Ui.RefreshAbilites();
			storeUi.AddAbility(data);
		}

		public void Refresh()
		{
			nameText.text = data.Name;
			descriptionText.text = data.DescriptionStore;
			image.sprite = data.Sprites[0].GetFirstSprite;
			gpCost.text = $"GP: {data.AbilityEPCost}";
			cpCost.text = $"CP: {data.AbilityChemCost}";

			buyButton.SetActive(changelingMain.EvolutionPoints - data.AbilityEPCost >= 0 && !changelingMain.HasAbility(data));

			if ((changelingMain.EvolutionPoints - data.AbilityEPCost >= 0 || changelingMain.HasAbility(data)) == true)
			{
				gpCost.color = new Color(0.1921569f, 0.3098039f, 0.172549f, 1f);
			} else
			{
				gpCost.color = Color.red;
			}
		}
	}
}