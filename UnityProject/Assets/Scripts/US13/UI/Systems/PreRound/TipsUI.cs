using TMPro;
using UnityEngine;
using Util;

namespace US13.UI.Systems.PreRound
{
	public class TipsUI : MonoBehaviour
	{
		[SerializeField] private global::US13.ScriptableObjects.StringList GeneralTipsList;

		[SerializeField] private TMP_Text UI_Text;

		private void Awake()
		{
			DisplayRandomTip();
		}

		public void DisplayRandomTip()
		{
			UI_Text.text = GeneralTipsList.Strings.PickRandom();
		}
	}

}
