using System.Collections.Generic;
using System.Linq;
using Managers;
using TMPro;
using UnityEngine;

namespace Core.Chat
{
	public class LanguageScreen : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private TMP_Text currentLanguage = null;

		private List<LanguageEntry> entryPool = new List<LanguageEntry>();

		private void OnEnable()
		{
			Refresh();
		}

		private void OnDisable()
		{
			Refresh();
		}

		public void Refresh()
		{
			var player = PlayerManager.LocalPlayerScript;
			if (player == null)
			{
				gameObject.SetActive(false);
				return;
			}

			currentLanguage.text = $"Current Language:\n {player.MobLanguages.CurrentLanguage.OrNull()?.LanguageName}";

			var understoodLanguages = player.MobLanguages.UnderstoodLanguages.OrderByDescending(x => x.Priority).ToArray();
			var spokenLanguages = player.MobLanguages.SpokenLanguages;

			if (player.MobLanguages.OmniTongue)
			{
				understoodLanguages = LanguageManager.Instance.AllLanguages.ToArray();
				spokenLanguages = LanguageManager.Instance.AllLanguages.ToHashSet();
			}

			if (entryPool.Count < understoodLanguages.Length)
			{
				var missing = understoodLanguages.Length - entryPool.Count;
				for (int i = 0; i < missing; i++)
				{
					AddEntry();
				}
			}

			if (entryPool.Count > understoodLanguages.Length)
			{
				var missing = entryPool.Count - understoodLanguages.Length;
				for (int i = 0; i < missing; i++)
				{
					RemoveEntry();
				}
			}

			//You cant speak a language and not understand it so this is fine just going through understood languages
			for (int i = 0; i < understoodLanguages.Length; i++)
			{
				var language = understoodLanguages[i];
				entryPool[i].SetUp($"[{(spokenLanguages.Contains(language) ? "U + S" : "U")}]{language.LanguageName} key: ,{language.Key}", language.Desc,
					language.Sprite, this, language.LanguageUniqueId);
			}
		}

		private void AddEntry()
		{
			entryPrefab.SetActive(true);
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<LanguageEntry>();
			entryPrefab.SetActive(false);
			entryPool.Add(newEntry);
		}

		private void RemoveEntry()
		{
			Destroy(entryPool[^1]);

			entryPool.RemoveAt(entryPool.Count - 1);
		}

		public void OnSelect(ushort languageId)
		{
			var mobLanguages = PlayerManager.LocalPlayerScript.MobLanguages;
			var language = LanguageManager.Instance.GetLanguageById(languageId);

			if (mobLanguages.CanSpeakLanguage(language) == false)
			{
				global::Chat.AddExamineMsgToClient("You cannot speak that language!");
				return;
			}

			mobLanguages.CmdChangeLanguage(languageId);
		}
	}
}