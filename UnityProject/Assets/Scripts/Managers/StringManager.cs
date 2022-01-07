using System.Collections;
using System.Collections.Generic;
using Initialisation;
using Managers;
using UnityEngine;

public class StringManager : SingletonManager<StringManager>, IInitialise
{
	/// <summary>
	/// The PlayerPref key for ChatBubble preference.
	/// Use PlayerPrefs.GetInt(chatBubblePref) to determine the players
	/// preference for showing the chat bubble or not.
	/// 0 = false
	/// 1 = true
	/// </summary>
	public static string ChatBubblePref = "ChatBubble";

	public Dictionary<string, List<string>> textObjects = new Dictionary<string, List<string>>();

	public List<TextAsset> nameTextFiles;

	public InitialisationSystems Subsystem => InitialisationSystems.StringManager;

	void IInitialise.Initialise()
	{
		for (int i = 0; i < nameTextFiles.Count; i++)
		{
			string[] lines = nameTextFiles[i].text.Split(
				new [] { "\r\n", "\r", "\n" },
				System.StringSplitOptions.None);
			textObjects.Add(nameTextFiles[i].name, new List<string>(lines));
		}
	}

	public static string GetRandomLizardName(Gender gender)
	{
		//Uses random gendered name if NonBinary
		if (gender == Gender.NonBinary) gender = Random.value > 0.5f ? Gender.Male : Gender.Female;

		//ToLowerInvariant because ToLower has different behaviour based on culture
		var genderKey = gender.ToString().ToLowerInvariant();

		//Random.Range is max exclusive and as such .Count can be used directly
		var randomLizard =
			Instance.textObjects[$"lizard_{genderKey}"][Random.Range(0, Instance.textObjects[$"lizard_{genderKey}"].Count)];

		return randomLizard;
	}

	public static string GetRandomMaleName()
	{
		return GetRandomName(Gender.Male);
	}

	public static string GetRandomFemaleName()
	{
		return GetRandomName(Gender.Female);
	}

	/// <summary>
	/// Combines a random first and last name depending on gender, uses both male and female names if gender is NonBinary
	/// </summary>
	public static string GetRandomName(Gender gender)
	{
		if (gender == Gender.NonBinary) gender = Random.value > 0.5f ? Gender.Male : Gender.Female; //Uses random gendered name if NonBinary
		var genderKey = gender.ToString().ToLowerInvariant(); //ToLowerInvariant because ToLower has different behaviour based on culture
		var firstName = Instance.textObjects[$"first_{genderKey}"][Random.Range(0, Instance.textObjects[$"first_{genderKey}"].Count)]; //Random.Range is max exclusive and as such .Count can be used directly
		var lastName = Instance.textObjects["last"][Random.Range(0, Instance.textObjects["last"].Count)];
		return $"{firstName} {lastName}";
	}
}