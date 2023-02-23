using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI.Core
{
	public class DynamicChoiceEntry : MonoBehaviour
	{
		[SerializeField] private TMP_Text choiceText;
		[SerializeField] private Image choiceIcon;

		private System.Action onClickDoAction;

		public void Setup(string text, Sprite icon, System.Action actionOnClick)
		{
			onClickDoAction = actionOnClick;

			choiceText.text = text.Capitalize();
			if (icon == null)
			{
				choiceIcon.SetActive(false);
			}
			else
			{
				choiceIcon.sprite = icon;
			}
		}

		public void OnClick()
		{
			onClickDoAction?.Invoke();
			DynamicChoiceUI.Instance.OnChoiceTaken?.Invoke();
		}
	}
}