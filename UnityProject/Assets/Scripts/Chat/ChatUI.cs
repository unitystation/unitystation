using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using AdminTools;
using System.Linq;

public class ChatUI : MonoBehaviour
{
	public static ChatUI Instance;
	public GameObject chatInputWindow;
	public Transform content;
	public GameObject chatEntryPrefab;
	public int maxLogLength = 90;
	[SerializeField] private Text chatInputLabel = null;
	[SerializeField] private RectTransform channelPanel = null;
	[SerializeField] private GameObject channelToggleTemplate = null;
	[SerializeField] private GameObject background = null;
	[SerializeField] private GameObject uiObj = null;
	[SerializeField] private GameObject activeRadioChannelPanel = null;
	[SerializeField] private GameObject activeChannelTemplate = null;
	[SerializeField] private InputField InputFieldChat = null;
	[SerializeField] private Transform thresholdMarkerBottom = null;
	[SerializeField] private Transform thresholdMarkerTop = null;
	[SerializeField] private AdminHelpChat adminHelpChat = null;
	private bool windowCoolDown = false;

	private ChatChannel selectedChannels;

	/// <summary>
	/// Latest parsed input from input field
	/// </summary>
	private ParsedChatInput parsedInput;

	private ChatInputContext chatContext = new ChatInputContext();

	/// <summary>
	/// The currently selected chat channels. Prunes all unavailable ones on get.
	/// </summary>
	public ChatChannel SelectedChannels
	{
		get { return selectedChannels & GetAvailableChannels(); }
		set { selectedChannels = value; }
	}

	/// <summary>
	/// The ChatLimitCPM component of the ChatSystem prefab. Cached in Start() for performance.
	/// Used for limiting the number of characters the user can send per minute.
	/// </summary>
	private ChatFilter chatFilter = null;

	/// <summary>
	/// A map of channel names and their toggles for UI manipulation
	/// </summary>
	private Dictionary<ChatChannel, Toggle> ChannelToggles = new Dictionary<ChatChannel, Toggle>();

	/// <summary>
	/// A map of channel names and their active radio channel entry for UI manipulation
	/// </summary>
	private Dictionary<ChatChannel, GameObject> ActiveChannels = new Dictionary<ChatChannel, GameObject>();

	//All the current chat entries in the chat feed
	private List<ChatEntry> allEntries = new List<ChatEntry>();
	private int hiddenEntries = 0;
	private bool scrollBarInteract = false;
	public event Action<bool> scrollBarEvent;
	public event Action checkPositionEvent;

	/// <summary>
	/// Invokes when player edited chat input field.
	/// </summary>
	public event Action<string, ChatChannel> OnChatInputChanged;

	/// <summary>
	/// Invokes when player closed chat window
	/// </summary>
	public event Action OnChatWindowClosed;

	/// <summary>
	/// The main channels which shouldn't be active together.
	/// Local, Ghost and OOC.
	/// Order determines default selection priority so DON'T CHANGE THE ORDER!
	/// </summary>
	private static readonly List<ChatChannel> MainChannels = new List<ChatChannel>
	{
		ChatChannel.Local,
		ChatChannel.Ghost,
		ChatChannel.OOC
	};

	/// <summary>
	/// Radio channels which should also broadcast to local
	/// </summary>
	private static readonly List<ChatChannel> RadioChannels = new List<ChatChannel>
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

	/// <summary>
	/// The last available set of channels. May be out of date.
	/// </summary>
	private ChatChannel availableChannelCache;

	/// <summary>
	/// Are the channel toggles on show?
	/// </summary>
	private bool showChannels = false;

	public ChatEntryPool entryPool;

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

	public void Start()
	{
		// subscribe to input fields update
		InputFieldChat.onValueChanged.AddListener(OnInputFieldChatValueChanged);

		// Create all the required channel toggles
		InitToggles();

		// Make sure the window and channel panel start disabled
		chatInputWindow.SetActive(false);
		//channelPanel.gameObject.SetActive(false);
		EventManager.AddHandler(EVENT.UpdateChatChannels, OnUpdateChatChannels);
		chatFilter = GetComponent<ChatFilter>();
	}

	private void OnDestroy()
	{
		EventManager.RemoveHandler(EVENT.UpdateChatChannels, OnUpdateChatChannels);
	}

	private void Update()
	{
		// TODO add events to inventory slot changes to trigger channel refresh
		if (chatInputWindow.activeInHierarchy && !isChannelListUpToDate())
		{
			Logger.Log("Channel list is outdated!", Category.UI);
			RefreshChannelPanel();
		}

		if (KeyboardInputManager.IsEnterPressed() && !windowCoolDown && chatInputWindow.activeInHierarchy)
		{
			if (UIManager.IsInputFocus)
			{
				parsedInput = Chat.ParsePlayerInput(InputFieldChat.text, chatContext);
				if (Chat.IsValidToSend(parsedInput.ClearMessage))
				{
					PlayerSendChat(parsedInput.ClearMessage);
				}

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
	}

	/// <summary>
	/// Only to be used via chat relay!
	/// </summary>
	public void AddChatEntry(string message)
	{
		//Check for chat entry dupes:
		if (allEntries.Count != 0)
		{
			if (message.Equals(allEntries[allEntries.Count - 1].Message))
			{
				allEntries[allEntries.Count - 1].AddChatDuplication();
				return;
			}
		}

		GameObject entry = entryPool.GetChatEntry();
		var chatEntry = entry.GetComponent<ChatEntry>();
		chatEntry.thresholdMarkerBottom = thresholdMarkerBottom;
		chatEntry.thresholdMarkerTop = thresholdMarkerTop;
		chatEntry.SetText(message);
		allEntries.Add(chatEntry);
		SetEntryTransform(entry);
		CheckLengthOfChatLog();
		checkPositionEvent?.Invoke();
	}

	void CheckLengthOfChatLog()
	{
		if (allEntries.Count >= maxLogLength)
		{
			RemoveChatEntry(allEntries[0]);
		}
	}

	/// <summary>
	/// Remove the entry if it has been culled from the
	/// list
	/// </summary>
	/// <param name="entry"></param>
	private void RemoveChatEntry(ChatEntry entry)
	{
		if (allEntries.Contains(entry))
		{
			entry.ReturnToPool();
			allEntries.Remove(entry);
		}
	}

	public void AddAdminPrivEntry(string message)
	{
		adminHelpChat.gameObject.SetActive(true);
		adminHelpChat.AddChatEntry(message);
	}

	void SetEntryTransform(GameObject entry)
	{
		entry.transform.SetParent(content, false);
		entry.transform.localScale = Vector3.one;
		entry.transform.SetAsLastSibling();
		hiddenEntries++;
		ReportEntryState(false);
	}

	public void ReportEntryState(bool isHidden, bool fromCoolDownFade = false)
	{
		if (isHidden)
		{
			if (hiddenEntries < 0) hiddenEntries = 0;
			hiddenEntries++;
			DetermineScrollBarState(fromCoolDownFade);
		}
		else
		{
			hiddenEntries--;
			DetermineScrollBarState(fromCoolDownFade);
		}
	}

	private void DetermineScrollBarState(bool coolDownFade)
	{
		// TODO revisit when we work on chat system v2
		/*
		if ((allEntries.Count - hiddenEntries) < 20)
		{
			float fadeTime = 0f;
			if (coolDownFade) fadeTime = 3f;
			scrollBackground.CrossFadeAlpha(0.01f, fadeTime, false);
			scrollHandle.CrossFadeAlpha(0.01f, fadeTime, false);
		}
		else
		{
			scrollBackground.CrossFadeAlpha(1f, 0f, false);
			scrollHandle.CrossFadeAlpha(1f, 0f, false);
		}
		*/
	}

	//This is an editor interface trigger event, do not delete
	public void OnScrollBarInteract()
	{
		scrollBarInteract = true;
		scrollBarEvent?.Invoke(scrollBarInteract);
	}

	public void OnScrollBarMove()
	{
		checkPositionEvent?.Invoke();
	}

	//This is an editor interface trigger event, do not delete
	public void OnScrollBarInteractEnd()
	{
		scrollBarInteract = false;
		scrollBarEvent?.Invoke(scrollBarInteract);
	}

	private void OnUpdateChatChannels()
	{
		TrySelectDefaultChannel();
		RefreshChannelPanel();
	}

	public void OnClickSend()
	{
		parsedInput = Chat.ParsePlayerInput(InputFieldChat.text, chatContext);
		if (Chat.IsValidToSend(parsedInput.ClearMessage))
		{
			SoundManager.Play("Click01");
			PlayerSendChat(parsedInput.ClearMessage);
		}

		CloseChatWindow();
	}

	private void PlayerSendChat(string sendMessage)
	{
		// Selected channels already masks all unavailable channels in it's get method
		chatFilter.Send(sendMessage, SelectedChannels);
		// The filter can be skipped / replaced by calling the following method instead:
		// PostToChatMessage.Send(sendMessage, SelectedChannels);
		InputFieldChat.text = "";

	}

	public void OnChatCancel()
	{
		SoundManager.Play("Click01");
		InputFieldChat.text = "";
		CloseChatWindow();
	}

	/// <summary>
	/// Opens the chat window to send messages
	/// </summary>
	/// <param name="newChannel">The chat channels to select when opening it</param>
	public void OpenChatWindow(ChatChannel newChannel = ChatChannel.None)
	{
		//Prevent input spam
		if (windowCoolDown || UIManager.PreventChatInput) return;
		windowCoolDown = true;
		StartCoroutine(WindowCoolDown());

		// Can't open chat window while main menu open
		if (GUI_IngameMenu.Instance.menuWindow.activeInHierarchy)
		{
			return;
		}

		var availChannels = GetAvailableChannels();

		// Change the selected channel if one is passed to the function and it's available
		if (newChannel != ChatChannel.None && (availChannels & newChannel) == newChannel)
		{
			EnableChannel(newChannel);
		}
		else if (SelectedChannels == ChatChannel.None)
		{
			// Make sure the player has at least one channel selected
			TrySelectDefaultChannel();
		}
		// Otherwise use the previously selected channels again

		EventManager.Broadcast(EVENT.ChatFocused);
		chatInputWindow.SetActive(true);
		background.SetActive(true);
		UIManager.IsInputFocus = true; // should work implicitly with InputFieldFocus
		EventSystem.current.SetSelectedGameObject(InputFieldChat.gameObject, null);
		InputFieldChat.OnPointerClick(new PointerEventData(EventSystem.current));
		RefreshChannelPanel();
	}

	public void CloseChatWindow()
	{
		windowCoolDown = true;
		StartCoroutine(WindowCoolDown());
		UIManager.IsInputFocus = false;
		chatInputWindow.SetActive(false);
		EventManager.Broadcast(EVENT.ChatUnfocused);
		background.SetActive(false);
		UIManager.PreventChatInput = false;

		// if doesn't clear input next opening can be by OOC or other hotkey
		// That create a lot of misunderstanding and can lead to IC in OOC
		// also clears ParsedChatInput as a side effect
		InputFieldChat.text = "";

		OnChatWindowClosed?.Invoke();
	}

	IEnumerator WindowCoolDown()
	{
		yield return WaitFor.EndOfFrame;
		windowCoolDown = false;
	}

	/// <summary>
	/// Will update the toggles, active radio channels and channel text
	/// </summary>
	private void RefreshChannelPanel()
	{
		Logger.LogTrace("Refreshing channel panel!", Category.UI);
		Logger.Log("Selected channels: " + ListChannels(SelectedChannels), Category.UI);
		RefreshToggles();
		RefreshRadioChannelPanel();
		UpdateInputLabel();
	}

	public void Toggle_ChannelPanel()
	{
		showChannels = !showChannels;
		SoundManager.Play("Click01");
		if (showChannels)
		{
			channelPanel.gameObject.SetActive(true);
			RefreshToggles();
		}
		else
		{
			channelPanel.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Try to select the most appropriate channel (Local, Ghost then OOC)
	/// </summary>
	private void TrySelectDefaultChannel()
	{
		var availChannels = GetAvailableChannels();

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

	/// <summary>
	/// Turn a ChatChannel into a string of all channels within it
	/// </summary>
	/// <returns>Returns a string of all of the channel names</returns>
	private static string ListChannels(ChatChannel channels, string separator = ", ")
	{
		string listChannels = string.Join(separator, EncryptionKey.getChannelsByMask(channels));
		return listChannels == "" ? "None" : listChannels;
	}

	/// <summary>
	/// Creates a channel toggle for the channel, and adds it to the ChannelToggles dictionary
	/// </summary>
	private void CreateToggle(ChatChannel channel)
	{
		// Check a channel toggle doesn't already exist
		if (ChannelToggles.ContainsKey(channel))
		{
			Logger.LogWarning($"Channel toggle already exists for {channel}!", Category.UI);
			return;
		}

		// Create the toggle button
		GameObject channelToggleItem =
			Instantiate(channelToggleTemplate, channelToggleTemplate.transform.parent, false);
		var uiToggleScript = channelToggleItem.GetComponent<UIToggleChannel>();

		//Set the new UIToggleChannel object and
		// Add it to a list for easy access later
		ChannelToggles.Add(channel, uiToggleScript.SetToggle(channel));
	}

	/// <summary>
	/// Creates an active radio entry for the channel, and adds it to the ActiveChannels dictionary
	/// </summary>
	private void CreateActiveRadioEntry(ChatChannel channel)
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

	/// <summary>
	/// Creates all the channel toggles
	/// </summary>
	private void InitToggles()
	{
		// Create toggles for all main and radio channels
		foreach (ChatChannel channel in MainChannels)
		{
			CreateToggle(channel);
		}

		foreach (ChatChannel channel in RadioChannels)
		{
			CreateToggle(channel);
		}
	}

	/// <summary>
	/// Will show all available channel toggles, and hide the rest
	/// </summary>
	private void RefreshToggles()
	{
		ChatChannel availChannels = GetAvailableChannels();

		foreach (var entry in ChannelToggles)
		{
			ChatChannel toggleChannel = entry.Key;
			GameObject toggle = entry.Value.gameObject;
			// If the channel is available activate it's toggle, otherwise disable it
			if ((availChannels & toggleChannel) == toggleChannel)
			{
				toggle.SetActive(true);
			}
			else
			{
				toggle.SetActive(false);
			}
		}
	}

	/// <summary>
	/// Will show the active radio channel panel if a radio channel is active, otherwise hide it
	/// </summary>
	private void RefreshRadioChannelPanel()
	{
		// Enable the radio panel if radio channels are active, otherwise hide it
		foreach (var radioChannel in RadioChannels)
		{
			// Check if the radioChannel is set in SelectedChannels
			if ((SelectedChannels & radioChannel) == radioChannel)
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
	}

	private void TryDisableOOC()
	{
		// Disable OOC if it's on
		if (ChannelToggles.ContainsKey(ChatChannel.OOC) && ChannelToggles[ChatChannel.OOC].isOn)
		{
			SelectedChannels &= ~ChatChannel.OOC;
			ChannelToggles[ChatChannel.OOC].isOn = false;
		}
	}

	private void ClearTogglesExcept(ChatChannel channel)
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

	private void ClearActiveRadioChannels()
	{
		Logger.Log("Clearing active radio channel panel", Category.UI);
		foreach (var channelEntry in ActiveChannels)
		{
			channelEntry.Value.SetActive(false);
		}

		activeRadioChannelPanel.SetActive(false);
	}

	/// <summary>
	/// Updates the label next to the chat input field
	/// </summary>
	private void UpdateInputLabel()
	{
		if ((SelectedChannels & ChatChannel.OOC) == ChatChannel.OOC)
		{
			chatInputLabel.text = "OOC:";
		}
		else if ((SelectedChannels & ChatChannel.Ghost) == ChatChannel.Ghost)
		{
			chatInputLabel.text = "Ghost:";
		}
		else
		{
			chatInputLabel.text = "Say:";
		}
	}

	/// <summary>
	/// Checks if the availableChannelCache is out of date and updates it if so
	/// </summary>
	private bool isChannelListUpToDate()
	{
		ChatChannel availableChannels = GetAvailableChannels();

		// See if available channels have changed
		if (availableChannelCache != availableChannels)
		{
			availableChannelCache = availableChannels;
			return false;
		}
		else
		{
			return true;
		}
	}

	/// <summary>
	/// Enable a channel and perform all special logic for it.
	/// Main channels disable all other channels, and radio channels enable local too
	/// </summary>
	private void EnableChannel(ChatChannel channel)
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
			SelectedChannels = channel;
		}
		else
		{
			// Disable OOC and enable the channel
			TryDisableOOC();
			SelectedChannels |= channel;

			// Only enable local if it's a radio channel
			if (RadioChannels.Contains(channel))
			{
				// Activate local channel again
				SelectedChannels |= ChatChannel.Local;

				if (ChannelToggles.ContainsKey(channel))
				{
					ChannelToggles[ChatChannel.Local].isOn = true;
				}

				// Only add to active channel list if it's a radio channel
				if (!ActiveChannels.ContainsKey(channel))
				{
					CreateActiveRadioEntry(channel);
				}

				ActiveChannels[channel].SetActive(true);
				activeRadioChannelPanel.SetActive(true);
			}
		}

		UpdateInputLabel();
	}

	/// <summary>
	/// Disable all selected chanels except main channels
	/// </summary>
	private void DisableAllChanels()
	{
		/*var selectedEnumerable = SelectedChannels.GetFlags();
		foreach (ChatChannel selected in selectedEnumerable)
		{
			if (!MainChannels.Contains(selected))
				DisableChannel(selected);
		}*/
	}

	/// <summary>
	/// Disable a channel and perform all special logic for it.
	/// Main channels can't be disabled, and radio channels can hide the active radio channel panel
	/// </summary>
	private void DisableChannel(ChatChannel channel)
	{
		Logger.Log($"Disabling {channel}", Category.UI);

		// Special behaviour for main channels
		if (MainChannels.Contains(channel))
		{
			ClearToggles();
			ClearActiveRadioChannels();

			// Make sure toggle is still on so player can't disable them all
			ChannelToggles[channel].isOn = true;
			SelectedChannels = channel;
		}
		else
		{
			// Remove channel from SelectedChannels and disable toggle
			SelectedChannels &= ~channel;
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

		// check if channel was forced by tag
		/*if (channel == prevTagSelectedChannel && channel != ChatChannel.None)
		{
			// delete tag from input field
			if (parsedInput != null)
				InputFieldChat.text = parsedInput.ClearMessage;
		}*/

		UpdateInputLabel();
	}

	private ChatChannel GetAvailableChannels()
	{
		if (PlayerManager.LocalPlayerScript == null)
		{
			return ChatChannel.OOC;
		}
		else
		{
			return PlayerManager.LocalPlayerScript.GetAvailableChannelsMask();
		}
	}

	private void OnInputFieldChatValueChanged(string rawInput)
	{
		// update parsed input
		parsedInput = Chat.ParsePlayerInput(rawInput, chatContext);
		var inputChannel = parsedInput.ParsedChannel;

		// Check if player typed new channel shotrcut (for instance ';' or ':e')
		if (inputChannel != ChatChannel.None)
		{
			// check if entered channel avaliable for player
			var availChannels = GetAvailableChannels();
			if (availChannels.HasFlag(inputChannel))
			{
				EnableChannel(inputChannel);
			}
			else
			{
				// TODO: need some addition UX indication that channel is not avaliable
				Logger.Log($"Player trying to write message to channel {inputChannel}, but there are only {availChannels} avaliable;", Category.UI);
			}

			// delete all tags from input
			InputFieldChat.text = parsedInput.ClearMessage;
		}

		OnChatInputChanged?.Invoke(rawInput, selectedChannels);
	}

	/// <summary>
	/// Opens the admin help window to talk to the admins
	/// </summary>
	public void OnAdminHelpButton()
	{
		CloseChatWindow();
		if (adminHelpChat.gameObject.activeInHierarchy)
		{
			adminHelpChat.gameObject.SetActive(false);
		}
		else
		{
			adminHelpChat.gameObject.SetActive(true);
		}
	}
}