using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControlChat : MonoBehaviour
{
	public static ControlChat Instance;

	private readonly List<ChatEvent> _localEvents = new List<ChatEvent>();
	public Toggle channelListToggle;

	public RectTransform channelPanel;
	public GameObject channelToggle;
	public GameObject chatInputWindow;
	public RectTransform ChatPanel;
	public Transform content;
	public GameObject chatEntryPrefab;
	public GameObject background;
	public GameObject uiObj;
	public GameObject activeRadioChannelPanel;

	public GameObject activeChannelTemplate;
	public Dictionary<ChatChannel, GameObject> ActiveChannels = new Dictionary<ChatChannel, GameObject>();

	/// <summary>
	/// The main types of channels which shouldn't be active together.
	/// Local and OOC
	/// </summary>
	private readonly static List<ChatChannel> MainChannels = new List<ChatChannel>
	{
		ChatChannel.Local,
		ChatChannel.OOC
	};

	/// <summary>
	/// General radio channels which should also broadcast to local
	/// </summary>
	private readonly static List<ChatChannel> RadioChannels = new List<ChatChannel>
	{
		ChatChannel.Common,
		ChatChannel.Binary,
		ChatChannel.Supply,
		ChatChannel.CentComm,
		ChatChannel.Command,
		ChatChannel.Engineering,
		ChatChannel.Medical,
		ChatChannel.Science,
		ChatChannel.Security,
		ChatChannel.Service,
		ChatChannel.Syndicate
	};

	// set in inspector (to enable/disable panel)

	public InputField InputFieldChat;

	//		public bool isChatFocus;
	public Scrollbar scrollBar;

	private bool showChannels = false;
	public InputField usernameInput;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
			uiObj.SetActive(true);
		}
		else
		{
			Destroy(gameObject); //Kill the whole tree
		}
	}

	public void AddChatEvent(ChatEvent chatEvent)
	{
		_localEvents.Add(chatEvent);
		//ChatRelay.Instance.RefreshLog();
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
		if (channelPanel.gameObject.activeInHierarchy && !isChannelListUpToDate())
		{
			RefreshChannelPanel();
		}

		if (UIManager.IsInputFocus)
		{
			if (!string.IsNullOrEmpty(InputFieldChat.text.Trim()) &&
				KeyboardInputManager.IsEnterPressed())
			{
				PlayerSendChat();
				CloseChatWindow();
			}
		}

		if (!chatInputWindow.activeInHierarchy) return;
		if (KeyboardInputManager.IsEscapePressed())
		{
			CloseChatWindow();
		}

		if (InputFieldChat.isFocused) return;
		if (KeyboardInputManager.IsMovementPressed() || KeyboardInputManager.IsEscapePressed())
		{
			CloseChatWindow();
		}

		if (!string.IsNullOrEmpty(InputFieldChat.text.Trim()) &&
			KeyboardInputManager.IsEnterPressed())
		{
			PlayerSendChat();
			CloseChatWindow();
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
		if (GameManager.Instance.GameOver)
		{
			//OOC only
			PostToChatMessage.Send(InputFieldChat.text, ChatChannel.OOC);
		}
		else
		{
			if (PlayerManager.LocalPlayerScript.IsGhost)
			{
				//dead chat only
				PostToChatMessage.Send(InputFieldChat.text, ChatChannel.Ghost);
			}
			else
			{
				// Selected channels already masks all unavailable channels in it's get method
				PostToChatMessage.Send (InputFieldChat.text, PlayerManager.LocalPlayerScript.SelectedChannels);
			}
		}

		if (PlayerChatShown())
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(true);
		}
		InputFieldChat.text = "";
	}
	//Check cases where player should not have chat icon
	public bool PlayerChatShown()
	{
		// case where player is in crit
		if (PlayerManager.LocalPlayerScript.playerHealth.IsCrit)
		{
			return false;
		}
		// case where text is empty
		if (InputFieldChat.text == "")
		{
			return false;
		}
		return true;
	}

	public void OnChatCancel()
	{
		SoundManager.Play("Click01");
		InputFieldChat.text = "";
		CloseChatWindow();
	}

	public void OpenChatWindow (ChatChannel selectedChannel = ChatChannel.None)
	{
		if (PlayerManager.LocalPlayer == null)
		{
			Logger.LogWarning("You cannot use the chat without the LocalPlayer object being set in PlayerManager", Category.Telecoms);
			return;
		}
		// Change the selected channel if one is passed to the function
		if (selectedChannel != ChatChannel.None)
		{
			ClearActiveRadioChannels();
			ClearToggles();
			PlayerManager.LocalPlayerScript.SelectedChannels = selectedChannel;
			EnableChannel(selectedChannel);
			RefreshRadioChannelPanel();
		}
		EventManager.Broadcast (EVENT.ChatFocused);
		chatInputWindow.SetActive (true);
		background.SetActive (true);
		UIManager.IsInputFocus = true; // should work implicitly with InputFieldFocus
		EventSystem.current.SetSelectedGameObject (InputFieldChat.gameObject, null);
		InputFieldChat.OnPointerClick (new PointerEventData (EventSystem.current));
		UpdateChannelToggleText ();
	}
	public void CloseChatWindow()
	{
		UIManager.IsInputFocus = false;
		chatInputWindow.SetActive(false);
		EventManager.Broadcast(EVENT.ChatUnfocused);
		background.SetActive(false);
	}

	public void RefreshChannelPanel()
	{
		channelPanel.gameObject.SetActive(false);
		channelPanel.gameObject.SetActive(true);
	}

	public void Toggle_ChannelPanel()
	{
		showChannels = !showChannels;
		//			SoundManager.Play("Click01");
		if (showChannels)
		{
			channelPanel.gameObject.SetActive(true);
			PruneUnavailableChannels();
			PopulateChannelPanel(PlayerManager.LocalPlayerScript.GetAvailableChannelsMask(),
				PlayerManager.LocalPlayerScript.SelectedChannels);
			RefreshRadioChannelPanel();
			//				Logger.Log($"Toggling channel panel ON. selected:{ListChannels(PlayerManager.LocalPlayerScript.SelectedChannels)}, " +
			//				          $"available:{ListChannels(PlayerManager.LocalPlayerScript.GetAvailableChannelsMask())}");
		}
		else
		{
			channelPanel.gameObject.SetActive(false);
			EmptyChannelPanel();
			//				Logger.Log("Toggling channel panel OFF.");
		}
	}

	private void TrySelectDefaultChannel()
	{
		//Try Local, then ghost, then OOC,
		var availChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();
		var selectedChannels = PlayerManager.LocalPlayerScript.SelectedChannels;
	}

	private void PruneUnavailableChannels()
	{
		PlayerManager.LocalPlayerScript.SelectedChannels &= PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();
		UpdateChannelToggleText();
	}

	/// Visualize that channel mask mess
	public static string ListChannels(ChatChannel channels, string separator = ", ")
	{
		string listChannels = string.Join(separator, EncryptionKey.getChannelsByMask(channels));
		return listChannels == "" ? "None" : listChannels;
	}

	///Channel-Toggle map for UI things
	public Dictionary<ChatChannel, Toggle> ChannelToggles = new Dictionary<ChatChannel, Toggle>();

	public void PopulateChannelPanel(ChatChannel channelsAvailable, ChatChannel channelsSelected)
	{
		// TODO make it so the toggles don't get created and destroyed all the time
		foreach (ChatChannel currentChannel in Enum.GetValues(typeof(ChatChannel)))
		{
			// Skip the channel if it's None or isn't in the available channels
			if (currentChannel == ChatChannel.None || (channelsAvailable & currentChannel) != currentChannel)
			{
				continue;
			}

			bool channelIsOn = (channelsSelected & currentChannel) == currentChannel;

			// Create the toggle button
			GameObject channelToggleItem = Instantiate(channelToggle, channelPanel.transform);
			Toggle toggle = channelToggleItem.GetComponent<Toggle>();
			toggle.GetComponent<UIToggleChannel>().channel = currentChannel;
			toggle.GetComponentInChildren<Text>().text = IconConstants.ChatPanelIcons[currentChannel];

			// Use the OnClick trigger to invoke Toggle_Channel instead of OnValueChanged
			EventTrigger trigger = toggle.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener( (eventData) => Toggle_Channel(toggle.isOn) );
			trigger.triggers.Add(entry);

			toggle.isOn = channelIsOn;
			if (!ChannelToggles.ContainsKey(currentChannel))
			{
				ChannelToggles.Add(currentChannel, toggle);
			}

			// Create an active channel entry for radio channels
			if (RadioChannels.Contains(currentChannel) && !ActiveChannels.ContainsKey(currentChannel))
			{
				Logger.Log($"Creating radio channel entry for {currentChannel}", Category.UI);
				// Create the template object which is hidden in the list but deactivated
				GameObject radioEntry = Instantiate(activeChannelTemplate, activeChannelTemplate.transform.parent, false);

				// Setup the name and onClick function
				radioEntry.GetComponentInChildren<Text>().text = currentChannel.ToString();
				radioEntry.GetComponentInChildren<Button>().onClick.AddListener(() =>
				{
					SoundManager.Play("Click01");
					DisableChannel(currentChannel);
				});
				radioEntry.SetActive(channelIsOn);
				// Add it to a list for easy access later
				ActiveChannels.Add(currentChannel, radioEntry);
			}
		}

		float width = 64f;
		int count = ChannelToggles.Count;
		LayoutElement layoutElement = channelPanel.GetComponent<LayoutElement>();
		HorizontalLayoutGroup horizontalLayoutGroup = channelPanel.GetComponent<HorizontalLayoutGroup>();
		layoutElement.minWidth = width * count + horizontalLayoutGroup.spacing * count;
		//			Logger.Log($"Populating wid={width} cnt={count} minWid={layoutElement.minWidth}");
	}

	public void EmptyChannelPanel()
	{
		// Clear out the channel toggles
		foreach (var entry in ChannelToggles)
		{
			Destroy(entry.Value.gameObject);
		}
		ChannelToggles.Clear();
	}

	private void RefreshRadioChannelPanel()
	{
		// Enable the radio panel if radio channels are active, otherwise hide it
		foreach (var radioChannel in RadioChannels)
		{
			if (PlayerManager.LocalPlayerScript.SelectedChannels.HasFlag(radioChannel))
			{
				activeRadioChannelPanel.SetActive(true);
				return;
			}
		}
		activeRadioChannelPanel.SetActive(false);
	}

	public void Toggle_Channel(bool turnOn)
	{
		SoundManager.Play("Click01");
		GameObject curObject = EventSystem.current.currentSelectedGameObject;
		if (!curObject)
		{
			return;
		}

		UIToggleChannel source = curObject.GetComponent<UIToggleChannel>();
		if (!source)
		{
			return;
		}

		if (turnOn)
		{
			EnableChannel(source.channel);
		}
		else
		{
			DisableChannel(source.channel);
		}

		UpdateChannelToggleText();
	}

	private void TryDisableOOC()
	{
		// Disable OOC if it's on
		if (ChannelToggles.ContainsKey(ChatChannel.OOC) && ChannelToggles[ChatChannel.OOC].isOn)
		{
			PlayerManager.LocalPlayerScript.SelectedChannels &= ~ChatChannel.OOC;
			ChannelToggles[ChatChannel.OOC].isOn = false;
		}
	}

	private void ClearTogglesExcept (ChatChannel channel)
	{
		foreach (KeyValuePair<ChatChannel, Toggle> chanToggle in ChannelToggles)
		{
			if (chanToggle.Key == channel)
			{
				continue;
			}

			chanToggle.Value.isOn = false;
		}
	}

	private void ClearToggles()
	{
		foreach (var entry in ChannelToggles)
		{
			// Disable the toggle
			entry.Value.isOn = false;
		}
	}

	private void UpdateChannelToggleText()
	{
		ChatChannel channelsSelected = PlayerManager.LocalPlayerScript.SelectedChannels;
		string channelString = ListChannels(channelsSelected, "\n");
		Text text = channelListToggle.GetComponentInChildren<Text>();
		text.text = channelString;
	}

	private bool isChannelListUpToDate()
	{
		ChatChannel availableChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();
		int availableCount = EnumUtils.GetSetBitCount((long)availableChannels);
		UIToggleChannel[] displayedChannels = channelPanel.GetComponentsInChildren<UIToggleChannel>();

		if (availableCount != displayedChannels.Length)
		{
			return false;
		}

		for (var i = 0; i < displayedChannels.Length; i++)
		{
			UIToggleChannel toggleChannel = displayedChannels[i];
			if ((availableChannels & toggleChannel.channel) != toggleChannel.channel)
			{
				return false;
			}
		}

		return true;
	}

	private void ClearActiveRadioChannels()
	{
		Logger.Log("Clearing active radio channel panel", Category.UI);
		foreach (var channelEntry in ActiveChannels)
		{
			channelEntry.Value.SetActive(false);
		}
		activeRadioChannelPanel.SetActive(false);
	}

	public void EnableChannel(ChatChannel channel)
	{
		Logger.Log($"Enabling {channel}", Category.UI);

		if (ChannelToggles.ContainsKey(channel))
		{
			ChannelToggles[channel].isOn = true;
		}

		//Deselect all other channels in UI if OOC was selected
		if (channel == ChatChannel.OOC)
		{
			ClearTogglesExcept (ChatChannel.OOC);
			ClearActiveRadioChannels();
			PlayerManager.LocalPlayerScript.SelectedChannels = ChatChannel.OOC;
		}
		else
		{
			// Disable OOC and enable the local channel
			TryDisableOOC();
			PlayerManager.LocalPlayerScript.SelectedChannels |= channel;

			if (channel != ChatChannel.Local)
			{
				// Activate local channel again
				PlayerManager.LocalPlayerScript.SelectedChannels |= ChatChannel.Local;

				if (ChannelToggles.ContainsKey(channel))
				{
					ChannelToggles[ChatChannel.Local].isOn = true;
				}

				// Only add to active channel list if it's a radio channel
				ActiveChannels[channel].SetActive(true);
				activeRadioChannelPanel.SetActive(true);
			}
		}
	}

	public void DisableChannel(ChatChannel channel)
	{
		Logger.Log($"Disabling {channel}", Category.UI);

		// Special behaviour for main channels
		if (MainChannels.Contains(channel))
		{
			ClearToggles();
			ClearActiveRadioChannels();

			// Make sure toggle is still on so player can't disable them all
			ChannelToggles[channel].isOn = true;
			PlayerManager.LocalPlayerScript.SelectedChannels = channel;
		}
		else
		{
			// Remove channel from SelectedChannels and disable toggle
			PlayerManager.LocalPlayerScript.SelectedChannels &= ~channel;
			if (ChannelToggles.ContainsKey(channel))
			{
				ChannelToggles[channel].isOn = false;
			}

			if (RadioChannels.Contains(channel))
			{
				ActiveChannels[channel].SetActive(false);
				RefreshRadioChannelPanel();
			}
		}
	}

	// TODO simplify all this shit by assigning selectedchannels and refreshing everything based on that (much simpler)
	// TODO ghost testing and shit
}
