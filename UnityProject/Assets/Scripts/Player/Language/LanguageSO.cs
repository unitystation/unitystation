using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Managers;
using NaughtyAttributes;
using TMPro;
using UnityEditor;
using UnityEngine;
using Util;

namespace Player.Language
{
	[CreateAssetMenu(fileName = "LanguageSO", menuName = "ScriptableObjects/Player/LanguageSO")]
	public class LanguageSO : ScriptableObject
	{
		[SerializeField]
		private string languageName = "";
		public string LanguageName => languageName;

		[SerializeField]
		private ushort languageUniqueId = 0;
		public ushort LanguageUniqueId => languageUniqueId;

		[SerializeField]
		[TextArea(10, 10)]
		private string desc = "";
		public string Desc => desc;

		[SerializeField]
		[Tooltip("The keycode for language use in the chat system")]
		private char key;
		public char Key => key;

		[SerializeField]
		[Tooltip("Chance of making a new sentence after each syllable")]
		private float sentenceChance = 5;
		public float SentenceChance => sentenceChance;

		[SerializeField]
		[Tooltip("Chance of getting a space in the random scramble string")]
		private float spaceChance = 55;
		public float SpaceChance => spaceChance;

		[SerializeField]
		[Tooltip("Priority that this language is the default, higher is preferred")]
		private int priority = 0;
		public int Priority => priority;

		[SerializeField]
		[Tooltip("Sprite icon for this language")]
		private Sprite sprite = null;
		public Sprite Sprite => sprite;

		[SerializeField]
		[Tooltip("Sprite icon for chat for this language")]
		private TMP_SpriteAsset chatSprite = null;
		public TMP_SpriteAsset ChatSprite => chatSprite;

		[SerializeField]
		[Tooltip("Flags for this language")]
		private LanguageFlags flags = LanguageFlags.None;
		public LanguageFlags Flags => flags;

		[SerializeField]
		protected List<string> syllables = new List<string>();
		public List<string> Syllables => syllables;

		public string RandomSyllable()
		{
			return syllables.PickRandom();
		}

#if UNITY_EDITOR

		[Button]
		public void GiveUniqueId()
		{
			ushort id = 1;

			var manager = OtherUtil.GetManager<LanguageManager>("LanguageManager");

			while (manager.AllLanguages.Any(x => x.LanguageUniqueId == id && x != this))
			{
				id++;
			}

			languageUniqueId = id;

			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		[Button]
		public void AddToLanguageManagerList()
		{
			var manager = OtherUtil.GetManager<LanguageManager>("LanguageManager");

			manager.AddToList(this);

			EditorUtility.SetDirty(manager);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		[SerializeField]
		[TextArea(10, 10)]
		private string syllablesString = "";

		/// <summary>
		/// Converts the syllablesString into the format for syllables list
		/// Used to convert the DM lists of syllables easier
		/// </summary>
		[Button]
		public void ConvertString()
		{
			syllables.Clear();

			Regex regex = new Regex("\"(.*?)\"");

			var matches = regex.Matches(syllablesString);

			foreach (Match  match in matches)
			{
				syllables.Add(match.Groups[1].Value);
			}
		}

#endif
	}

	[Flags]
	public enum LanguageFlags
	{
		None = 0,

		//Hide the icon in the chat if understood
		HideIconIfUnderstood = 1 << 0,

		//Hide the icon in the chat if not understood
		HideIconIfNotUnderstood = 1 << 1,

		//Can speak without tongue
		TonguelessSpeech = 1 << 2,
	}
}