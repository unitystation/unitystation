using System;
using System.Collections.Generic;
using ScriptableObjects.RP;
using Shared.Managers;
using UI.Core;
using UnityEngine;

namespace Core.Chat
{
	public class EmoteActionManager : SingletonManager<EmoteActionManager>
	{
		[SerializeField] private EmoteListSO emoteList;
		public EmoteListSO EmoteList => emoteList;


		public override void Start()
		{
			base.Start();
			UpdateManager.Add(CallbackType.UPDATE, CheckForInputForEmoteWindow);
		}

		public override void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.UPDATE, CheckForInputForEmoteWindow);
			base.OnDestroy();
		}

		private void CheckForInputForEmoteWindow()
		{
			if (PlayerManager.LocalPlayerObject == null) return;
			if (IsPressingEmoteWindowInput() == false) return;
			var choices = new List<DynamicUIChoiceEntryData>();
			foreach (var emote in emoteList.Emotes)
			{
				var newChoice = new DynamicUIChoiceEntryData();
				newChoice.Text = emote.EmoteName;
				newChoice.Icon = emote.EmoteIcon;
				// Emotes can only be ran server side, so we have to invoke a command on the server.
				newChoice.ChoiceAction = () => PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdDoEmote(emote.EmoteName);
				choices.Add(newChoice);
			}
			DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Emotes", "Choose an emote you'd like to perform.", choices, true);
		}

		private bool IsPressingEmoteWindowInput()
		{
			return KeybindManager.Instance.CaptureKeyCombo() ==
			       KeybindManager.Instance.userKeybinds[KeyAction.EmoteWindowUI].PrimaryCombo;
		}

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
