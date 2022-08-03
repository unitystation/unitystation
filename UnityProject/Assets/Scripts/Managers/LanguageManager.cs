using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Player.Language;
using UnityEngine;

namespace Managers
{
	public class LanguageManager : SingletonManager<LanguageManager>
	{
		[SerializeField]
		private List<LanguageSO> allLanguages = new List<LanguageSO>();
		public List<LanguageSO> AllLanguages => allLanguages;

		private Dictionary<LanguageSO, Dictionary<string, string>> stringCache =
			new Dictionary<LanguageSO, Dictionary<string, string>>(10);

		private const int CacheCapacity = 50;

		private readonly List<char> endings = new List<char>{'!', '?', '.'};

		public static string Scramble(LanguageSO languageToTranslate, PlayerScript targetPlayer, string message)
		{
			if (languageToTranslate == null) return message;

			if (targetPlayer.MobLanguages.CanUnderstandLanguage(languageToTranslate))
			{
				return message;
			}

			return Instance.InternalScramble(languageToTranslate, message);
		}

		private string InternalScramble(LanguageSO language, string message)
		{
			//See if cache already has string
			if (stringCache.TryGetValue(language, out var cache))
			{
				if (cache.TryGetValue(message, out var cachedMessage))
				{
					return cachedMessage;
				}
			}
			else
			{
				//Add new language for the first time
				stringCache.Add(language, new Dictionary<string, string>(CacheCapacity));
				cache = stringCache[language];
			}

			var convertedMessage = new StringBuilder();
			var messageSize = message.Length;
			var capitaliseNext = false;

			while (convertedMessage.Length < messageSize)
			{
				var nextSyllables = language.RandomSyllable();

				if (capitaliseNext)
				{
					capitaliseNext = false;
					nextSyllables = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nextSyllables);
				}

				convertedMessage.Append(nextSyllables);

				var random = Random.Range(0f, 100f);

				if (random <= language.SentenceChance)
				{
					convertedMessage.Append(". ");
					capitaliseNext = true;
					continue;
				}

				if (random <= language.SpaceChance)
				{
					convertedMessage.Append(" ");
				}
			}

			//If dot is at end remove it
			if (convertedMessage[^1] == '.')
			{
				convertedMessage.Remove(convertedMessage.Length - 1, 1);
			}

			var lastInput = message[message.Length];

			//Add the ending of the input message if needed
			foreach (var ending in endings)
			{
				if(lastInput != ending) continue;
				convertedMessage.Append(ending);
				break;
			}

			//If over or at capacity remove first element
			if (cache.Count >= CacheCapacity)
			{
				//Might be better way to do this or use different collection?
				cache.Remove(cache.ElementAt(0).Key);
			}

			var finishedMessage = convertedMessage.ToString();

			//Add to cache
			cache.Add(message, finishedMessage);

			return finishedMessage;
		}

		public LanguageSO GetLanguageById(ushort languageId)
		{
			if (languageId == 0)
			{
				return null;
			}

			foreach (var language in allLanguages)
			{
				if(languageId != language.LanguageUniqueId) continue;

				return language;
			}

			return null;
		}

		public LanguageSO GetLanguageByKey(char key)
		{
			if (key == default)
			{
				return null;
			}

			foreach (var language in allLanguages)
			{
				if(key != language.Key) continue;

				return language;
			}

			return null;
		}

		/// <summary>
		/// Gets the language by its name (not the language file name!)
		/// </summary>
		public LanguageSO GetLanguageByName(string languageName)
		{
			if (string.IsNullOrEmpty(languageName))
			{
				return null;
			}

			foreach (var language in allLanguages)
			{
				if(languageName != language.LanguageName) continue;

				return language;
			}

			return null;
		}

		public Sprite GetLanguageSprite(ushort languageId)
		{
			return GetLanguageById(languageId).OrNull()?.Sprite;
		}

		public void AddToList(LanguageSO languageSo)
		{
			if(allLanguages.Contains(languageSo)) return;

			allLanguages.Add(languageSo);
		}
	}
}