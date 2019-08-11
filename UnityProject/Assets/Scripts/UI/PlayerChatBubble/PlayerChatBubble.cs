using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField]
    private GameObject chatBubble;
    [SerializeField]
    private Text bubbleText;
    class BubbleMsg { public float maxTime; public string msg; public float elapsedTime = 0f; }
    private Queue<BubbleMsg> msgQueue = new Queue<BubbleMsg>();
    private bool showingDialogue = false;

    void Start()
    {
        chatBubble.SetActive(false);
        bubbleText.text = "";
    }

    public void DetermineChatVisual(bool toggle, string message, ChatChannel chatChannel)
    {
        if (!UseChatBubble())
        {
            chatIcon.ToggleChatIcon(toggle);
        }
        else
        {
            AddChatBubbleMsg(message, chatChannel);
        }
    }

    private void AddChatBubbleMsg(string msg, ChatChannel channel)
    {
        if (msg.Length > 82)
        {
            while (msg.Length > 82)
            {
                int ws = -1;
                //Searching for the nearest whitespace
                for (int i = 82; i >= 0; i--)
                {
                    if (char.IsWhiteSpace(msg[i]))
                    {
                        ws = i;
                        break;
                    }
                }
                //Player is spamming with no whitespace. Cut it up at index 82
                if (ws == -1 || ws == 0) ws = 82;

                var split = msg.Substring(0, ws - 1);
                msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(split.Length), msg = split });

                msg = msg.Substring(ws + 1);
                if (msg.Length <= 82)
                {
                    msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg });
                }
            }
        }
        else
        {
            msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg });
        }

        if (!showingDialogue) StartCoroutine(ShowDialogue());
    }

    IEnumerator ShowDialogue()
    {
        showingDialogue = true;
        if (msgQueue.Count == 0)
        {
            yield return WaitFor.EndOfFrame;
            yield break;
        }
        var b = msgQueue.Dequeue();
        chatBubble.SetActive(true);
        bubbleText.text = b.msg;
        while (showingDialogue)
        {
            yield return WaitFor.EndOfFrame;
            b.elapsedTime += Time.deltaTime;
            if (b.elapsedTime >= b.maxTime)
            {
                if (msgQueue.Count == 0)
                {
                    bubbleText.text = "";
                    chatBubble.SetActive(false);
                    showingDialogue = false;
                }
                else
                {
                    b = msgQueue.Dequeue();
                    bubbleText.text = b.msg;
                }
            }
        }

        yield return WaitFor.EndOfFrame;
    }

    /// <summary>
    /// Used to calculate showing length time
    /// </summary>
    private float TimeToShow(int charCount)
    {
        return Mathf.Clamp((float) charCount / 20f, 1f, 10f);
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