using UnityEngine;

public class Emote : MonoBehaviour {

    public string emoteName;
    public string emoteText;

    public virtual void Do(GameObject player)
    {
        Chat.AddActionMsgToChat(player, "", $"{player.name} {emoteText}.");
    }
}