using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NaughtyAttributes;
using UnityEngine;

namespace Player.Language
{
	[CreateAssetMenu(fileName = "LanguageSO", menuName = "ScriptableObjects/Player/LanguageSO")]
	public class LanguageSO : ScriptableObject
	{
		[SerializeField]
		private string languageName = "";
		public string LanguageName => languageName;

		[SerializeField]
		[TextArea(10, 10)]
		private string desc = "";
		public string Desc => desc;

		[SerializeField]
		[Tooltip("The keycode for language use in the chat system")]
		private string key = "";
		public string Key => key;

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
		[Tooltip("Flags for this language")]
		private LanguageFlags flags = LanguageFlags.None;
		public LanguageFlags Flags => flags;

		[SerializeField]
		protected List<string> syllables = new List<string>();
		public List<string> Syllables => syllables;

#if UNITY_EDITOR

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