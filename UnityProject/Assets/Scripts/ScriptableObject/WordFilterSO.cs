using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "WordFilterSO", menuName = "Singleton/WordFilterSO")]
public class WordFilterSO : SingletonScriptableObject<WordFilterSO>
{
	public List<WordFilterEntry> FilterList = new List<WordFilterEntry>();
	private Dictionary<string, string> loadedList = new Dictionary<string, string>();
	private bool init = false;

	public string ProcessMessage(string message)
	{
		if (!init) Init();

		foreach (var kvp in loadedList)
		{
			Regex r = new Regex(@"\b" + kvp.Key + @"\b", RegexOptions.IgnoreCase);
			message = r.Replace(message, kvp.Value);
		}

		return message;
	}

	void Init()
	{
		foreach (var w in FilterList)
		{
			var targetWord = w.TargetWord.ToLower();
			if (!loadedList.ContainsKey(targetWord))
			{
				loadedList.Add(targetWord, w.ReplaceWithWord);
			}
		}

		init = true;
	}
}

[Serializable]
public class WordFilterEntry
{
	public string TargetWord;
	public string ReplaceWithWord;
}
