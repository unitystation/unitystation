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
	/// Camera zoom. Keep values within 0 - 10 range
	/// 0 = AutoZoom
	/// <summary>
	public static string CamZoomKey = "CamZoomSetting";

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
	/// ClientID is a unique identifier that allows a player to reclaim
	/// their body on a reconnect. An id is given to each client so that
	/// we are not relying on SteamID as the identifier.
	/// </summary>
	public static string ClientID = "ClientID";

	/// <summary>
	/// MuteMusic toggle
	/// 0 = disabled
	/// 1 = enabled
	/// </summary>
	public static string MuteMusic = "MuteMusic";


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
}