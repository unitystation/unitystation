using System;
using System.Collections.Generic;
using ScriptableObjects.RP;
using UnityEngine;

namespace Core.Chat
{
	public class EmoteActionManager : Managers.SingletonManager<EmoteActionManager>
	{
		[SerializeField] private EmoteListSO emoteList;
		public EmoteListSO EmoteList => emoteList;

		public static bool HasEmote(string emote)
		{
			string[] emoteArray = emote.Split(' ');

			foreach (var e in Instance.emoteList.Emotes)
			{
				if(emoteArray[0].Equals(e.EmoteName, StringComparison.CurrentCultureIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		public static void DoEmote(string emote, GameObject player)
		{
			foreach (var e in Instance.emoteList.Emotes)
			{
				if(emote.Equals(e.EmoteName, StringComparison.CurrentCultureIgnoreCase))
				{
					e.Do(player);
					return;
				}
			}
		}

		public static void DoEmote(EmoteSO emoteSo, GameObject player)
		{
			if (emoteSo == null) return;
			emoteSo.Do(player);
		}
	}
}
