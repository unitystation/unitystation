using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.Language
{
	public class MobLanguages : MonoBehaviour
	{
		[SerializeField]
		private DefaultLanguageHolderSO defaultLanguages = null;

		[SerializeField]
		private bool omniTongue = false;

		private List<LanguageSO> understoodLanguages = new List<LanguageSO>();
		public List<LanguageSO> UnderstoodLanguages => understoodLanguages;

		private List<LanguageSO> spokenLanguages = new List<LanguageSO>();
		public List<LanguageSO> SpokenLanguages => spokenLanguages;

		private List<LanguageSO> blockedLanguages  = new List<LanguageSO>();
		public List<LanguageSO> BlockedLanguages => blockedLanguages;

		private void Start()
		{
			if(defaultLanguages == null) return;

			//Copy the default lists to this script lists so we can add to it during runtime without adding to the SO
			understoodLanguages = defaultLanguages.UnderstoodLanguages.ToList();
			spokenLanguages = defaultLanguages.SpokenLanguages.ToList();
			blockedLanguages = defaultLanguages.BlockedLanguages.ToList();
		}

		public bool CanUnderstandLanguage(LanguageSO languageToTest)
		{
			return understoodLanguages.Contains(languageToTest);
		}

		public bool CanSpeakLanguage(LanguageSO languageToTest)
		{
			return spokenLanguages.Contains(languageToTest);
		}

		public bool IsBlockedLanguage(LanguageSO languageToTest)
		{
			return blockedLanguages.Contains(languageToTest);
		}

		public void LearnLanguage(LanguageSO languageToLearn, bool canSpeak = false, bool overrideBlocked = false)
		{
			if (overrideBlocked == false && IsBlockedLanguage(languageToLearn))
			{
				Chat.AddExamineMsgFromServer(gameObject, $"You cannot learn to understand {languageToLearn.LanguageName} it is too complex!");
				return;
			}

			if (CanUnderstandLanguage(languageToLearn) == false)
			{
				understoodLanguages.Add(languageToLearn);
				Chat.AddExamineMsgFromServer(gameObject, $"You learn to understand {languageToLearn.LanguageName}");
			}

			if (canSpeak && CanSpeakLanguage(languageToLearn) == false)
			{
				spokenLanguages.Add(languageToLearn);
				Chat.AddExamineMsgFromServer(gameObject, $"You learn to speak {languageToLearn.LanguageName}");
			}
		}
	}
}