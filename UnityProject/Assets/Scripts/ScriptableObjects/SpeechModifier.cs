using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "SpeechModifier", menuName = "ScriptableObjects/SpeechModifiers/SpeechModifier")]

	public class SpeechModifier : ScriptableObject
	{
		[Header("Replacements")]
		[Tooltip("Activate the replacement behavior for this speech modifier.")]
		[SerializeField] private bool activateReplacements = true;
		[Tooltip("Strict replacement. Will only replace word or words isolated by spaces.")]
		[SerializeField] private List<StringListOfStrings> wordReplaceList = null;
		[Tooltip("Lazy replacement. Will replace anything you put here, doesn't matter if isolated or in the middle of a word")]
		[SerializeField] private List<StringListOfStrings> letterReplaceList = null;

		[Header("Additions")]
		[Tooltip("Activate the addition of text to ending or begining of message.")]
		[SerializeField] private bool activateAdditions = false;
		[Tooltip("Chances of this happening in %.")]
		[SerializeField] [Range(0,100)] private int probability = 0;
		[SerializeField] private List<string> beginning = null;
		[SerializeField] private List<string> ending = null;

		[Header("Special")]
		[Tooltip("If assigned, text will be processed by this class instead. Remember to implement a ProcessMessage method with a string message as argument!")]
		public CustomSpeechModifier customCode = null;

		private Dictionary<string, List<string>> wordLookup;

		string Replace(string message)
		{
			// Construct a lookup table on first run
			if (wordLookup == null)
			{
				wordLookup = new Dictionary<string, List<string>>(wordReplaceList.Count);
				foreach (StringListOfStrings strings in wordReplaceList)
				{
					try
					{
						wordLookup.Add(strings.original, strings.replaceWith);
					}
					catch (ArgumentException)
					{
						Debug.LogWarningFormat("Duplicate word {0} in {1}", strings.original, this);
					}
				}
			}

			// Split the message into words, and replace them if needed
			var builder = new StringBuilder(message.Length);
			int start = 0;
			for (int i = 0; i < message.Length; i++)
			{
				char c = message[i];

				if (!char.IsSeparator(c) && !char.IsPunctuation(c)) continue;

				string substring = message.Substring(start, i - start);
				if (wordLookup.TryGetValue(substring, out var replacements))
				{
					string replacement = replacements.PickRandom();
					if (WasYelling(substring))
					{
						replacement = replacement.ToUpper();
					}
					builder.Append(replacement);
				}
				else
				{
					builder.Append(substring);
				}

				builder.Append(c);
				start = i + 1;
			}

			// Add the rest of the text, if present
			if (start < message.Length - 1)
			{
				builder.Append(message.Substring(start));
			}

			message = builder.ToString();

			// Replace letter combinations
			foreach (StringListOfStrings strings in letterReplaceList)
			{
				int index = 0;
				string original = strings.original;
				while (true)
				{
					int match = message.IndexOf(original, index, StringComparison.InvariantCultureIgnoreCase);
					if (match == -1)
					{
						break;
					}

					string replacement = strings.replaceWith.PickRandom();
					if (WasYelling(message.Substring(match, original.Length)))
					{
						replacement = replacement.ToUpper();
					}
					message = message.Remove(match, original.Length).Insert(match, replacement);
					index = match + replacement.Length;
				}
			}

			return message;
		}

		string AddText(string message)
		{
			if (DMMath.Prob(probability))
			{
				if (beginning.Count > 0)
				{
					message = $"{beginning.PickRandom()} {message}";
				}

				if (ending.Count > 0)
				{
					message = $"{message} {ending.PickRandom()}";
				}
			}

			return message;
		}

		bool WasYelling(string message)
		{
			foreach (char c in message)
			{
				if (char.IsLower(c))
				{
					return false;
				}
			}

			return true;
		}

		public string ProcessMessage(string message)
		{
			if (customCode != null)
			{
				return customCode.ProcessMessage(message);
			}

			if (activateReplacements) message = Replace(message);

			if (activateAdditions) message = AddText(message);

			return message;
		}
	}

	[Serializable]
	public class StringListOfStrings
	{
		public string original;
		public List<String> replaceWith;
	}

	[Serializable]
	public abstract class CustomSpeechModifier : ScriptableObject
	{
		public abstract string ProcessMessage(string message);
	}
}