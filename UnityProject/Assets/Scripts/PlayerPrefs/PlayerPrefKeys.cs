public static class PlayerPrefKeys
{
	#region Account

	/// <summary>The PlayerPref key for the current user's Unitystation account email address.</summary>
	public static readonly string AccountEmail = "lastLogin";

	/// <summary>The PlayerPref key for the current user's Unitystation account username.</summary>
	public static readonly string AccountUsername = "username";

	/// <summary>The PlayerPref key for the current user's Unitystation account login token, if allowed to remember.</summary>
	public static readonly string AccountToken = "cookie";

	#endregion

	#region Player

	/// <summary>The PlayerPref key for index of the <see cref="CharacterSheet"/> the player the last used.</summary>
	public static readonly string LastCharacterIndex = "lastCharacter";

	#endregion

	/// <summary>
	/// The PlayerPref key for ChatBubble preference.
	/// Use PlayerPrefs.GetInt(chatBubblePref) to determine the players
	/// preference for showing the chat bubble or not.
	/// 0 = false
	/// 1 = true
	/// </summary>
	public static readonly string ChatBubbleKey = "ChatBubble";

	/// <summary>
	/// Camera zoom.
	/// <summary>
	public static readonly string CamZoomKey = "CamZoomSetting1";

	/// <summary>
	/// ScrollWheelZoom preference.
	/// 0 = disabled
	/// 1 = enabled
	/// <summary>
	public static readonly string ScrollWheelZoom = "ScrollWheelZoom";

	/// <summary>
	/// Ambient Volume level
	/// 0 - 1f
	/// </summary>
	public static readonly string AmbientVolumeKey = "AmbientVol";

	/// <summary>
	/// Master Volume level
	/// 0 - 1f
	/// </summary>
	public static readonly string MasterVolumeKey = "MasterVol";

	/// <summary>
	/// Sound FX Volume level
	/// 0 - 1f
	/// </summary>
	public static readonly string SoundFXVolumeKey = "SoundFXVol";

	/// <summary>
	/// TTS Toggle Pref.
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static readonly string TTSToggleKey = "TTSSetting";

	/// <summary>
	/// AudioReflections Toggle Pref.
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static readonly string AudioReflectionsToggleKey = "AudioReflectionsSetting";

	/// <summary>
	/// Returns the name of the currently selected theme name
	/// </summary>
	public static readonly string ChatBubbleThemeKey = "ChatBubbleTheme";

	/// <summary>
	/// MuteMusic toggle
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static readonly string MuteMusic = "MuteMusic";

	/// <summary>
	/// MuteMusic toggle
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static readonly string MusicVolumeKey = "MusicVol";

	/// <summary>
	/// TTS Volume level
	/// 0 - 1f
	/// </summary>
	public static readonly string TtsVolumeKey = "TTSVol";

	/// <summary>
	/// Whether or not to show highlights on items
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static readonly string EnableHighlights = "EnableHighlights";
	/// <summary>
	/// Sets the client side target frame rate preference
	/// </summary>
	public static readonly string TargetFrameRate = "TargetFrameRate";

	#region Chat Bubble

	/// <summary>
	/// Sets the normal chat bubble size preference
	/// </summary>
	public static readonly string ChatBubbleSize = "ChatBubbleSize";
	/// <summary>
	/// Whether chat bubble text appears instantly or if each character pops in
	/// </summary>
	public static readonly string ChatBubbleInstant = "ChatBubbleInstant";
	/// <summary>
	/// If chat bubble isn't instant, how quick should each character pop in
	/// </summary>
	public static readonly string ChatBubblePopInSpeed = "ChatBubblePopInSpeed";
	/// <summary>
	/// Additional time the chat bubble should stay open after all characters have popped in
	/// </summary>
	public static readonly string ChatBubbleAdditionalTime = "ChatBubbleAdditionalTime";
	/// <summary>
	/// Whether the clown text is coloured
	/// </summary>
	public static readonly string ChatBubbleClownColour = "ChatBubbleClownColour";

	#endregion

	/// <summary>
	/// VSync preference.
	/// 0 = disabled
	/// 1 = enabled, every VBlank
	/// <summary>
	public static readonly string VSyncEnabled = "EnableVSync";

	/// <summary>
	/// Hightlight character/OOC name when mentioned in chat
	/// <summary>
	public static readonly string HighlightChat = "HighlightChat";

	/// <summary>
	/// Play sound when character name mentioned in chat
	/// <summary>
	public static readonly string MentionSound = "MentionSound";

	/// <summary>
	/// Index of the selected mention sound
	/// <summary>
	public static readonly string MentionSoundIndex = "MentionSoundIndex";

	/// <summary>
	/// Allows for chat's log history to be customizable.
	/// <summary>
	public static readonly string ChatLogSize = "ChatLogSize";

	/// <summary>
	/// How loud the radio chatter sounds are
	/// 0 - 1f
	/// </summary>
	public static readonly string RadioVolumeKey = "RadioVolume";

	/// <summary>
	/// MuteMusic toggle
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static readonly string CommonRadioToggleKey = "CommonRadioToggle";


	/// <summary>
	/// Chat messages visibility
	/// 0 = Invisible
	/// 1 = Fully present at all times
	/// </summary>
	public static readonly string ChatContentMinimumAlpha = "ChatContentMinimumAlpha";


	/// <summary>
	/// Chat Background visibility
	/// 0 = Invisible
	/// 1 = Fully present at all times
	/// </summary>
	public static readonly string ChatBackgroundMinimumAlpha = "ChatBackgroundMinimumAlpha";

	public static readonly string HoverTooltipDelayKey = "hoverTooltipDelay";

	public static readonly string ThrowHoldPreference = "ThrowHoldPreference";
}
