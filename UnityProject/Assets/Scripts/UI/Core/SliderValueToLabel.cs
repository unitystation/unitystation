using UnityEngine;
using UnityEngine.UI;

namespace UI.Core
{
	public class SliderValueToLabel : MonoBehaviour
	{
		[SerializeField]
		private Slider slider = null;

		[SerializeField]
		private Text label = null;

		[SerializeField]
		[Tooltip("beforeText + slider value")]
		private string beforeText = "";

		[SerializeField]
		[Tooltip("slider value + afterText")]
		private string afterText = "";

		public void OnSliderChange()
		{
			label.text = $"{beforeText} {slider.value.ToString()} {afterText}";
		}
	}
}
