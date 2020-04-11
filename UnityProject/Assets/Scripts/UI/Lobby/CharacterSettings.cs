using System;
using System.Linq;

/// <summary>
/// Class containing all character preferences for a player
/// Includes appearance, job preferences etc...
/// </summary>
public class CharacterSettings
{
	public const int MAX_NAME_LENGTH = 26; //Arbitrary limit, but 26 is the max the current UI can fit
	public string Username;
	public string Name = "Cuban Pete";
	public Gender Gender = Gender.Male;
	public ClothingStyle ClothingStyle = ClothingStyle.JumpSuit;
	public BagStyle BagStyle = BagStyle.Backpack;
	public int Age = 22;
	public Speech Speech = Speech.None;
	public string HairStyleName = "None";
	public string HairColor = "black";
	public string EyeColor = "black";
	public string FacialHairName = "None";
	public string FacialHairColor = "black";
	public string SkinTone = "#ffe0d1";
	public string UnderwearName = "Mankini";
	public string SocksName = "Knee-High (Freedom)";
	public JobPrefsDict JobPreferences = new JobPrefsDict();

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
	public string PossessivePronoun()
	{
		switch (Gender)
		{
			case Gender.Male:
				return "his";
			case Gender.Female:
				return "her";
			default:
				return "their";
		}
	}

	/// <summary>
	/// Returns a personal pronoun string (i.e. "he", "she", "they") for the provided gender enum.
	/// </summary>
	public string PersonalPronoun()
	{
		switch (Gender)
		{
			case Gender.Male:
				return "he";
			case Gender.Female:
				return "she";
			default:
				return "they";
		}
	}
}