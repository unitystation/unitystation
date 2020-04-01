using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeechModifierSO", menuName = "Singleton/SpeechModifierSO")]
public class SpeechModifierSO : SingletonScriptableObject<SpeechModifierSO>
{
    [Header("Replacements")]
    public bool activateReplacements = true;
    [ConditionalField(nameof(activateReplacements), true)] public List<WordReplacement> WordReplaceList = new List<WordReplacement>();
    [ConditionalField(nameof(activateReplacements), true)] public List<LetterReplacement> LetterReplaceList = new List<LetterReplacement>();

    [Header("Additions")]
    public bool activateAdditions;
    [ConditionalField(nameof(activateAdditions), true)] public int probability;
    [ConditionalField(nameof(activateAdditions), true)] public List<string> Beginning = new List<string>();
    [ConditionalField(nameof(activateAdditions), true)] public List<string> Ending = new List<string>();
    
    [Header("Special")]
    public bool unintelligible;
    [Tooltip("If assigned, text will be processed by this class instead. Remember to implement a ProcessMessage method with a string message as argument!")]
    public MonoBehaviour customCode = null;

    private Dictionary<string, List<string>> wordlist = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> letterlist = new Dictionary<string, List<string>>();
    private bool init = false;

    void Init()
	{
		foreach (var w in WordReplaceList)
		{
			var original = w.original.ToLower();
			if (!wordlist.ContainsKey(original))
			{
				wordlist.Add(original, w.replaceWith);
			}
		}

		init = true;
	}
    public string ProcessMessage(string message)
	{
		// if (!init) Init();

		// foreach (var kvp in loadedList)
		// {
		// 	Regex r = new Regex(@"\b" + kvp.Key + @"\b", RegexOptions.IgnoreCase);
		// 	message = r.Replace(message, kvp.Value);
		// }

		return message;
	}

    private string ReplaceWord(string message)
    {
        
        
        return message;
    }

    private string ReplaceLetter(string message)
    {
        return message;
    }

    private string AddWords(string message)
    {
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
