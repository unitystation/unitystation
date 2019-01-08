using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControlChat : MonoBehaviour
{
    public static ControlChat Instance;

    private readonly List<ChatEvent> _localEvents = new List<ChatEvent> ();
    public Toggle channelListToggle;

    public RectTransform channelPanel;
    public GameObject channelToggle;
    public GameObject chatInputWindow;
    public RectTransform ChatPanel;
    public Transform content;
    public GameObject chatEntryPrefab;
    public GameObject background;
    public GameObject uiObj;

    // set in inspector (to enable/disable panel)

    public InputField InputFieldChat;

    //		public bool isChatFocus;
    public Scrollbar scrollBar;

    public bool ShowState = true;
    public InputField usernameInput;

    private void Awake ()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad (gameObject);
            uiObj.SetActive(true);
        }
        else
        {
            Destroy (gameObject); //Kill the whole tree
        }
    }

    public void AddChatEvent (ChatEvent chatEvent)
    {
        _localEvents.Add (chatEvent);
        //ChatRelay.Instance.RefreshLog();
    }

    public List<ChatEvent> GetChatEvents ()
    {
        return _localEvents;
    }

    public void Start ()
    {
        chatInputWindow.SetActive (false);
    }

    public void Update ()
    {
        if (channelPanel.gameObject.activeInHierarchy && !isChannelListUpToDate ())
        {
            RefreshChannelPanel ();
        }

        if (UIManager.IsInputFocus)
        {
            if (!string.IsNullOrEmpty (InputFieldChat.text.Trim ()) &&
                KeyboardInputManager.IsEnterPressed())
            {
                PlayerSendChat ();
                CloseChatWindow ();
            }
        }

        if (chatInputWindow.activeInHierarchy)
        {
            if (KeyboardInputManager.IsEscapePressed())
            {
                CloseChatWindow ();
            }

            if (!InputFieldChat.isFocused)
            {
                if (KeyboardInputManager.IsMovementPressed() || KeyboardInputManager.IsEscapePressed())
                {
                    CloseChatWindow ();
                }

                if (!string.IsNullOrEmpty (InputFieldChat.text.Trim ()) &&
                    KeyboardInputManager.IsEnterPressed())
                {
                    PlayerSendChat ();
                    CloseChatWindow ();
                }
            }
        }
    }

    public void OnClickSend ()
    {
        if (!string.IsNullOrEmpty (InputFieldChat.text.Trim ()))
        {
            SoundManager.Play ("Click01");
            PlayerSendChat ();
        }
        CloseChatWindow ();
    }

    private void PlayerSendChat ()
    {
        if (GameManager.Instance.GameOver)
        {
            //OOC only
            PostToChatMessage.Send (InputFieldChat.text, ChatChannel.OOC);
        }
        else
        {
            if (PlayerManager.LocalPlayerScript.playerMove.isGhost)
            {
                //dead chat only
                PostToChatMessage.Send (InputFieldChat.text, ChatChannel.Ghost);
            }
            else
            {
                PostToChatMessage.Send (InputFieldChat.text, PlayerManager.LocalPlayerScript.SelectedChannels);
            }
        }

        if (InputFieldChat.text != "")
        {
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleChatIcon (true);
        }
        InputFieldChat.text = "";
    }

    public void OnChatCancel ()
    {
        SoundManager.Play ("Click01");
        InputFieldChat.text = "";
        CloseChatWindow ();
    }

    public void OpenChatWindow (/* ChatChannel selectedChannel = ChatChannel.None */)
    {
        // TODO add ability to pass a channel to select
        EventManager.Broadcast (EVENT.ChatFocused);
        chatInputWindow.SetActive (true);
        background.SetActive (true);
        UIManager.IsInputFocus = true; // should work implicitly with InputFieldFocus
        EventSystem.current.SetSelectedGameObject (InputFieldChat.gameObject, null);
        InputFieldChat.OnPointerClick (new PointerEventData (EventSystem.current));
        // PlayerManager.LocalPlayerScript.SelectedChannels =
        UpdateChannelToggleText ();
    }
    public void CloseChatWindow ()
    {
        UIManager.IsInputFocus = false;
        chatInputWindow.SetActive (false);
        EventManager.Broadcast (EVENT.ChatUnfocused);
        background.SetActive (false);
    }

    public void RefreshChannelPanel ()
    {
        channelPanel.gameObject.SetActive (false);
        channelPanel.gameObject.SetActive (true);
    }

    public void Toggle_ChannelPannel ()
    {
        bool isOn = channelListToggle.isOn;
        //			SoundManager.Play("Click01");
        if (isOn)
        {
            channelPanel.gameObject.SetActive (true);
            PruneUnavailableChannels ();
            PopulateChannelPanel (PlayerManager.LocalPlayerScript.GetAvailableChannelsMask (),
                PlayerManager.LocalPlayerScript.SelectedChannels);
            //				Logger.Log($"Toggling channel panel ON. selected:{ListChannels(PlayerManager.LocalPlayerScript.SelectedChannels)}, " +
            //				          $"available:{ListChannels(PlayerManager.LocalPlayerScript.GetAvailableChannelsMask())}");
        }
        else
        {
            channelPanel.gameObject.SetActive (false);
            EmptyChannelPanel ();
            //				Logger.Log("Toggling channel panel OFF.");
        }
    }

    private void TrySelectDefaultChannel ()
    {
        //Try Local, then ghost, then OOC,
        var availChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask ();
        var selectedChannels = PlayerManager.LocalPlayerScript.SelectedChannels;
    }

    private void PruneUnavailableChannels ()
    {
        PlayerManager.LocalPlayerScript.SelectedChannels &= PlayerManager.LocalPlayerScript.GetAvailableChannelsMask ();
        UpdateChannelToggleText ();
    }

    /// Visualize that channel mask mess
    public static string ListChannels (ChatChannel channels, string separator = ", ")
    {
        string listChannels = string.Join (separator, EncryptionKey.getChannelsByMask (channels));
        return listChannels == "" ? "None" : listChannels;
    }

    ///Channel-Toggle map for UI things
    public Dictionary<ChatChannel, Toggle> ChannelToggles = new Dictionary<ChatChannel, Toggle> ();

    public void PopulateChannelPanel (ChatChannel channelsAvailable, ChatChannel channelsSelected)
    {
        foreach (ChatChannel currentChannel in Enum.GetValues (typeof (ChatChannel)))
        {
            if (currentChannel == ChatChannel.None || (channelsAvailable & currentChannel) != currentChannel)
            {
                continue;
            }

            GameObject channelToggleItem = Instantiate (channelToggle, channelPanel.transform);
            Toggle toggle = channelToggleItem.GetComponent<Toggle> ();
            toggle.GetComponent<UIToggleChannel> ().channel = currentChannel;
            toggle.GetComponentInChildren<Text> ().text = IconConstants.ChatPanelIcons[currentChannel];
            toggle.onValueChanged.AddListener (Toggle_Channel);

            toggle.isOn = (channelsSelected & currentChannel) == currentChannel;
            if (!ChannelToggles.ContainsKey (currentChannel))
            {
                ChannelToggles.Add (currentChannel, toggle);
            }
        }

        float width = 64f;
        int count = ChannelToggles.Count;
        LayoutElement layoutElement = channelPanel.GetComponent<LayoutElement> ();
        HorizontalLayoutGroup horizontalLayoutGroup = channelPanel.GetComponent<HorizontalLayoutGroup> ();
        layoutElement.minWidth = width * count + horizontalLayoutGroup.spacing * count;
        //			Logger.Log($"Populating wid={width} cnt={count} minWid={layoutElement.minWidth}");
    }

    public void EmptyChannelPanel ()
    {
        ChannelToggles.Clear ();
        LayoutElement layoutElement = channelPanel.GetComponent<LayoutElement> ();
        layoutElement.minWidth = 0;

        foreach (Transform child in channelPanel.transform)
        {
            Destroy (child.gameObject);
        }
    }

    public void Toggle_Channel (bool isOn)
    {
        SoundManager.Play ("Click01");
        GameObject curObject = EventSystem.current.currentSelectedGameObject;
        if (!curObject)
        {
            return;
        }

        UIToggleChannel source = curObject.GetComponent<UIToggleChannel> ();
        if (!source)
        {
            return;
        }
        ChatChannel curChannel = source.channel;

        if (isOn)
        {
            //Deselect all other channels in UI if OOC was selected
            if (curChannel == ChatChannel.OOC)
            {
                DisableAllButOOC (curChannel);
                PlayerManager.LocalPlayerScript.SelectedChannels = curChannel;
            }
            else
            {
                TryDisableOOC ();
                PlayerManager.LocalPlayerScript.SelectedChannels |= curChannel;
            }
        }
        else
        {
            PlayerManager.LocalPlayerScript.SelectedChannels &= ~curChannel;
        }

        UpdateChannelToggleText ();
    }

    private void TryDisableOOC ()
    {
        foreach (KeyValuePair<ChatChannel, Toggle> chanToggle in ChannelToggles)
        {
            if (chanToggle.Key == ChatChannel.OOC)
            {
                PlayerManager.LocalPlayerScript.SelectedChannels &= ~ChatChannel.OOC;
                chanToggle.Value.isOn = false;
            }
        }
    }

    private void DisableAllButOOC (ChatChannel channel)
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

    private void UpdateChannelToggleText ()
    {
        ChatChannel channelsSelected = PlayerManager.LocalPlayerScript.SelectedChannels;
        string channelString = ListChannels (channelsSelected, "\n");
        Text text = channelListToggle.GetComponentInChildren<Text> ();
        text.text = channelString;
    }

    private bool isChannelListUpToDate ()
    {
        ChatChannel availableChannels = PlayerManager.LocalPlayerScript.GetAvailableChannelsMask ();
        int availableCount = EnumUtils.GetSetBitCount ((long) availableChannels);
        UIToggleChannel[] displayedChannels = channelPanel.GetComponentsInChildren<UIToggleChannel> ();

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
}