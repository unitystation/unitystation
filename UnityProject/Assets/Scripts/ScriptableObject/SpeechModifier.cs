using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "SpeechModifierSO", menuName = "ScriptableObjects/SpeechModifierSO")]
public class SpeechModifierSO : ScriptableObject
{
    [Header("Replacements")]
    [Tooltip("Activate the replacement behavior for this speech modifier.")]
    public bool activateReplacements = true;
    [Tooltip("Strict replacement. Will only replace word or words isolated by spaces.")]
    [ConditionalField(nameof(activateReplacements), true)] public List<WordReplacement> WordReplaceList = new List<WordReplacement>();
    [Tooltip("Lazy replacement. Will replace anything you put here, doesn't matter if isolated or in the middle of a word")]
    [ConditionalField(nameof(activateReplacements), true)] public List<LetterReplacement> LetterReplaceList = new List<LetterReplacement>();

    [Header("Additions")]
    [Tooltip("Activate the addition of text to ending or begining of message.")]
    public bool activateAdditions;
    [Tooltip("Chances of this happening in %.")]
    [Range(1,100)][ConditionalField(nameof(activateAdditions), true)]public int probability;
    [ConditionalField(nameof(activateAdditions), true)] public List<string> Beginning = new List<string>();
    [ConditionalField(nameof(activateAdditions), true)] public List<string> Ending = new List<string>();
    
    [Header("Special")]
    [Tooltip("Currently unused!")]
    public bool unintelligible;
    [Tooltip("If assigned, text will be processed by this class instead. Remember to implement a ProcessMessage method with a string message as argument!")]
    public CustomSpeechModifier customCode = null;

    private Dictionary<string, List<string>> wordlist = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> letterlist = new Dictionary<string, List<string>>();
    private bool init = false;

    void Init()
	{
        if (activateReplacements)
        {
            foreach (WordReplacement word in WordReplaceList)
            {
                if(!wordlist.ContainsKey(word.original))
                {
                    wordlist.Add(word.original, word.replaceWith);
                }
            }

            foreach (LetterReplacement letter in LetterReplaceList)
            {
                if(!letterlist.ContainsKey(letter.original))
                {
                    letterlist.Add(letter.original, letter.replaceWith);
                }
            }
        }

		init = true;
	}
    
    string Replace(string message)
    {//compile
        if(wordlist.Count != 0)
        {
            foreach (var kvp in wordlist)
            {
                Regex r = new Regex(@"\b" + kvp.Key + @"\b", RegexOptions.IgnoreCase);
                message = r.Replace(message, WasYelling(message) ? kvp.Value.PickRandom().ToUpper(): kvp.Value.PickRandom());
            };
        }

        if(letterlist.Count != 0)
        {
            foreach (var kvp in letterlist)
            {
                Regex r = new Regex(kvp.Key, RegexOptions.IgnoreCase);
                message = r.Replace(message, WasYelling(message) ? kvp.Value.PickRandom().ToUpper(): kvp.Value.PickRandom());
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
		
        if (!init) Init();

        if (activateReplacements) message = Replace(message);

        if (activateAdditions) message = AddText(message);

		return message;
	}
}

[Serializable]
public class WordReplacement
{
    public string original;
    public List<String> replaceWith;
}

[Serializable]
public class LetterReplacement
{
    public string original;
    public List<String> replaceWith;
}

[Serializable]
public abstract class CustomSpeechModifier : ScriptableObject
{
    public abstract string ProcessMessage(string message);
}