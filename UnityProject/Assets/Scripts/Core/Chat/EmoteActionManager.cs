using System;
using System.Collections.Generic;
using ScriptableObjects.RP;
using UnityEngine;

namespace Core.Chat
{
	public class EmoteActionManager : MonoBehaviour
	{
		[SerializeField]
		private List<EmoteSO> emotes;

		public static bool HasEmote(string emote, EmoteActionManager instance)
		{
			string[] emoteArray = emote.Split(' ');

			foreach (var e in instance.emotes)
			{
				if(emoteArray[0].Equals(e.EmoteName, StringComparison.CurrentCultureIgnoreCase))
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
				if(emote.Equals(e.EmoteName, StringComparison.CurrentCultureIgnoreCase))
				{
					e.Do(player);
					return;
				}
			}
		}
	}
}
