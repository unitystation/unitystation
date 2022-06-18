using System;
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
		string[] lineEndings = { "\r\n", "\r", "\n" };
		foreach (var nameFile in nameTextFiles)
		{
			var lines = nameFile.text.Split(lineEndings, StringSplitOptions.None);
			textObjects.Add(nameFile.name, new List<string>(lines));
		}
	}

	public static string GetRandomLizardName(Gender gender = Gender.NonBinary)
	{
		//Uses random gendered name if NonBinary
		if (gender == Gender.NonBinary)
		{
			gender = DMMath.Prob(50) ? Gender.Male : Gender.Female;
		}

		//ToLowerInvariant because ToLower has different behaviour based on culture
		var genderKey = gender.ToString().ToLowerInvariant();

		return Instance.textObjects[$"lizard_{genderKey}"].PickRandom();
	}

	/// <summary>
	/// Combines a random first and last name depending on gender.
	/// Uses both male and female names if gender is NonBinary.
	/// </summary>
	public static string GetRandomName(Gender gender = Gender.NonBinary)
	{
		//Uses random gendered name if NonBinary
		if (gender == Gender.NonBinary)
		{
			gender = DMMath.Prob(50) ? Gender.Male : Gender.Female;
		}

		//ToLowerInvariant because ToLower has different behaviour based on culture
		var genderKey = gender.ToString().ToLowerInvariant();

		var firstName = Instance.textObjects[$"first_{genderKey}"].PickRandom();
		var lastName = Instance.textObjects["last"].PickRandom();

		return $"{firstName} {lastName}";
	}
}
