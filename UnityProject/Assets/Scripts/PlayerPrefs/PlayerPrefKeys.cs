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
}
