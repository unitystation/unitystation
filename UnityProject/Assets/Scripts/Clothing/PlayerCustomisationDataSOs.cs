using System;
using System.Collections.Generic;
using System.Linq;
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
	private PlayerCustomisationDictionary playerCustomisationDictionary;

	/// <summary>
	/// Returns a PlayerCustomisationData using the type and name.
	/// Returns null if not found.
	/// </summary>
	public PlayerCustomisationData Get(CustomisationType type, string customisationName)
	{
		if (!playerCustomisationDictionary.ContainsKey(type))
		{
			Logger.LogErrorFormat("No entries for {0} CustomisationType", Category.Character, type);
			return null;
		}

		return playerCustomisationDictionary[type].FirstOrDefault(pcd =>
			pcd.Type == type && pcd.Name == customisationName);
	}

	/// <summary>
	/// Returns the first customisation type it can find
	/// </summary>
	private PlayerCustomisationData GetFirst(CustomisationType type)
	{
		if (!playerCustomisationDictionary.ContainsKey(type))
		{
			Logger.LogErrorFormat("No entries for {0} CustomisationType", Category.Character, type);
			return null;
		}

		return playerCustomisationDictionary[type].FirstOrDefault(pcd => pcd.Type == type);
	}

	/// <summary>
	/// Returns all PlayerCustomisationDatas of a certain type.
	/// </summary>
	public IEnumerable<PlayerCustomisationData> GetAll(CustomisationType type)
	{
		if (!playerCustomisationDictionary.ContainsKey(type))
		{
			Logger.LogErrorFormat("No entries for {0} CustomisationType", Category.Character, type);
			return null;
		}

		return playerCustomisationDictionary[type].Where(pcd => pcd.Type == type);
	}

	/// <summary>
	/// Validates all character settings that use strings (excluding colors).
	/// Will reset settings to a default value if they are invalid.
	/// </summary>
	/// <param name="character">The character settings to validate</param>
	public bool ValidateCharacterSettings(ref CharacterSettings character)
	{
		var result = true;
		if (!IsSettingValid(CustomisationType.HairStyle, character.HairStyleName, out string defaultSetting));
		{
			character.HairStyleName = defaultSetting;
			result = false;
		}

		if (!IsSettingValid(CustomisationType.FacialHair, character.FacialHairName, out defaultSetting));
		{
			character.FacialHairName = defaultSetting;
			result = false;
		}

		if (!IsSettingValid(CustomisationType.Underwear, character.UnderwearName, out defaultSetting));
		{
			character.UnderwearName = defaultSetting;
			result = false;
		}

		if (!IsSettingValid(CustomisationType.Socks, character.SocksName, out defaultSetting));
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
	/// <param name="settingName">The name of the setting</param>
	/// <param name="defaultSettingName">The default setting to assign on failure</param>
	/// <returns></returns>
	private bool IsSettingValid(CustomisationType type, string settingName, out string defaultSettingName)
	{
		if (Get(type, settingName) != null)
		{
			defaultSettingName = string.Empty;
			return true;
		}

		defaultSettingName = GetFirst(type).Name;
		if (string.IsNullOrEmpty(defaultSettingName))
		{
			Logger.LogError("testing");
			Logger.LogErrorFormat(
				"Cannot find a default {0} setting, have customisation options been populated correctly?",
				Category.Character, type);
		}
		else
		{
			Logger.LogWarningFormat("Invalid {0} setting: cannot find {1}. Resetting to {2}.",
				Category.Character, type, settingName, defaultSettingName);
		}
		return false;
	}

	[Serializable]
	private class PlayerCustomisationListStorage : SerializableDictionary.Storage<List<PlayerCustomisationData>> {}

	[Serializable]
	private class PlayerCustomisationDictionary : SerializableDictionary<CustomisationType, List<PlayerCustomisationData>, PlayerCustomisationListStorage>
	{
	}
}
