using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

		string Replace (string message)
		{
			if (wordReplaceList.Count > 0)
			{
				foreach (var word in wordReplaceList)
				{
					message = Regex.Replace(
						message,
						@"\b(" + word.original + @")\b",
						m => WasYelling(m.Groups[1].Value) ? word.replaceWith.PickRandom().ToUpper() : word.replaceWith.PickRandom(),
						RegexOptions.IgnoreCase);
				}
			}

			if (letterReplaceList.Count > 0)
			{
				foreach (var word in letterReplaceList)
				{
					message = Regex.Replace(
						message,
						"(" + word.original + ")",
						m => WasYelling(m.Groups[1].Value) ? word.replaceWith.PickRandom().ToUpper() : word.replaceWith.PickRandom(),
						RegexOptions.IgnoreCase);
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

		bool WasYelling(string message) => message == message.ToUpper();

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