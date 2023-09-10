using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NaughtyAttributes;
using TMPro;
using AdminTools;
using Core.Chat;
using Items;
using Logs;
using Shared.Managers;
using UnityEngine.Serialization;

namespace UI.Chat_UI
{
	public class ChatUI : SingletonManager<ChatUI>
	{
		public GameObject chatInputWindow = default;
		public Transform content = default;
		public GameObject chatEntryPrefab = default;
		public int maxLogLength = 90;

		[SerializeField]
		private TMP_Text chatInputLabel = null;
		[SerializeField]
		private GameObject channelToggleTemplate = null;
		[SerializeField]
		private Image background = null;
		[SerializeField]
		private TMP_InputField InputFieldChat = null;
		[SerializeField]
		private RectTransform viewportTransform = null;

		[SerializeField, BoxGroup("Scroll Bar")]
		private Image scrollHandle = null;
		[SerializeField, BoxGroup("Scroll Bar")]
		private Image scrollBackground = null;

		[SerializeField]
		private AdminHelpChat adminHelpChat = null;
		[SerializeField]
		private AdminHelpChat mentorHelpChat = null;
		[SerializeField]
		private AdminHelpChat playerPrayerWindow = null;

		[SerializeField]
		private GameObject helpSelectionPanel = null;
		[SerializeField]
		private RectTransform chatUITransform = default;

		[SerializeField]
		private LanguageScreen languagePanel = null;
		public LanguageScreen LanguagePanel => languagePanel;

		/// <summary>The root transform for the chat UI.</summary>
		public RectTransform ChatUITransform => chatUITransform;
		private bool windowCoolDown = false;

		private ChatChannel selectedChannels;
		private int selectedVoiceLevel;

		/// <summary>
		/// Latest parsed input from input field
		/// </summary>
		private ParsedChatInput parsedInput;

		private ChatInputContext chatContext = new ChatInputContext();

		/// <summary>
		/// The currently selected chat channels. Prunes all unavailable ones on get.
		/// </summary>
		public ChatChannel SelectedChannels {
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

		//All the current chat entries in the chat feed
		private List<ChatEntry> allEntries = new List<ChatEntry>();
		private int hiddenEntries = 0;
		private bool scrollBarInteract = false;
		public event Action<bool> scrollBarEvent;
		public event System.Action checkPositionEvent;

		/// <summary>
		/// Invokes when player edited chat input field.
		/// </summary>
		public event Action<string, ChatChannel> OnChatInputChanged;

		/// <summary>
		/// Invokes when player closed chat window
		/// </summary>
		public event System.Action OnChatWindowClosed;

		[BoxGroup("Animation")] public float ChatFadeSpeed = 2f;
		[FormerlySerializedAs("ChatMinimumAlpha")] [BoxGroup("Animation"), Range(0,1)] public float ChatMinimumBackgroundAlpha = 0.5f;
		[BoxGroup("Animation")] public bool SetChatBackgroundToHiddenOnStartup = true;

		private const float FULLY_VISIBLE_ALPHA = 0.95f;


		[BoxGroup("Animation"), Range(0,1)] public float ChatContentMinimumAlpha = 0f;

		[field: SerializeField] public List<TMP_FontAsset> Fonts = new List<TMP_FontAsset>();
		public int FontIndexToUse = -1;



		public void SetPreferenceChatContent(float preference)
		{
			ChatContentMinimumAlpha = preference;
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatContentMinimumAlpha, preference);
			PlayerPrefs.Save();
		}

		public float GetPreferenceChatContent()
		{
			if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatContentMinimumAlpha))
			{
				return PlayerPrefs.GetFloat(PlayerPrefKeys.ChatContentMinimumAlpha);
			}
			else
			{
				PlayerPrefs.SetFloat(PlayerPrefKeys.ChatContentMinimumAlpha, 0);
				PlayerPrefs.Save();
				return 0f;
			}
		}

		public void SetPreferenceChatBackground(float preference)
		{
			ChatMinimumBackgroundAlpha = preference;
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBackgroundMinimumAlpha, preference);
			PlayerPrefs.Save();
		}

		public float GetPreferenceChatBackground()
		{
			if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBackgroundMinimumAlpha))
			{
				return PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBackgroundMinimumAlpha);
			}
			else
			{
				PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBackgroundMinimumAlpha, 0);
				PlayerPrefs.Save();
				return 0f;
			}
		}


		public override void Awake()
		{
			base.Awake();
			ChatMinimumBackgroundAlpha = GetPreferenceChatBackground();
			ChatContentMinimumAlpha = GetPreferenceChatContent();

			var Option =PlayerPrefs.GetString("fontPref", "LiberationSans SDF");

			for (int i = 0; i < Fonts.Count; i++)
			{
				if (Fonts[i].name == Option)
				{
					FontIndexToUse = i;
					break;
				}
			}
		}

		/// <summary>
		/// The main channels which shouldn't be active together.
		/// Local, Ghost and OOC.
		/// Order determines default selection priority so DON'T CHANGE THE ORDER!
		/// </summary>
		private static readonly List<ChatChannel> MainChannels = new List<ChatChannel>
		{
			ChatChannel.Local,

			//Blob only has access to blob so can be default
			ChatChannel.Blob,
			//Alien only has access to Alien so can be default
			ChatChannel.Alien,

			ChatChannel.Ghost,
			ChatChannel.OOC
		};

		/// <summary>
		/// Radio channels which should also broadcast to local
		/// </summary>
		private static readonly List<ChatChannel> RadioChannels = new List<ChatChannel>
		{
			ChatChannel.Common,
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
		/// OtherChannels which don't broadcast to local
		/// </summary>
		private static readonly List<ChatChannel> OtherChannels = new List<ChatChannel>
		{
			ChatChannel.Binary,
			ChatChannel.Blob,
			ChatChannel.Alien
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

		public bool Showing = false;
		public bool Animating = false;

		public override void Start()
		{
			base.Start();

			// subscribe to input fields update
			InputFieldChat.onValueChanged.AddListener(OnInputFieldChatValueChanged);

			// Create all the required channel toggles
			InitToggles();

			// Make sure the window and channel panel start disabled
			chatInputWindow.SetActive(false);
			if (SetChatBackgroundToHiddenOnStartup)
			{
				Color c = background.color;
				c.a = 0f;
				background.color = c;
			}
			//channelPanel.gameObject.SetActive(false);
			EventManager.AddHandler(Event.UpdateChatChannels, OnUpdateChatChannels);
			chatFilter = Chat.Instance.GetComponent<ChatFilter>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			EventManager.RemoveHandler(Event.UpdateChatChannels, OnUpdateChatChannels);
		}

		private void UpdateMe()
		{
			// TODO add events to inventory slot changes to trigger channel refresh
			if (chatInputWindow.activeInHierarchy && !isChannelListUpToDate())
			{
				Loggy.Log("Channel list is outdated!", Category.Chat);
				RefreshChannelPanel();
			}

			if (KeyboardInputManager.IsEnterPressed() && !windowCoolDown && chatInputWindow.activeInHierarchy)
			{
				if (UIManager.IsInputFocus)
				{
					parsedInput = Chat.ParsePlayerInput(InputFieldChat.text, chatContext);
					if (Chat.IsValidToSend(parsedInput.ClearMessage))
					{
						PlayerSendChat(parsedInput);
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
		public void AddChatEntry(string message, TMP_SpriteAsset languageSprite = null)
		{
			// Check for chat entry duplication
			if (allEntries.Count > 0 && message.Equals(allEntries[allEntries.Count - 1].Message))
			{
				allEntries[allEntries.Count - 1].AddChatDuplication();
				return;
			}

			GameObject entry = entryPool.GetChatEntry();
			var chatEntry = entry.GetComponent<ChatEntry>();
			chatEntry.ViewportTransform = viewportTransform;
			chatEntry.SetText(message, languageSprite, FontIndexToUse != -1 ? Fonts[FontIndexToUse] : null);
			allEntries.Add(chatEntry);
			SetEntryTransform(entry);
			CheckLengthOfChatLog();
			checkPositionEvent?.Invoke();
		}

		private void CheckLengthOfChatLog()
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

		public void AddMentorPrivEntry(string message)
		{
			mentorHelpChat.gameObject.SetActive(true);
			mentorHelpChat.AddChatEntry(message);
		}

		private void SetEntryTransform(GameObject entry)
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
			if ((allEntries.Count - hiddenEntries) < 20)
			{
				float fadeTime = coolDownFade ? 3f : 0f;
				scrollBackground.CrossFadeAlpha(0f, fadeTime, false);
				scrollHandle.CrossFadeAlpha(0f, fadeTime, false);
			}
			else
			{
				scrollBackground.CrossFadeAlpha(1f, 0f, false);
				scrollHandle.CrossFadeAlpha(1f, 0f, false);
			}
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

		public void OnVoiceLevelChanged(float newLevel)
		{
			selectedVoiceLevel = Mathf.RoundToInt(newLevel);
		}

		public void OnClickSend()
		{
			parsedInput = Chat.ParsePlayerInput(InputFieldChat.text, chatContext);
			if (Chat.IsValidToSend(parsedInput.ClearMessage))
			{
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
				PlayerSendChat(parsedInput);
			}

			CloseChatWindow();
		}

		private void PlayerSendChat(ParsedChatInput parsedChat)
		{
			parsedChat.ClearMessage = parsedChat.ClearMessage.Replace("\n", " ").Replace("\r", " ");  // We don't want users to spam chat vertically
			if (selectedVoiceLevel == -1)
				parsedChat.ClearMessage = "#" + parsedChat.ClearMessage;
			if (selectedVoiceLevel == 1)
				parsedChat.ClearMessage = parsedChat.ClearMessage.ToUpper();

			// Selected channels already masks all unavailable channels in it's get method
			chatFilter.Send(parsedChat, SelectedChannels);
			// The filter can be skipped / replaced by calling the following method instead:
			// PostToChatMessage.Send(sendMessage, SelectedChannels);
			InputFieldChat.text = "";

		}

		public void OnChatCancel()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			InputFieldChat.text = "";
			CloseChatWindow();
		}

		/// <summary>
		/// Opens the chat window to send messages
		/// </summary>
		/// <param name="newChannel">The chat channels to select when opening it</param>
		public void OpenChatWindow(ChatChannel newChannel = ChatChannel.None, bool inputFocus = true)
		{
			//Prevent input spam
			if (windowCoolDown || UIManager.PreventChatInput) return;
			StartWindowCooldown();
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

			EventManager.Broadcast(Event.ChatFocused);
			chatInputWindow.SetActive(true);
			Showing = true;
			StartCoroutine(AnimateBackground());
			if (inputFocus)
			{
				UIManager.IsInputFocus = true; // should work implicitly with InputFieldFocus
				EventSystem.current.SetSelectedGameObject(InputFieldChat.gameObject, null);
				InputFieldChat.OnPointerClick(new PointerEventData(EventSystem.current));
			}

			RefreshChannelPanel();
		}

		public void CloseChatWindow(bool quickClose = false)
		{
			StartWindowCooldown();
			UIManager.IsInputFocus = false;
			chatInputWindow.SetActive(false);
			languagePanel.gameObject.SetActive(false);

			EventManager.Broadcast(quickClose ? Event.ChatQuickUnfocus : Event.ChatUnfocused);

			Showing = false;
			StartCoroutine(AnimateBackground());

			UIManager.PreventChatInput = false;

			// if doesn't clear input next opening can be by OOC or other hotkey
			// That create a lot of misunderstanding and can lead to IC in OOC
			// also clears ParsedChatInput as a side effect
			InputFieldChat.text = "";

			OnChatWindowClosed?.Invoke();
		}

		#region ChatAnim


		private IEnumerator AnimateBackground()
		{
			if (Animating) yield break;

			Animating = true;

			Color color = background.color;
			while((Showing && background.color.a < FULLY_VISIBLE_ALPHA) || (Showing == false && background.color.a > 0.0001f))
			{
				yield return WaitFor.EndOfFrame;
				if (Showing)
				{
					color.a = Mathf.Lerp(color.a, FULLY_VISIBLE_ALPHA, ChatFadeSpeed * Time.deltaTime);
				}
				else
				{
					color.a = Mathf.Lerp(color.a, ChatMinimumBackgroundAlpha, ChatFadeSpeed * Time.deltaTime);
				}

				color.a = Mathf.Clamp(color.a, 0f, FULLY_VISIBLE_ALPHA);
				background.color = color;
			}
			Animating = false;

		}
		#endregion

		public void StartWindowCooldown()
		{
			if(windowCoolDown) return;

			windowCoolDown = true;
			StartCoroutine(WindowCoolDown());
		}

		private IEnumerator WindowCoolDown()
		{
			yield return WaitFor.EndOfFrame;
			windowCoolDown = false;
		}

		public void OnMouseEnter()
		{
			if (UIManager.IsInputFocus) return;
			Showing = true;
			StartCoroutine(AnimateBackground());
			OpenChatWindow( inputFocus : false );
		}

		public void OnMouseExit()
		{
			if (UIManager.IsInputFocus) return;
			Showing = false;
			StartCoroutine(AnimateBackground());
			CloseChatWindow(true);
		}

		/// <summary>
		/// Will update the toggles, active radio channels and channel text
		/// </summary>
		private void RefreshChannelPanel()
		{
			Loggy.LogTrace("Refreshing channel panel!", Category.Chat);
			Loggy.Log("Selected channels: " + ListChannels(SelectedChannels), Category.Chat);
			RefreshToggles();
			UpdateInputLabel();
		}

		public void Toggle_ChannelPanel()
		{
			showChannels = !showChannels;
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (showChannels)
			{
				//channelPanel.gameObject.SetActive(true);
				RefreshToggles();
			}
			else
			{
				//channelPanel.gameObject.SetActive(false);
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
				Loggy.LogWarning($"Channel toggle already exists for {channel}!", Category.Chat);
				return;
			}

			// Create the toggle button
			GameObject channelToggleItem =
				Instantiate(channelToggleTemplate, channelToggleTemplate.transform.parent, false);
			var uiToggleScript = channelToggleItem.GetComponentInChildren<UIToggleChannel>();

			//Set the new UIToggleChannel object and
			// Add it to a list for easy access later
			ChannelToggles.Add(channel, uiToggleScript.SetToggle(channel));
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

			foreach (ChatChannel channel in OtherChannels)
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
				GameObject toggleParent = entry.Value.transform.parent.gameObject;
				// If the channel is available activate its toggle, otherwise disable it
				toggleParent.SetActive((availChannels & toggleChannel) == toggleChannel);
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

		/// <summary>
		/// Updates the label next to the chat input field
		/// </summary>
		private void UpdateInputLabel()
		{
			var localStatus = selectedChannels.GetFlags().Any(x => RadioChannels.Contains((ChatChannel)x))
				? $"{SpeakRadioText()}" : "to nearby characters";
			if ((SelectedChannels & ChatChannel.OOC) == ChatChannel.OOC)
			{
				chatInputLabel.text = "Speaking Out Of Character (OOC):";
			}
			else if ((SelectedChannels & ChatChannel.Ghost) == ChatChannel.Ghost)
			{
				chatInputLabel.text = "Speaking as a Ghost:";
			}
			else
			{
				chatInputLabel.text = PlayerManager.
					LocalPlayerScript != null ?
					$"Say as {PlayerManager.LocalPlayerScript.visibleName} {localStatus}:"
					: "Say:";
			}
		}

		private string SpeakRadioText()
		{
			if (selectedChannels.GetFlags().Count() > 3) return "to multiple channels.";
			var speakTo = "to ";
			int count = selectedChannels.GetFlags().Count() - 1;
			int index = -1;
			foreach (var channel in selectedChannels.GetFlags())
			{
				index++;
				if (channel.ToString() == "None") continue;
				speakTo += index != count ? $"{channel.ToString()}, " : $"and {channel.ToString()} ";
			}

			return speakTo + "channels";
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
		public void EnableChannel(ChatChannel channel)
		{
			Loggy.Log($"Enabling {channel}", Category.Chat);

			if (ChannelToggles.ContainsKey(channel))
			{
				ChannelToggles[channel].isOn = true;
			}
			else
			{
				Loggy.LogWarning($"Can't enable {channel} because it isn't in ChannelToggles!", Category.Chat);
			}

			//Deselect all other channels in UI if it's a main channel
			if (MainChannels.Contains(channel))
			{
				ClearTogglesExcept(channel);
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
		public void DisableChannel(ChatChannel channel)
		{
			Loggy.Log($"Disabling {channel}", Category.Chat);

			// Special behaviour for main channels
			if (MainChannels.Contains(channel))
			{
				ClearToggles();

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

		public ChatChannel GetAvailableChannels()
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
					Loggy.Log($"Player trying to write message to channel {inputChannel}, but there are only {availChannels} avaliable;", Category.Chat);
				}

				// delete all tags from input
				InputFieldChat.text = parsedInput.ClearMessage;
			}

			OnChatInputChanged?.Invoke(rawInput, selectedChannels);
		}

		/// <summary>
		/// Opens a panel to select whether admin or mentor help is needed
		/// </summary>
		public void OnHelpButton()
		{
			CloseChatWindow();
			if (helpSelectionPanel.gameObject.activeInHierarchy)
			{
				helpSelectionPanel.gameObject.SetActive(false);
			}
			else
			{
				helpSelectionPanel.gameObject.SetActive(true);
			}
		}

		/// <summary>
		/// Opens the prayer window to pray to the gods (admins).
		/// </summary>
		public void OnPlayerPrayerButton()
		{
			CloseChatWindow();
			playerPrayerWindow.gameObject.SetActive(playerPrayerWindow.gameObject.activeInHierarchy == false);
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
				if (helpSelectionPanel != null && helpSelectionPanel.activeInHierarchy)
				{
					helpSelectionPanel.gameObject.SetActive(false);
				}
			}
		}

		/// <summary>
		/// Opens the mentor help window to talk to the mentors
		/// </summary>
		public void OnMentorHelpButton()
		{
			CloseChatWindow();
			if (mentorHelpChat.gameObject.activeInHierarchy)
			{
				mentorHelpChat.gameObject.SetActive(false);
			}
			else
			{
				mentorHelpChat.gameObject.SetActive(true);
				if (helpSelectionPanel != null && helpSelectionPanel.activeInHierarchy)
				{
					helpSelectionPanel.gameObject.SetActive(false);
				}
			}
		}

		public void OnLanguageButton()
		{
			if(PlayerManager.LocalPlayerScript == null) return;

			languagePanel.gameObject.SetActive(true);
		}
	}
}
