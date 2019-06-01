using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ControlChat : MonoBehaviour
{
	public static ControlChat Instance;

	private readonly List<ChatEvent> _localEvents = new List<ChatEvent>();
	[FormerlySerializedAs("channelListToggle")]
	public Toggle chatInputLabel;

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
	/// The main channels which shouldn't be active together.
	/// Local, Ghost and OOC.
	/// Order determines default selection priority so DON'T CHANGE THE ORDER!
	/// </summary>
	private readonly static List<ChatChannel> MainChannels = new List<ChatChannel>
	{
		ChatChannel.Local,
		ChatChannel.Ghost,
		ChatChannel.OOC
	};

	/// <summary>
	/// Radio channels which should also broadcast to local
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

	public InputField InputFieldChat;
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
		EventManager.AddHandler(EVENT.UpdateChatChannels, OnUpdateChatChannels);
	}

	private void OnDestroy()
	{
		EventManager.RemoveHandler(EVENT.UpdateChatChannels, OnUpdateChatChannels);
	}

	public void Update()
	{
		// if (channelPanel.gameObject.activeInHierarchy && !isChannelListUpToDate())
		// {
			// RefreshChannelPanel();
		// }

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

	private void OnUpdateChatChannels()
	{
		// PopulateChannelPanel(PlayerManager.LocalPlayerScript.GetAvailableChannelsMask(),
		// 		PlayerManager.LocalPlayerScript.SelectedChannels);
		TrySelectDefaultChannel();
		RefreshChannelPanel();
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
		// Selected channels already masks all unavailable channels in it's get method
		PostToChatMessage.Send (InputFieldChat.text, PlayerManager.LocalPlayerScript.SelectedChannels);

		// if (GameManager.Instance.GameOver)
		// {
		// 	//OOC only
		// 	PostToChatMessage.Send(InputFieldChat.text, ChatChannel.OOC);
		// }
		// else
		// {
		// 	if (PlayerManager.LocalPlayerScript.IsGhost)
		// 	{
		// 		//dead chat only
		// 		PostToChatMessage.Send(InputFieldChat.text, ChatChannel.Ghost);
		// 	}
		// 	else
		// 	{
		// 		// Selected channels already masks all unavailable channels in it's get method
		// 		PostToChatMessage.Send (InputFieldChat.text, PlayerManager.LocalPlayerScript.SelectedChannels);
		// 	}
		// }

		if (PlayerChatShown())
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon(true);
		}
		InputFieldChat.text = "";
	}

	/// <summary>
	/// Check if player should show a speech balloon
	/// </summary>
	public bool PlayerChatShown()
	{
		// Don't show if player is dead, crit or sent an empty message
		if (PlayerManager.LocalPlayerScript.IsGhost ||
			PlayerManager.LocalPlayerScript.playerHealth.IsCrit ||
			InputFieldChat.text == ""
			)
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
		var availChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();

		// Change the selected channel if one is passed to the function and it's available
		if (selectedChannel != ChatChannel.None && (availChannels & selectedChannel) == selectedChannel)
		{
			EnableChannel(selectedChannel);
			RefreshRadioChannelPanel();
			RefreshChannelPanel();
		}
		else
		{
			TrySelectDefaultChannel();
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
		Logger.LogTrace("Refreshing channel panel!", Category.UI);

		// Empty everything
		EmptyChannelPanel();

		// Repopulate the channel toggles if it's on show
		if (showChannels)
		{
			PopulateChannelPanel(PlayerManager.LocalPlayerScript.GetAvailableChannelsMask(),
				PlayerManager.LocalPlayerScript.SelectedChannels);
		}
		RefreshRadioChannelPanel();
		UpdateChannelToggleText();
	}

	public void Toggle_ChannelPanel()
	{
		showChannels = !showChannels;
		//			SoundManager.Play("Click01");
		if (showChannels)
		{
			channelPanel.gameObject.SetActive(true);
			// PruneUnavailableChannels();
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

	/// <summary>
	/// Tried to select the most appropriate channel (Local, Ghost then OOC)
	/// </summary>
	private void TrySelectDefaultChannel()
	{
		var availChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();

		// Relies on the order of the channels being Local, Ghost then OOC!
		foreach (ChatChannel channel in MainChannels)
		{
			// Check if channel is available
			if ((availChannels & channel) == channel)
			{
				EnableChannel(channel);
				return;
			}
		}
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
		// FIXME: make it so the toggles don't get created and destroyed all the time
		foreach (ChatChannel currentChannel in Enum.GetValues(typeof(ChatChannel)))
		{
			// Skip the channel if it's None or isn't in the available channels
			if (currentChannel == ChatChannel.None || (channelsAvailable & currentChannel) != currentChannel)
			{
				continue;
			}

			// Create a channel toggle if one doesn't already exist
			if (!ChannelToggles.ContainsKey(currentChannel))
			{
				CreateToggle(currentChannel);
			}
			// Create an active channel entry if it's a radio channel
			if (RadioChannels.Contains(currentChannel) && !ActiveChannels.ContainsKey(currentChannel))
			{
				CreateRadioEntry(currentChannel);
			}

			// If the channel is currently selected enable it
			if ((channelsSelected & currentChannel) == currentChannel)
			{
				EnableChannel(currentChannel);
			}
		}

		float width = 64f;
		int count = ChannelToggles.Count;
		LayoutElement layoutElement = channelPanel.GetComponent<LayoutElement>();
		HorizontalLayoutGroup horizontalLayoutGroup = channelPanel.GetComponent<HorizontalLayoutGroup>();
		layoutElement.minWidth = width * count + horizontalLayoutGroup.spacing * count;
		//			Logger.Log($"Populating wid={width} cnt={count} minWid={layoutElement.minWidth}");
	}

	private void CreateToggle(ChatChannel channel)
	{
		Logger.Log($"Creating channel toggle for {channel}", Category.UI);
		// Create the toggle button
		GameObject channelToggleItem = Instantiate(channelToggle, channelPanel.transform);
		Toggle toggle = channelToggleItem.GetComponent<Toggle>();
		toggle.GetComponent<UIToggleChannel>().channel = channel;
		toggle.GetComponentInChildren<Text>().text = IconConstants.ChatPanelIcons[channel];

		// Use the OnClick trigger to invoke Toggle_Channel instead of OnValueChanged
		// This stops infinite loops happening when the value is changed from the code
		EventTrigger trigger = toggle.GetComponent<EventTrigger>();
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = EventTriggerType.PointerClick;
		entry.callback.AddListener( (eventData) => Toggle_Channel(toggle.isOn) );
		trigger.triggers.Add(entry);

		ChannelToggles.Add(channel, toggle);
	}

	private void CreateRadioEntry(ChatChannel channel)
	{
		Logger.Log($"Creating radio channel entry for {channel}", Category.UI);
		// Create the template object which is hidden in the list but deactivated
		GameObject radioEntry = Instantiate(activeChannelTemplate, activeChannelTemplate.transform.parent, false);

		// Setup the name and onClick function
		radioEntry.GetComponentInChildren<Text>().text = channel.ToString();
		radioEntry.GetComponentInChildren<Button>().onClick.AddListener(() =>
		{
			SoundManager.Play("Click01");
			DisableChannel(channel);
		});
		// Add it to a list for easy access later
		ActiveChannels.Add(channel, radioEntry);
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
			// Check if the radioChannel is set in SelectedChannels
			if ((PlayerManager.LocalPlayerScript.SelectedChannels & radioChannel) == radioChannel)
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
		Text text = chatInputLabel.GetComponentInChildren<Text>();
		text.text = channelString;
	}

	private bool isChannelListUpToDate()
	{
		ChatChannel availableChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();
		int availableCount = EnumUtils.GetSetBitCount((long)availableChannels);

		// Check the lengths for cheap comparison
		if (availableCount != ChannelToggles.Count)
		{
			return false;
		}

		// Check all the individual channels match if the length is the same
		foreach (var entry in ChannelToggles)
		{
			ChatChannel channel = entry.Key;
			if ((availableChannels & channel) != channel)
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
		else
		{
			Logger.LogWarning($"Can't enable {channel} because it isn't in ChannelToggles!");
		}

		//Deselect all other channels in UI if it's a main channel
		if (MainChannels.Contains(channel))
		{
			ClearTogglesExcept(channel);
			ClearActiveRadioChannels();
			PlayerManager.LocalPlayerScript.SelectedChannels = channel;
		}
		else
		{
			// Disable OOC and enable the channel
			TryDisableOOC();
			PlayerManager.LocalPlayerScript.SelectedChannels |= channel;

			// Only enable local if it's a radio channel
			if (RadioChannels.Contains(channel))
			{
				// Activate local channel again
				PlayerManager.LocalPlayerScript.SelectedChannels |= ChatChannel.Local;

				if (ChannelToggles.ContainsKey(channel))
				{
					ChannelToggles[ChatChannel.Local].isOn = true;
				}

				// Only add to active channel list if it's a radio channel
				if (!ActiveChannels.ContainsKey(channel))
				{
					CreateRadioEntry(channel);
				}
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
}
