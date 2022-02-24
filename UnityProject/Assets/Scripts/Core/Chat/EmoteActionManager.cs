using System;
using System.Collections.Generic;
using ScriptableObjects.RP;
using UnityEngine;

namespace Core.Chat
{
	public class EmoteActionManager : MonoBehaviour
	{
		public static EmoteActionManager Instance;
		[SerializeField] private EmoteListSO emoteList;

		private void Awake()
		{
			Instance = this;
		}

		public static bool HasEmote(string emote, EmoteActionManager instance)
		{
			string[] emoteArray = emote.Split(' ');

			foreach (var e in instance.emoteList.Emotes)
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
			foreach (var e in instance.emoteList.Emotes)
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
			foreach (var emote in Instance.emoteList.Emotes)
			{
				if(emote != emoteSo) continue;
				emote.Do(player);
			}
		}
	}
}
