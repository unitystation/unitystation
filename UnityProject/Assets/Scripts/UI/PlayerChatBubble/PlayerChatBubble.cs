using UnityEngine;

/// <summary>
/// Handles the ChatIcon and PlayerChatBubble. 
/// Automatically checks PlayerPrefs to determine 
/// the use of each one.
/// </summary>
public class PlayerChatBubble : MonoBehaviour
{
    /// <summary>
    /// The const string of the PlayerPref key for ChatBubble preference.
    /// Use PlayerPrefs.GetInt(chatBubblePref) to determine the players
    /// preference for showing the chat bubble or not.
    /// 0 = false
    /// 1 = true
    /// </summary>
    [HideInInspector]
    public const string chatBubblePref = "ChatBubble";

    [SerializeField]
    private ChatIcon chatIcon;

    public void DetermineChatVisual(bool toggle, string message, ChatChannel chatChannel)
    {
        if (!UseChatBubble())
        {
            chatIcon.ToggleChatIcon(toggle);
        }
        else
        {
            
        }
    }

    /// <summary>
    /// Show the ChatBubble or the ChatIcon
    /// </summary>
    private bool UseChatBubble()
    {
        if (!PlayerPrefs.HasKey(chatBubblePref))
        {
            PlayerPrefs.SetInt(chatBubblePref, 0);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetInt(chatBubblePref) == 1;
    }
}