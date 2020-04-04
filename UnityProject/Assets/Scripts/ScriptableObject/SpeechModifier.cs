using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "SpeechModifier", menuName = "ScriptableObjects/SpeechModifiers/SpeechModifier")]

public class SpeechModifier : ScriptableObject
{
	[Header("Replacements")]
	[Tooltip("Activate the replacement behavior for this speech modifier.")]
	public bool activateReplacements = true;
	[Tooltip("Strict replacement. Will only replace word or words isolated by spaces.")]
 	public List<StringListOfStrings> WordReplaceList = new List<StringListOfStrings>();
	[Tooltip("Lazy replacement. Will replace anything you put here, doesn't matter if isolated or in the middle of a word")]
	public List<StringListOfStrings> LetterReplaceList = new List<StringListOfStrings>();

	[Header("Additions")]
	[Tooltip("Activate the addition of text to ending or begining of message.")]
	public bool activateAdditions;
	[Tooltip("Chances of this happening in %.")]
	[Range(0,100)]public int probability;
	public List<string> Beginning = new List<string>();
	public List<string> Ending = new List<string>();
	
	[Header("Special")]
	[Tooltip("If assigned, text will be processed by this class instead. Remember to implement a ProcessMessage method with a string message as argument!")]
	public CustomSpeechModifier customCode = null;

	string Replace (string message)
	{
		if (WordReplaceList.Count != 0)
		{
			foreach (var word in WordReplaceList)
			{
				message = Regex.Replace(
					message, 
					@"\b(" + word.original + @")\b", 
					m => WasYelling(m.Groups[1].Value) ? word.replaceWith.PickRandom().ToUpper() : word.replaceWith.PickRandom(),
					RegexOptions.IgnoreCase);
			}
		}

		if (LetterReplaceList.Count != 0)
		{
			foreach (var word in LetterReplaceList)
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
		   if (Beginning.Count != 0)
		   {
			   message = $"{Beginning.PickRandom()} {message}";
		   }

		   if (Ending.Count != 0)
		   {
			   message = $"{message} {Ending.PickRandom()}";
		   }
	   }
		
		return message;
	}

	bool WasYelling(string message)
	{
		if(message == message.ToUpper()) return true;

		return false;
	}

	public string ProcessMessage(string message)
	{
		if (customCode != null) return customCode.ProcessMessage(message);

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