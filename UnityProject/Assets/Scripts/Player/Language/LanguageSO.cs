using System.Collections.Generic;
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
		private List<string> syllables = new List<string>();
		public List<string> Syllables => syllables;

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
	}
}