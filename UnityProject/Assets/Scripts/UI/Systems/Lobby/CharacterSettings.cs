using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lobby;
using Newtonsoft.Json;
using UI.CharacterCreator;

/// <summary>
/// Class containing all character preferences for a player
/// Includes appearance, job preferences etc...
/// </summary>
public class CharacterSettings
{
	// TODO: all of the in-game appearance variables should probably be refactored into a separate class which can
	// then be used in PlayerScript since job preferences are only needed at round start in ConnectedPlayer

	// IMPORTANT: these fields use primitive types (int, string... etc) so they can be sent  over the network with
	// RPCs and Commands without needing to serialise them to JSON!
	public const int MAX_NAME_LENGTH = 26; //Arbitrary limit, but 26 is the max the current UI can fit
	public string Username;
	public string Name = "Cuban Pete";
	public BodyType BodyType = BodyType.Male;
	public ClothingStyle ClothingStyle = ClothingStyle.JumpSuit;
	public BagStyle BagStyle = BagStyle.Backpack;
	public int Age = 22;
	public Speech Speech = Speech.None;
	public string SkinTone = "#ffe0d1";
	public List<CustomisationStorage> SerialisedBodyPartCustom;
	public List<ExternalCustomisation> SerialisedExternalCustom;


	public string Species = "Human";
	public JobPrefsDict JobPreferences = new JobPrefsDict();
	public AntagPrefsDict AntagPreferences = new AntagPrefsDict();


	[System.Serializable]
	public class CustomisationClass
	{
		public string SelectedName = "None";
		public string Colour = "#ffffff";
	}




	public override string ToString()
	{
		var sb = new StringBuilder($"{Username}'s character settings:\n", 300);
		sb.AppendLine($"Name: {Name}");
		sb.AppendLine($"ClothingStyle: {ClothingStyle}");
		sb.AppendLine($"BagStyle: {BagStyle}");
		sb.AppendLine($"Age: {Age}");
		sb.AppendLine($"Speech: {Speech}");
		sb.AppendLine($"SkinTone: {SkinTone}");
		sb.AppendLine($"JobPreferences: \n\t{string.Join("\n\t", JobPreferences)}");
		sb.AppendLine($"AntagPreferences: \n\t{string.Join("\n\t", AntagPreferences)}");
		return sb.ToString();
	}

	/// <summary>
	/// Does nothing if all the character's properties are valid
	/// <exception cref="InvalidOperationException">If the character settings are not valid</exception>
	/// </summary>
	public void ValidateSettings()
	{
		ValidateName();
		ValidateJobPreferences();
	}

	/// <summary>
	/// Checks if the character name follows all rules
	/// </summary>
	/// <exception cref="InvalidOperationException">If the name not valid</exception>
	private void ValidateName()
	{
		if (String.IsNullOrWhiteSpace(Name))
		{
			throw new InvalidOperationException("Name cannot be blank");
		}

		if (Name.Length > MAX_NAME_LENGTH)
		{
			throw new InvalidOperationException("Name cannot exceed " + MAX_NAME_LENGTH + " characters");
		}
	}

	/// <summary>
	/// Checks if the job preferences have more than one high priority set
	/// </summary>
	/// <exception cref="InvalidOperationException">If the job preferences are not valid</exception>
	private void ValidateJobPreferences()
	{
		if (JobPreferences.Count(jobPref => jobPref.Value == Priority.High) > 1)
		{
			throw new InvalidOperationException("Cannot have more than one job set to high priority");
		}
	}

	/// <summary>
	/// Returns a possessive string (i.e. "their", "his", "her") for the provided gender enum.
	/// </summary>
	public string TheirPronoun()
	{
		switch (BodyType)
		{
			case BodyType.Male:
				return "his";
			case BodyType.Female:
				return "her";
			default:
				return "their";
		}
	}

	/// <summary>
	/// Returns a personal pronoun string (i.e. "he", "she", "they") for the provided gender enum.
	/// </summary>
	public string TheyPronoun()
	{
		switch (BodyType)
		{
			case BodyType.Male:
				return "he";
			case BodyType.Female:
				return "she";
			default:
				return "they";
		}
	}
	/// <summary>
	/// Returns an object pronoun string (i.e. "him", "her", "them") for the provided gender enum.
	/// </summary>
	public string ThemPronoun()
	{
		switch (BodyType)
		{
			case BodyType.Male:
				return "him";
			case BodyType.Female:
				return "her";
			default:
				return "them";
		}
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "he's", "she's", "they're") for the provided gender enum.
	/// </summary>
	public string TheyrePronoun()
	{
		switch (BodyType)
		{
			case BodyType.Male:
				return "he's";
			case BodyType.Female:
				return "she's";
			default:
				return "they're";
		}
	}
}