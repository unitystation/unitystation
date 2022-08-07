using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NaughtyAttributes;
using UnityEngine;

namespace Player.Language
{
	[CreateAssetMenu(fileName = "NarsianLanguageSO", menuName = "ScriptableObjects/Player/NarsianLanguageSO")]
	public class NarsianLanguageSO : LanguageSO
	{
		[SerializeField]
		private List<string> baseSyllables = new List<string>();
		public List<string> BaseSyllables => baseSyllables;

		[SerializeField]
		private List<string> fullSyllables = new List<string>();
		public List<string> FullSyllables => fullSyllables;

		private void Awake()
		{
			syllables.Clear();

			foreach (var baseSyllable in baseSyllables)
			{
				foreach (var otherSyllable in baseSyllables)
				{
					if (baseSyllable == otherSyllable) continue;

					if (baseSyllable.Length + otherSyllable.Length > 8)
					{
						syllables.Add($"{baseSyllable}{otherSyllable}");
						continue;
					}

					if (DMMath.Prob(80))
					{
						syllables.Add($"{baseSyllable}'{otherSyllable}");
						continue;
					}

					if (DMMath.Prob(25))
					{
						syllables.Add($"{baseSyllable}-{otherSyllable}");
						continue;
					}

					syllables.Add($"{baseSyllable}{otherSyllable}");
				}
			}

			syllables.AddRange(fullSyllables);
		}

#if UNITY_EDITOR

		[SerializeField]
		[TextArea(10, 10)]
		private string syllablesBaseString = "";

		/// <summary>
		/// Converts the baseSyllables into the format for syllables list
		/// Used to convert the DM lists of syllables easier
		/// </summary>
		[Button]
		public void ConvertBaseString()
		{
			baseSyllables.Clear();

			Regex regex = new Regex("\"(.*?)\"");

			var matches = regex.Matches(syllablesBaseString);

			foreach (Match  match in matches)
			{
				baseSyllables.Add(match.Groups[1].Value);
			}
		}

		[SerializeField]
		[TextArea(10, 10)]
		private string syllablesFullString = "";

		/// <summary>
		/// Converts the fullSyllables into the format for syllables list
		/// Used to convert the DM lists of syllables easier
		/// </summary>
		[Button]
		public void ConvertFullString()
		{
			fullSyllables.Clear();

			Regex regex = new Regex("\"(.*?)\"");

			var matches = regex.Matches(syllablesFullString);

			foreach (Match  match in matches)
			{
				fullSyllables.Add(match.Groups[1].Value);
			}
		}

#endif
	}
}