using System;
using System.Collections.Generic;
using PlayGroup;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class ControlChat : MonoBehaviour
	{
		private readonly List<ChatEvent> _localEvents = new List<ChatEvent>();
		public Toggle channelListToggle;

		public RectTransform channelPanel;
		public GameObject channelToggle;
		public GameObject chatInputWindow;
		public RectTransform ChatPanel;

		public Text CurrentChannelText;
		// set in inspector (to enable/disable panel)

		public InputField InputFieldChat;

		public bool isChatFocus;
		public Scrollbar scrollBar;

		public bool ShowState = true;
		public InputField usernameInput;

		public void AddChatEvent(ChatEvent chatEvent)
		{
			_localEvents.Add(chatEvent);
			ChatRelay.Instance.RefreshLog();
		}

		public List<ChatEvent> GetChatEvents()
		{
			return _localEvents;
		}

		public void Start()
		{
			chatInputWindow.SetActive(false);
		}

		public void Update()
		{
			if (!chatInputWindow.activeInHierarchy && Input.GetKey(KeyCode.T) && GameData.IsInGame
			    && CustomNetworkManager.Instance.IsClientConnected())
			{
				chatInputWindow.SetActive(true);
				isChatFocus = true;
				EventSystem.current.SetSelectedGameObject(InputFieldChat.gameObject, null);
				InputFieldChat.OnPointerClick(new PointerEventData(EventSystem.current));
				UpdateChannelToggleText();
			}
			if (isChatFocus)
			{
				if (!string.IsNullOrEmpty(InputFieldChat.text.Trim()) &&
				    (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)))
				{
					PlayerSendChat();
					CloseChatWindow();
				}
			}

			if (channelPanel.gameObject.activeInHierarchy && !isChannelListUpToDate())
			{
				//TODO figure out how to update the channel list without it spazzing out
			}
		}

		public void OnClickSend()
		{
			if (!string.IsNullOrEmpty(InputFieldChat.text.Trim()))
			{
				SoundManager.Play("Click01");
				PlayerSendChat();
			}
			CloseChatWindow();
		}

		private void PlayerSendChat()
		{
			PostToChatMessage.Send(InputFieldChat.text, PlayerManager.LocalPlayerScript.SelectedChannels);
			if (InputFieldChat.text != "")
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(true);
			}
			InputFieldChat.text = "";
		}

		public void OnChatCancel()
		{
			SoundManager.Play("Click01");
			InputFieldChat.text = "";
			CloseChatWindow();
		}

		private void CloseChatWindow()
		{
			isChatFocus = false;
			chatInputWindow.SetActive(false);
		}

		public void Toggle_ChannelPannel(bool isOn)
		{
			SoundManager.Play("Click01");
			if (isOn)
			{
				channelPanel.gameObject.SetActive(true);
				PopulateChannelPanel(PlayerManager.LocalPlayerScript.GetAvailableChannels(),
					PlayerManager.LocalPlayerScript.SelectedChannels);
			}
			else
			{
				channelPanel.gameObject.SetActive(false);
				EmptyChannelPanel();
			}
		}

		public void PopulateChannelPanel(ChatChannel channelsAvailable, ChatChannel channelsSelected)
		{
			foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
			{
				if (channel == ChatChannel.None)
				{
					continue;
				}

				if ((channelsAvailable & channel) == channel)
				{
					GameObject channelToggleItem = Instantiate(channelToggle, channelPanel.transform);
					Toggle toggle = channelToggleItem.GetComponent<Toggle>();
					toggle.GetComponent<UIToggleChannel>().channel = channel;
					toggle.GetComponentInChildren<Text>().text = IconConstants.ChatPanelIcons[channel];
					toggle.onValueChanged.AddListener(Toggle_Channel);

					if ((channelsSelected & channel) == channel)
					{
						toggle.isOn = true;
					}
					else
					{
						toggle.isOn = false;
					}
				}
			}

			float width = channelPanel.GetChild(0).GetComponent<RectTransform>().rect.width;
			int count = channelPanel.transform.childCount;
			LayoutElement layoutElement = channelPanel.GetComponent<LayoutElement>();
			HorizontalLayoutGroup horizontalLayoutGroup = channelPanel.GetComponent<HorizontalLayoutGroup>();
			layoutElement.minWidth = width * count + horizontalLayoutGroup.spacing * count;
		}

		public void EmptyChannelPanel()
		{
			LayoutElement layoutElement = channelPanel.GetComponent<LayoutElement>();
			layoutElement.minWidth = 0;

			foreach (Transform child in channelPanel.transform)
			{
				Destroy(child.gameObject);
			}
		}

		public void Toggle_Channel(bool isOn)
		{
			SoundManager.Play("Click01");
			UIToggleChannel source = EventSystem.current.currentSelectedGameObject.GetComponent<UIToggleChannel>();
			if (!source)
			{
				return;
			}
			ChatChannel channel = source.channel;

			if (isOn)
			{
				PlayerManager.LocalPlayerScript.SelectedChannels |= channel;
			}
			else
			{
				PlayerManager.LocalPlayerScript.SelectedChannels &= ~channel;
			}

			UpdateChannelToggleText();
		}

		private void UpdateChannelToggleText()
		{
			ChatChannel channelsSelected = PlayerManager.LocalPlayerScript.SelectedChannels;
			int selectedCount = EnumUtils.GetSetBitCount((long) channelsSelected);
			Text text = channelListToggle.GetComponentInChildren<Text>();

			if (selectedCount == 1)
			{
				foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
				{
					if (channel == ChatChannel.None)
					{
						continue;
					}
					if ((channelsSelected & channel) == channel)
					{
						text.text = channel.ToString();
						return;
					}
				}
			}

			if (selectedCount == 0)
			{
				text.text = "None";
				return;
			}

			if (selectedCount > 1)
			{
				text.text = "Multiple";
			}
		}

		private bool isChannelListUpToDate()
		{
			ChatChannel availableChannels = PlayerManager.LocalPlayerScript.GetAvailableChannels();
			int availableCount = EnumUtils.GetSetBitCount((long) availableChannels);
			UIToggleChannel[] displayedChannels = channelPanel.GetComponentsInChildren<UIToggleChannel>();

			if (availableCount != displayedChannels.Length)
			{
				return false;
			}

			foreach (UIToggleChannel toggleChannel in displayedChannels)
			{
				if ((availableChannels & toggleChannel.channel) != toggleChannel.channel)
				{
					return false;
				}
			}

			return true;
		}
	}
}