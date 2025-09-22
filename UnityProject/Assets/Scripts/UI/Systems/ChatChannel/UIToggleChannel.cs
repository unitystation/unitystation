using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI.Chat_UI
{
	public class UIToggleChannel : MonoBehaviour
	{
		public ChatChannel Channel { get; private set; }

		[SerializeField] private Toggle toggle = null;
		[SerializeField] private GameObject tooltip = null;
		[SerializeField] private TMPro.TMP_Text displayText = null;
		[SerializeField] private TMPro.TMP_Text tooltipText;

		private void Start()
		{
			tooltipText ??= tooltip.GetComponentInChildren<TMPro.TMP_Text>();
			tooltipText.text = ConstructTooltipText();
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
			tooltipText.text = ConstructTooltipText();
		}

		private string ConstructTooltipText()
		{
			var sb = new System.Text.StringBuilder();
			sb.Append($"<color=#{Chat.GetChannelColor(Channel)}><align=\"center\">{Channel.ToString()}</align></color>\n");
			if (KeybindManager.Instance != null)
			{
				switch (Channel)
				{
					case ChatChannel.OOC:
						sb.AppendLine($"Keybind: <b>{KeybindManager.Instance.userKeybinds[KeyAction.ChatOOC].PrimaryCombo.MainKey.ToString()}</b>");
						break;
					case ChatChannel.Common:
						sb.AppendLine($"Keybind: <b>{KeybindManager.Instance.userKeybinds[KeyAction.ChatRadio].PrimaryCombo.MainKey.ToString()}</b>");
						break;
				}
			}
			var prefix = Chat.ChannelsTags.FirstOrDefault(x => x.Value == Channel);
			if (prefix.Value is not ChatChannel.Binary)
			{
				sb.AppendLine($"Prefix: <b>.{prefix.Key}</b> or <b>:{prefix.Key}</b>");
			}
			string text = sb.ToString();
			return text;
		}
	}
}
