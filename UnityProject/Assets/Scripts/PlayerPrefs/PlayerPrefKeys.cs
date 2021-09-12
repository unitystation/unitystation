public static class PlayerPrefKeys
{
	/// <summary>
	/// The PlayerPref key for ChatBubble preference.
	/// Use PlayerPrefs.GetInt(chatBubblePref) to determine the players
	/// preference for showing the chat bubble or not.
	/// 0 = false
	/// 1 = true
	/// </summary>
	public static string ChatBubbleKey = "ChatBubble";

	/// <summary>
	/// Camera zoom.
	/// <summary>
	public static string CamZoomKey = "CamZoomSetting1";

	/// <summary>
	/// ScrollWheelZoom preference.
	/// 0 = disabled
	/// 1 = enabled
	/// <summary>
	public static string ScrollWheelZoom = "ScrollWheelZoom";

	/// <summary>
	/// Ambient Volume level
	/// 0 - 1f
	/// </summary>
	public static string AmbientVolumeKey = "AmbientVol";

	/// <summary>
	/// Master Volume level
	/// 0 - 1f
	/// </summary>
	public static string MasterVolumeKey = "MasterVol";

	/// <summary>
	/// TTS Toggle Pref.
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static string TTSToggleKey = "TTSSetting";

	/// <summary>
	/// Returns the name of the currently selected theme name
	/// </summary>
	public static string ChatBubbleThemeKey = "ChatBubbleTheme";

	/// <summary>
	/// MuteMusic toggle
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static string MuteMusic = "MuteMusic";

	/// <summary>
	/// MuteMusic toggle
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static string MusicVolume = "MusicVolume";

	/// <summary>
	/// Whether or not to show highlights on items
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static string EnableHighlights = "EnableHighlights";
	/// <summary>
	/// Sets the client side target frame rate preference
	/// </summary>
	public static string TargetFrameRate = "TargetFrameRate";

	/// <summary>
	/// Sets the normal chat bubble size preference
	/// </summary>
	public static string ChatBubbleSize = "ChatBubbleSize";
	/// <summary>
	/// Whether chat bubble text appears instantly or if each character pops in
	/// </summary>
	public static string ChatBubbleInstant = "ChatBubbleInstant";
	/// <summary>
	/// If chat bubble isn't instant, how quick should each character pop in
	/// </summary>
	public static string ChatBubblePopInSpeed = "ChatBubblePopInSpeed";
	/// <summary>
	/// Additional time the chat bubble should stay open after all characters have popped in
	/// </summary>
	public static string ChatBubbleAdditionalTime = "ChatBubbleAdditionalTime";
	/// <summary>
	/// Whether the clown text is coloured
	/// </summary>
	public static string ChatBubbleClownColour = "ChatBubbleClownColour";

	/// <summary>
	/// VSync preference.
	/// 0 = disabled
	/// 1 = enabled, every VBlank
	/// <summary>
	public static string VSyncEnabled = "EnableVSync";

	/// <summary>
	/// Hightlight character/OOC name when mentioned in chat
	/// <summary>
	public static string HighlightChat = "HighlightChat";

	/// <summary>
	/// Play sound when character name mentioned in chat
	/// <summary>
	public static string MentionSound = "MentionSound";

	/// <summary>
	/// Index of the selected mention sound
	/// <summary>
	public static string MentionSoundIndex = "MentionSoundIndex";

	/// <summary>
	/// Allows for chat's log history to be customizable.
	/// <summary>
	public static string ChatLogSize = "ChatLogSize";
}