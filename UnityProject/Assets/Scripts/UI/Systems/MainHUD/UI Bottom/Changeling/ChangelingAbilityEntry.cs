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
			image.sprite = data.Sprites[0].Variance[0].Frames[0].sprite;
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