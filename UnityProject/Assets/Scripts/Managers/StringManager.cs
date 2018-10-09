using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class StringManager : MonoBehaviour
{
	private static StringManager stringManager;
	public static StringManager Instance
	{
		get
		{
			if (stringManager == null)
			{
				stringManager = FindObjectOfType<StringManager>();
			}
			return stringManager;
		}
	}

	public Dictionary<string, List<string>> textObjects = new Dictionary<string, List<string>>();

	public List<TextAsset> nameTextFiles;

	void Start()
	{
		for (int i = 0; i < nameTextFiles.Count; i++)
		{
			string[] lines = Regex.Split(nameTextFiles[i].text, "\n|\r|\r\n");
			textObjects.Add(nameTextFiles[i].name, new List<string>(lines));
		}
	}

	public static string GetRandomMaleName(){
		var newName = Instance.textObjects["first_male"]
		[Random.Range(0,Instance.textObjects["first_male"].Count - 1)];
		newName += " " + Instance.textObjects["last"]
		[Random.Range(0,Instance.textObjects["last"].Count - 1)];
		return newName;
	}

	public static string GetRandomFemaleName(){
		var newName = Instance.textObjects["first_female"]
		[Random.Range(0,Instance.textObjects["first_female"].Count - 1)];
		newName += " " + Instance.textObjects["last"]
		[Random.Range(0,Instance.textObjects["last"].Count - 1)];
		return newName;
	}

}