using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI.Chat_UI
{
	public class UIToggleChannel : MonoBehaviour
	{
		public ChatChannel Channel { get; private set; }

		[SerializeField]
		private GameObject tooltip = null;
		[SerializeField]
		private Toggle toggle = null;
		[SerializeField]
		private Text displayText = null;

		private Text tooltipText;

		private void Start()
		{
			tooltipText = tooltip.GetComponentInChildren<Text>();
			tooltipText.text = Channel.ToString();
		}

		public Toggle SetToggle(ChatChannel _channel)
		{
			Channel = _channel;
			displayText.text = IconConstants.ChatPanelIcons[Channel];

			// Use the OnClick trigger to invoke Toggle_Channel instead of OnValueChanged
			// This stops infinite loops happening when the value is changed from the code
			EventTrigger trigger = toggle.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener((eventData) => ToggleChannel());
			trigger.triggers.Add(entry);
			return toggle;
		}

		private void ToggleChannel()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			if (toggle.isOn)
			{
				ChatUI.Instance.EnableChannel(Channel);
			}
			else
			{
				ChatUI.Instance.DisableChannel(Channel);
			}
		}

		public void ToggleTooltip(bool isOn)
		{
			tooltip.SetActive(isOn);
		}
	}
}
