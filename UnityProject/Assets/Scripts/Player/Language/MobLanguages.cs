using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Managers;
using Mirror;
using UI.Chat_UI;
using UnityEngine;

namespace Player.Language
{
	public class MobLanguages : NetworkBehaviour
	{
		[SerializeField]
		private DefaultLanguageGroupSO defaultLanguages = null;
		public DefaultLanguageGroupSO DefaultLanguages => defaultLanguages;

		[SerializeField]
		private bool omniTongue = false;
		public bool OmniTongue => omniTongue;

		public HashSet<LanguageSO> UnderstoodLanguages { get; private set; } = new HashSet<LanguageSO>();

		public HashSet<LanguageSO> SpokenLanguages { get; private set; } = new HashSet<LanguageSO>();

		public HashSet<LanguageSO> BlockedLanguages { get; private set; } = new HashSet<LanguageSO>();

		[SyncVar(hook = nameof(SyncCurrentLanguage))]
		private ushort currentLanguageId = 0;

		//Language we are currently speaking (valid server and client)
		public LanguageSO CurrentLanguage { get; private set; }

		//Only valid on owner player
		private SyncList<NetworkLanguage> addedLanguages = new SyncList<NetworkLanguage>();

		public PlayerScript PlayerScript;

		private void Start()
		{
			PlayerScript = this.GetComponent<PlayerScript>();
			PlayerScript.OnActionControlPlayer += OnPlayerEnterBody;
			if(defaultLanguages == null) return;

			//Copy the default lists to this script lists so we can add to it during runtime without adding to the SO
			SetupFromGroup(defaultLanguages);
		}

		public void OnPlayerEnterBody()
		{
			if(CustomNetworkManager.IsServer) return;

			addedLanguages.Callback += OnLanguageListChange;

			TryAdd();
		}

		[ContextMenu("Try add languages")]
		private void TryAdd()
		{
			foreach (var newLanguage in addedLanguages)
			{
				var language = LanguageManager.Instance.GetLanguageById(newLanguage.languageId);
				if(language == null) continue;

				LearnLanguageClient(language, newLanguage.canSpeak);
			}
		}

		public override void OnStopLocalPlayer()
		{
			addedLanguages.Callback -= OnLanguageListChange;
		}

		private void SetupFromGroup(DefaultLanguageGroupSO newGroup)
		{
			//Copy the newGroup lists to this script lists so we can add to it during runtime without adding to the SO
			UnderstoodLanguages = newGroup.UnderstoodLanguages.ToHashSet();
			SpokenLanguages = newGroup.SpokenLanguages.ToHashSet();
			BlockedLanguages = newGroup.BlockedLanguages.ToHashSet();

			if(CustomNetworkManager.IsServer == false) return;

			ResetCurrentLanguage();
		}

		public bool CanUnderstandLanguage(LanguageSO languageToTest)
		{
			if (omniTongue) return true;

			return UnderstoodLanguages.Contains(languageToTest);
		}

		public bool CanSpeakLanguage(LanguageSO languageToTest)
		{
			if (omniTongue) return true;

			return SpokenLanguages.Contains(languageToTest);
		}

		public bool IsBlockedLanguage(LanguageSO languageToTest)
		{
			return BlockedLanguages.Contains(languageToTest);
		}

		private void SetCurrentLanguage(LanguageSO languageToChangeTo)
		{
			if (languageToChangeTo == null)
			{
				CurrentLanguage = languageToChangeTo;
				currentLanguageId = 0;
				Chat.AddExamineMsg(gameObject, "You are not speaking a language!");
				return;
			}

			if (CanSpeakLanguage(languageToChangeTo) == false)
			{
				Chat.AddExamineMsg(gameObject, $"You do not know how to speak {languageToChangeTo.OrNull()?.LanguageName}!");
				return;
			}

			CurrentLanguage = languageToChangeTo;
			currentLanguageId = languageToChangeTo.LanguageUniqueId;

			Chat.AddExamineMsg(gameObject, $"You will now speak in {languageToChangeTo.OrNull()?.LanguageName}");
		}

		[Server]
		public void LearnLanguage(LanguageSO languageToLearn, bool canSpeak = false, bool overrideBlocked = false)
		{
			if (overrideBlocked == false && IsBlockedLanguage(languageToLearn))
			{
				Chat.AddExamineMsg(gameObject, $"You cannot learn to understand {languageToLearn.LanguageName} it is too complex!");
				return;
			}

			var addedUnderstand = false;
			var addedSpeak = false;

			if (CanUnderstandLanguage(languageToLearn) == false)
			{
				UnderstoodLanguages.Add(languageToLearn);
				Chat.AddExamineMsg(gameObject, $"You learn to understand {languageToLearn.LanguageName}");
				addedUnderstand = true;
			}

			if (canSpeak && CanSpeakLanguage(languageToLearn) == false)
			{
				SpokenLanguages.Add(languageToLearn);
				Chat.AddExamineMsg(gameObject, $"You learn to speak {languageToLearn.LanguageName}");
				addedSpeak = true;
			}

			if(addedUnderstand == false && addedSpeak == false) return;

			addedLanguages.Add(new NetworkLanguage {languageId = languageToLearn.LanguageUniqueId, canUnderstand = addedUnderstand,
				canSpeak = addedSpeak});
			netIdentity.isDirty = true;

			ResetCurrentLanguage();
		}

		[Client]
		private void LearnLanguageClient(LanguageSO languageToLearn, bool canSpeak = false)
		{
			UnderstoodLanguages.Add(languageToLearn);

			if (canSpeak)
			{
				SpokenLanguages.Add(languageToLearn);
			}
		}

		[Server]
		public void RemoveLanguage(LanguageSO languageToRemove, bool noLongerUnderstand = false)
		{
			SpokenLanguages.Remove(languageToRemove);

			ResetCurrentLanguage();

			if (noLongerUnderstand == false)
			{
				if (isServer == false) return;

				for (int i = addedLanguages.Count - 1; i >= 0; i--)
				{
					var language = addedLanguages[i];
					if(language.languageId != languageToRemove.LanguageUniqueId) continue;

					addedLanguages[i] = new NetworkLanguage { languageId = language.languageId, canUnderstand = true};
					netIdentity.isDirty = true;
					return;
				}

				return;
			}

			UnderstoodLanguages.Remove(languageToRemove);

			if (isServer == false) return;

			for (int i = addedLanguages.Count - 1; i >= 0; i--)
			{
				var language = addedLanguages[i];
				if(language.languageId != languageToRemove.LanguageUniqueId) continue;

				addedLanguages.RemoveAt(i);
				netIdentity.isDirty = true;
			}
		}

		[Client]
		private void RemoveLanguageClient(LanguageSO languageToRemove, bool noLongerUnderstand = false)
		{
			SpokenLanguages.Remove(languageToRemove);

			if (noLongerUnderstand == false) return;

			UnderstoodLanguages.Remove(languageToRemove);
		}

		private void ResetCurrentLanguage()
		{
			if (SpokenLanguages.Count == 0)
			{
				SetCurrentLanguage(null);
				Chat.AddExamineMsg(gameObject, "You can speak no languages!");
				return;
			}

			//Get highest priority language
			SetCurrentLanguage(SpokenLanguages.OrderByDescending(x => x.Priority).First());
		}

		#region Networking

		void OnLanguageListChange(SyncList<NetworkLanguage>.Operation op, int index, NetworkLanguage oldItem,
			NetworkLanguage newItem)
		{
			switch (op)
			{
				case SyncList<NetworkLanguage>.Operation.OP_ADD:
				case SyncList<NetworkLanguage>.Operation.OP_INSERT:
					var newLanguage = LanguageManager.Instance.GetLanguageById(newItem.languageId);
					if(newLanguage == null) break;

					LearnLanguageClient(newLanguage, newItem.canSpeak);
					break;
				case SyncList<NetworkLanguage>.Operation.OP_REMOVEAT:
					// index is where it was removed from the list
					// oldItem is the item that was removed
					var oldLanguage = LanguageManager.Instance.GetLanguageById(oldItem.languageId);
					if(oldLanguage == null) break;

					RemoveLanguageClient(oldLanguage, true);
					break;
				case SyncList<NetworkLanguage>.Operation.OP_SET:
					// index is of the item that was changed
					// oldItem is the previous value for the item at the index
					// newItem is the new value for the item at the index
					oldLanguage = LanguageManager.Instance.GetLanguageById(oldItem.languageId);
					RemoveLanguageClient(oldLanguage, true);

					newLanguage = LanguageManager.Instance.GetLanguageById(newItem.languageId);
					LearnLanguageClient(newLanguage, newItem.canSpeak);
					break;
				case SyncList<NetworkLanguage>.Operation.OP_CLEAR:
					RemoveAddedLanguagesClient();
					break;
				default:
					Loggy.LogError($"Failed to find case: {op}");
					return;
			}
		}

		[Client]
		private void RemoveAddedLanguagesClient()
		{
			foreach (var newLanguage in addedLanguages)
			{
				var language = LanguageManager.Instance.GetLanguageById(newLanguage.languageId);
				if(language == null) continue;

				RemoveLanguageClient(language, true);
			}
		}

		private void SyncCurrentLanguage(ushort oldLanguage, ushort newLanguage)
		{
			if(hasAuthority == false) return;

			currentLanguageId = newLanguage;

			CurrentLanguage = LanguageManager.Instance.GetLanguageById(newLanguage);

			ChatUI.Instance.LanguagePanel.Refresh();
		}

		[Command]
		public void CmdChangeLanguage(ushort languageId)
		{
			var language = LanguageManager.Instance.GetLanguageById(languageId);
			if (language == null || CanSpeakLanguage(language) == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You do not know that language!");
				return;
			}

			SetCurrentLanguage(language);
		}

		#endregion

		public struct NetworkLanguage
		{
			public ushort languageId;
			public bool canUnderstand;
			public bool canSpeak;
		}
	}
}
