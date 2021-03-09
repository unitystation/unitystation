using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmoteActionManager : MonoBehaviour
{
    [SerializeField]
    private List<EmoteSO> emotes;

    public static bool FindEmote(string emote, EmoteActionManager instance)
    {
		var emoteArray = emote.Split(' ');
        foreach (var e in instance.emotes)
        {
            if(emoteArray[1] == e.emoteName)
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
            if(emote == e.emoteName)
            {
                e.Do(player);
                return;
            }
        }
    }
}
