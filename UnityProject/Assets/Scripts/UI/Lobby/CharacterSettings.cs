using System;

[Serializable]
public class CharacterSettings
{
	public const int MAX_NAME_LENGTH = 28; //Arbitrary limit, but it seems reasonable
	public string username;
	public string Name = "Cuban Pete";
	public Gender Gender = Gender.Male;
	public int Age = 22;
	public string hairStyleName = "None";
	public string hairColor = "black";
	public string eyeColor = "black";
	public string facialHairName = "None";
	public string facialHairColor = "black";
	public string skinTone = "#ffe0d1";
	public string underwearName = "Mankini";
	public string socksName = "Knee-High (Freedom)";
	//add Reference to player race Data, When you can select different races

	public void LoadHairSetting(string hair)
	{
		hairStyleName = hair;
	}

	public void LoadFacialHairSetting(string facialHair)
	{
		facialHairName = facialHair;
	}

	public void LoadUnderwearSetting(string underwear)
	{
		underwearName = underwear;
	}

	public void LoadSocksSetting(string socks)
	{
		socksName = socks;
	}

	/// <summary>
	/// Does nothing if all the character's properties are valides
	/// <exception cref="InvalidOperationException">If the charcter settings are not valid</exception>
	/// </summary>
	public void ValidateSettings()
	{
		ValidateName();
	}

	/// <summary>
	/// Checks if the character name follows all rules
	/// </summary>
	/// <exception cref="InvalidOperationException">If the name not valid</exception>
	public void ValidateName()
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