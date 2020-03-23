using System;
using System.Linq;

public class CharacterSettings
{
	public const int MAX_NAME_LENGTH = 28; //Arbitrary limit, but it seems reasonable
	public string Username;
	public string Name = "Cuban Pete";
	public Gender Gender = Gender.Male;
	public int Age = 22;
	public string HairStyleName = "None";
	public string HairColor = "black";
	public string EyeColor = "black";
	public string FacialHairName = "None";
	public string FacialHairColor = "black";
	public string SkinTone = "#ffe0d1";
	public string UnderwearName = "Mankini";
	public string SocksName = "Knee-High (Freedom)";
	public JobPrefsDict JobPreferences;

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