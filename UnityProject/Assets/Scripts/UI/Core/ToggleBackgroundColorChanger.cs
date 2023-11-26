using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core
{
	public class ToggleBackgroundColorChanger : MonoBehaviour
	{
		[SerializeField] private Toggle backgrounToggle;
		[SerializeField] private TMP_Text label;

		private void Start()
		{
			StartCoroutine(Setup());
		}

		private IEnumerator Setup()
		{
			yield return WaitFor.Seconds(0.25f);
			Color color;
			if (ColorUtility.TryParseHtmlString("#" + label.text, out color))
			{
				label.fontSize = 0;
				backgrounToggle.image.color = color;
			}
			else
			{
				backgrounToggle.image.color  = Color.white;
			}
		}
	}
}