using TMPro;
using UnityEngine;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Core.NtOS
{
	public class NtOS_OutputEntry : MonoBehaviour
	{
		[field:SerializeField] public TMP_Text DisplayText { get; private set; }
		[field:SerializeField] public NetText_label NetworkedText { get; private set; }
		public int OutputId { get; private set; }

		private void Awake()
		{
			DisplayText ??= gameObject.GetComponentInChildren<TMP_Text>();
			NetworkedText ??= gameObject.GetComponentInChildren<NetText_label>();
		}

		public void Setup(int outputId, string displayText)
		{
			OutputId = outputId;
			SetDisplayText(displayText);
		}

		public void SetDisplayText(string text)
		{
			NetworkedText.SetValue(text);
		}
	}
}