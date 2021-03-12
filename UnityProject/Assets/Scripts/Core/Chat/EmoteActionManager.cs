using System.Collections.Generic;
using ScriptableObjects.RP;
using UnityEngine;

namespace Core.Chat
{
	public class EmoteActionManager : MonoBehaviour
	{
		[SerializeField]
		private List<EmoteSO> emotes;

		public static bool FindEmote(string emote, EmoteActionManager instance)
		{
			string[] emoteArray;
			if (emote.StartsWith("*"))
			{
				emoteArray = emote.Split('*');
			}
			else
			{
				emoteArray = emote.Split(' ');
			}
			foreach (var e in instance.emotes)
			{
				if(emoteArray[1] == e.EmoteName)
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
				if(emote == e.EmoteName)
				{
					e.Do(player);
					return;
				}
			}
		}
	}
}
