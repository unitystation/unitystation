using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Learning
{
	public class TipsUI : MonoBehaviour
	{
		[SerializeField] private ScriptableObjects.StringList GeneralTipsList;

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
