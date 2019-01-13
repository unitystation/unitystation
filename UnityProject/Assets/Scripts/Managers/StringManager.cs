using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
			string[] lines = nameTextFiles[i].text.Split(
				new[] { "\r\n", "\r", "\n" },
				System.StringSplitOptions.None);
			textObjects.Add(nameTextFiles[i].name, new List<string>(lines));
		}
	}

	public static string GetRandomMaleName(){
		return GetRandomName(Gender.Male);
	}

	public static string GetRandomFemaleName(){
		return GetRandomName(Gender.Female);
	}

	/// <summary>
	/// Combines a random first and last name depending on gender, uses both male and female names if gender is Nueter
	/// </summary>
	public static string GetRandomName(Gender gender)
	{
		if (gender == Gender.Neuter) gender = Random.value > 0.5f ? Gender.Male : Gender.Female; //Uses random gendered name if Nueter
		var genderKey = gender.ToString().ToLowerInvariant(); //ToLowerInvariant because ToLower has different behaviour based on culture
		var firstName = Instance.textObjects[$"first_{genderKey}"][Random.Range(0, Instance.textObjects[$"first_{genderKey}"].Count)]; //Random.Range is max exclusive and as such .Count can be used directly
		var lastName  = Instance.textObjects["last"              ][Random.Range(0, Instance.textObjects["last"              ].Count)];
		return $"{firstName} {lastName}";
	}
}