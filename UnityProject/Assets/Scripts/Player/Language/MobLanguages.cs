using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Mirror;
using UnityEngine;

namespace Player.Language
{
	public class MobLanguages : NetworkBehaviour
	{
		[SerializeField]
		private DefaultLanguageGroupSO defaultLanguages = null;

		[SerializeField]
		private bool omniTongue = false;
		public bool OmniTongue => omniTongue;

		public List<LanguageSO> UnderstoodLanguages { get; private set; } = new List<LanguageSO>();

		public List<LanguageSO> SpokenLanguages { get; private set; } = new List<LanguageSO>();

		public List<LanguageSO> BlockedLanguages { get; private set; } = new List<LanguageSO>();

		//Language we are currently speaking
		public LanguageSO CurrentLanguage { get; private set; }

		//Only valid on owner player
		private SyncList<NetworkLanguage> addedLanguages = new SyncList<NetworkLanguage>();

		private void Start()
		{
			if(defaultLanguages == null) return;

			//Copy the default lists to this script lists so we can add to it during runtime without adding to the SO
			SetupFromGroup(defaultLanguages);
		}

		public override void OnStartLocalPlayer()
		{
			addedLanguages.Callback += OnLanguageListChange;

			foreach (var newLanguage in addedLanguages)
			{
				var language = LanguageManager.Instance.GetLanguage(newLanguage.languageId);
				if(language == null) continue;

				LearnLanguage(language, newLanguage.canSpeak);
			}
		}

		public override void OnStopLocalPlayer()
		{
			addedLanguages.Callback -= OnLanguageListChange;
		}

		public void SetupFromGroup(DefaultLanguageGroupSO newGroup)
		{
			//Copy the newGroup lists to this script lists so we can add to it during runtime without adding to the SO
			UnderstoodLanguages = newGroup.UnderstoodLanguages.ToList();
			SpokenLanguages = newGroup.SpokenLanguages.ToList();
			BlockedLanguages = newGroup.BlockedLanguages.ToList();

			ResetCurrentLanguage();
		}

		public bool CanUnderstandLanguage(LanguageSO languageToTest)
		{
			return UnderstoodLanguages.Contains(languageToTest);
		}

		public bool CanSpeakLanguage(LanguageSO languageToTest)
		{
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
				Chat.AddExamineMsg(gameObject, "You are not speaking a language!");
				return;
			}

			if (CanSpeakLanguage(languageToChangeTo) == false)
			{
				Chat.AddExamineMsg(gameObject, $"You do not know how to speak {languageToChangeTo.OrNull()?.LanguageName}!");
				return;
			}

			CurrentLanguage = languageToChangeTo;

			Chat.AddExamineMsg(gameObject, $"You will now speak in {languageToChangeTo.OrNull()?.LanguageName}");
		}

		private void LearnLanguage(LanguageSO languageToLearn, bool canSpeak = false, bool overrideBlocked = false)
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

			if(isServer == false || (addedUnderstand == false && addedSpeak == false)) return;

			addedLanguages.Add(new NetworkLanguage {languageId = languageToLearn.LanguageUniqueId, canUnderstand = addedUnderstand,
				canSpeak = addedSpeak});
		}

		private void RemoveLanguage(LanguageSO languageToRemove, bool noLongerUnderstand = false)
		{
			SpokenLanguages.Remove(languageToRemove);

			if (noLongerUnderstand == false)
			{
				if (isServer == false) return;

				for (int i = addedLanguages.Count - 1; i >= 0; i--)
				{
					var language = addedLanguages[i];
					if(language.languageId != languageToRemove.LanguageUniqueId) continue;

					addedLanguages[i] = new NetworkLanguage { languageId = language.languageId, canUnderstand = true};
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
			}
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
					var newLanguage = LanguageManager.Instance.GetLanguage(newItem.languageId);
					if(newLanguage == null) break;

					LearnLanguage(newLanguage, newItem.canSpeak);
					break;
				case SyncList<NetworkLanguage>.Operation.OP_REMOVEAT:
					// index is where it was removed from the list
					// oldItem is the item that was removed
					var oldLanguage = LanguageManager.Instance.GetLanguage(oldItem.languageId);
					if(oldLanguage == null) break;

					RemoveLanguage(oldLanguage, true);
					break;
				case SyncList<NetworkLanguage>.Operation.OP_SET:
					// index is of the item that was changed
					// oldItem is the previous value for the item at the index
					// newItem is the new value for the item at the index
					oldLanguage = LanguageManager.Instance.GetLanguage(oldItem.languageId);
					RemoveLanguage(oldLanguage, true);

					newLanguage = LanguageManager.Instance.GetLanguage(newItem.languageId);
					LearnLanguage(newLanguage, newItem.canSpeak);
					break;
				case SyncList<NetworkLanguage>.Operation.OP_CLEAR:
					RemoveAddedLanguages();
					break;
			}
		}

		private void RemoveAddedLanguages()
		{
			foreach (var newLanguage in addedLanguages)
			{
				var language = LanguageManager.Instance.GetLanguage(newLanguage.languageId);
				if(language == null) continue;

				RemoveLanguage(language, true);
			}
		}

		#endregion

		private struct NetworkLanguage
		{
			public ushort languageId;
			public bool canUnderstand;
			public bool canSpeak;
		}
	}
}