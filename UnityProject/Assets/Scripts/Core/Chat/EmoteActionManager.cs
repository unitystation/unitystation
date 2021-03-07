using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmoteActionManager : MonoBehaviour
{
    [SerializeField]
    private List<EmoteSO> emotes;

    public static bool FindEmote(string emote, EmoteActionManager instance)
    {
        foreach (var e in instance.emotes)
        {
            if(e.emoteName == emote)
            {
                return true;
            }
            if(emote.Contains(e.emoteName))
            {
                return true;
            }
        }
        return false;
    }

    public static void DoEmote(string emote, GameObject player, EmoteActionManager instance)
    {
        foreach (var e in instance.emotes)
        {
            if(emote.Contains(e.emoteName))
            {
                e.Do(player);
                return;
            }
        }
    }
}
