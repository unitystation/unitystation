using System;
using System.Collections.Generic;
using Initialisation;
using Shared.Managers;
using UnityEngine;
using Random = System.Random;

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

	public static string GetRandomGenericBorgSerialNumberName()
	{
		Random random = new Random();
		int a = random.Next(0, 26);
		int b = random.Next(0, 26);
		int num = random.Next(100, 999);
		char ch1 = (char)('a' + a);
		char ch2 = (char)('a' + b);
		return $"{ch1}{ch2}-{num}".ToUpper();
	}

	/// <summary>
	/// Combines a random first and last name depending on gender.
	/// Uses both male and female names if gender is NonBinary. Species aware. Will return humanoid names if no species is specified.
	/// </summary>
	public static string GetRandomName(Gender gender = Gender.NonBinary, string species = "Human")
	{
		//TODO: Make this more generic so we don't hard-code these things all the time.
		if (species == "Lizard" || species == "Ashwalker")
		{
			return GetRandomLizardName(gender);
		}

		if (species == "Robot" || species == "Cyborg" || species == "Borg")
		{
			return GetRandomGenericBorgSerialNumberName();
		}

		return GetRandomHumanoidName(gender);
	}

	public static string GetRandomHumanoidName(Gender gender = Gender.NonBinary)
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
