using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCustomisationDataSOs", menuName = "Singleton/PlayerCustomisationData")]
public class PlayerCustomisationDataSOs : SingletonScriptableObject<PlayerCustomisationDataSOs>
{
	/// <summary>
	/// This stores all customisation SOs according to their type.
	/// </summary>
	[Header("Drag multiple ScriptableObjects into the lists to populate them")]
	[Tooltip("Contains entries for each customisation type which should list all customisation options")]
	[SerializeField]
	private PlayerCustomisationDictionary playerCustomisationDictionary = null;

	/// <summary>
	/// Returns a PlayerCustomisationData using the type and name.
	/// Returns null if not found.
	/// </summary>
	public PlayerCustomisationData Get(CustomisationType type, Gender gender, string customisationName)
	{
		if (!IsTypePopulated(type))
		{
			return null;
		}

		return playerCustomisationDictionary[type].FirstOrDefault(pcd =>
			pcd.Type == type && (pcd.gender == gender || pcd.gender == Gender.Neuter) && pcd.Name == customisationName);
	}

	/// <summary>
	/// Returns the first customisation type it can find
	/// </summary>
	private PlayerCustomisationData GetFirst(CustomisationType type, Gender gender)
	{
		if (!IsTypePopulated(type))
		{
			return null;
		}

		return playerCustomisationDictionary[type].FirstOrDefault(pcd =>
			pcd.Type == type && (pcd.gender == gender || pcd.gender == Gender.Neuter));
	}

	/// <summary>
	/// Returns all PlayerCustomisationDatas of a certain type.
	/// </summary>
	public IEnumerable<PlayerCustomisationData> GetAll(CustomisationType type, Gender gender)
	{
		if (!IsTypePopulated(type))
		{
			return null;
		}

		return playerCustomisationDictionary[type].Where(pcd =>
			pcd.Type == type && (pcd.gender == gender || pcd.gender == Gender.Neuter));
	}

	private bool IsTypePopulated(CustomisationType type)
	{
		if (playerCustomisationDictionary.ContainsKey(type) &&
			playerCustomisationDictionary[type].Any())
		{
			return true;
		}

		Logger.LogErrorFormat(
			"No entries for {0} CustomisationType. Have they been populated correctly in the inspector?",
			Category.Character, type);
		return false;

	}

	/// <summary>
	/// Validates all character settings that use strings (excluding colors).
	/// Will reset settings to a default value if they are invalid.
	/// </summary>
	/// <param name="character">The character settings to validate</param>
	public bool ValidateCharacterSettings(ref CharacterSettings character)
	{
		var result = true;
		if (!IsSettingValid(CustomisationType.HairStyle, character.Gender,
			character.HairStyleName, out string defaultSetting))
		{
			character.HairStyleName = defaultSetting;
			result = false;
		}

		if (!IsSettingValid(CustomisationType.FacialHair, character.Gender,
			character.FacialHairName, out defaultSetting))
		{
			character.FacialHairName = defaultSetting;
			result = false;
		}

		if (!IsSettingValid(CustomisationType.Underwear, character.Gender,
			character.UnderwearName, out defaultSetting))
		{
			character.UnderwearName = defaultSetting;
			result = false;
		}

		if (!IsSettingValid(CustomisationType.Socks, character.Gender,
			character.SocksName, out defaultSetting))
		{
			character.SocksName = defaultSetting;
			result = false;
		}
		return result;
	}

	/// <summary>
	/// Checks if a customisation type with settingName exists.
	/// If it can't find one then defaultSettingName contains a default one.
	/// </summary>
	/// <param name="type">Customisation type of settingName</param>
	/// <param name="gender">The gender of the customisation option</param>
	/// <param name="settingName">The name of the setting</param>
	/// <param name="defaultSettingName">The default setting to assign on failure</param>
	/// <returns></returns>
	private bool IsSettingValid(CustomisationType type, Gender gender, string settingName, out string defaultSettingName)
	{
		var foundSetting = Get(type, gender, settingName);
		if (settingName == "None" || foundSetting != null)
		{
			defaultSettingName = string.Empty;
			return true;
		}

		defaultSettingName = GetFirst(type, gender)?.Name ?? "None";
		Logger.LogWarningFormat("Invalid {0} setting: cannot find {1}. Resetting to {2}.",
				Category.Character, type, settingName, defaultSettingName);
		return false;
	}

	[Serializable]
	private class PlayerCustomisationListStorage : SerializableDictionary.Storage<List<PlayerCustomisationData>> {}

	[Serializable]
	private class PlayerCustomisationDictionary : SerializableDictionary<CustomisationType, List<PlayerCustomisationData>, PlayerCustomisationListStorage>
	{
	}
}
