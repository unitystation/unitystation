using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class CharacterCustomization : MonoBehaviour
	{
		public InputField characterNameField;
		public InputField ageField;
		public Text errorLabel;
		public Text genderText;
		public Image hairColor;
		public Image eyeColor;
		public Image facialColor;
		public Image skinColor;

		public Dropdown hairDropdown;
		public Dropdown facialHairDropdown;
		public Dropdown underwearDropdown;
		public Dropdown socksDropdown;

		public CharacterSprites hairSpriteController;
		public CharacterSprites facialHairSpriteController;
		public CharacterSprites underwearSpriteController;
		public CharacterSprites socksSpriteController;
		public CharacterSprites eyesSpriteController;
		public CharacterSprites[] skinControllers;

		public CharacterSprites torsoSpriteController;
		public CharacterSprites headSpriteController;

		[SerializeField]
		public List<string> availableHairColors = new List<string>();

		[SerializeField]
		public List<string> availableSkinColors = new List<string>();
		private CharacterSettings currentCharacter;

		public ColorPicker colorPicker;

		private CharacterSettings lastSettings;

		void OnEnable()
		{
			LoadSettings();
			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.AddListener(OnColorChange);
			var copyStr = JsonUtility.ToJson(currentCharacter);
			lastSettings = JsonUtility.FromJson<CharacterSettings>(copyStr);
			DisplayErrorText("");
		}

		void OnDisable()
		{
			colorPicker.onValueChanged.RemoveListener(OnColorChange);
		}
		private void LoadSettings()
		{
			currentCharacter = PlayerManager.CurrentCharacterSettings;
			PopulateAllDropdowns();
			DoInitChecks();
		}

		//First time setting up this character etc?
		private void DoInitChecks()
		{
			if (string.IsNullOrEmpty(currentCharacter.username))
			{
				currentCharacter.username = GameData.LoggedInUsername;
				RollRandomCharacter();
				SaveData();
			}
			else
			{
				SetAllDropdowns();
				RefreshAll();
			}
		}

		private void RefreshAll()
		{
			RefreshName();
			RefreshGender();
			RefreshHair();
			RefreshFacialHair();
			RefreshUnderwear();
			RefreshSocks();
			RefreshEyes();
			RefreshSkinColor();
			RefreshAge();
		}

		public void RollRandomCharacter()
		{
			currentCharacter.Gender = (Gender) UnityEngine.Random.Range(0, 2);

			// Repopulate underwear and facialhair dropdown boxes incase gender changes
			PopulateDropdown(SpriteManager.UnderwearCollection, underwearDropdown, true);
			PopulateDropdown(SpriteManager.FacialHairCollection, facialHairDropdown, true);

			// Select a random value from each dropdown
			hairDropdown.value = UnityEngine.Random.Range(0, hairDropdown.options.Count - 1);
			facialHairDropdown.value = UnityEngine.Random.Range(0, facialHairDropdown.options.Count - 1);
			underwearDropdown.value = UnityEngine.Random.Range(0, underwearDropdown.options.Count - 1);
			socksDropdown.value = UnityEngine.Random.Range(0, socksDropdown.options.Count - 1);

			// Do gender specific randomisation
			if (currentCharacter.Gender == Gender.Male)
			{
				currentCharacter.Name = StringManager.GetRandomMaleName();
				currentCharacter.facialHairColor = availableHairColors[UnityEngine.Random.Range(0, availableHairColors.Count - 1)];				
			}
			else
			{
				currentCharacter.Name = StringManager.GetRandomFemaleName();
			}

			// Randomise rest of data
			currentCharacter.hairColor = availableHairColors[UnityEngine.Random.Range(0, availableHairColors.Count - 1)];	
			currentCharacter.skinTone = availableSkinColors[UnityEngine.Random.Range(0, availableSkinColors.Count - 1)];
			currentCharacter.Age = UnityEngine.Random.Range(19, 78);

			RefreshAll();
		}

		//------------------
		//DROPDOWN BOXES:
		//------------------
		private void PopulateAllDropdowns()
		{
			PopulateDropdown(SpriteManager.HairCollection, hairDropdown);
			PopulateDropdown(SpriteManager.FacialHairCollection, facialHairDropdown, true);
			PopulateDropdown(SpriteManager.UnderwearCollection, underwearDropdown, true);
			PopulateDropdown(SpriteManager.SocksCollection, socksDropdown);
		}

		private void PopulateDropdown(List<SpriteAccessory> itemCollection, Dropdown itemDropdown, bool constrainGender = false )
		{
			// Clear out old options
			itemDropdown.ClearOptions();

			// Make a list of all available options which can then be passed to the dropdown box
			List<string> itemOptions = new List<string>();

			foreach (SpriteAccessory item in itemCollection)
			{
				// Check if options are being constrained by gender, only add valid gender options if so
				if (constrainGender)
				{
					if (item.gender == currentCharacter.Gender || item.gender == Gender.Neuter)
					{
						itemOptions.Add(item.name);
					}
				}
				else
				{
					itemOptions.Add(item.name);
				}

			}

			itemDropdown.AddOptions(itemOptions);
		}

		private void SetAllDropdowns()
		{
			SetDropdownValue(hairDropdown, currentCharacter.hairStyleName);
			SetDropdownValue(facialHairDropdown, currentCharacter.facialHairName);
			SetDropdownValue(underwearDropdown, currentCharacter.underwearName);
			SetDropdownValue(socksDropdown, currentCharacter.socksName);
		}

		private void SetDropdownValue(Dropdown itemDropdown, string currentSetting)
		{
			// Find the index of the setting in the dropdown list which matches the currentSetting
			int settingIndex = itemDropdown.options.FindIndex( option => option.text == currentSetting);

			if (settingIndex != -1)
			{
				// Make sure FindIndex is successful before changing value
				itemDropdown.value = settingIndex;
			}
			else
			{
				Logger.LogError($"Unable to find index of {currentSetting}!", Category.UI);
			}
		}

		public void DropdownScrollRight(Dropdown dropdown)
		{
			// Check if value should wrap around
			if (dropdown.value < dropdown.options.Count - 1)
			{
				dropdown.value++;
			}
			else
			{
				dropdown.value = 0;
			}
			// No need to call Refresh() since it gets called when value changes
		}

		public void DropdownScrollLeft(Dropdown dropdown)
		{
			// Check if value should wrap around
			if (dropdown.value > 0)
			{
				dropdown.value--;
			}
			else
			{
				dropdown.value = dropdown.options.Count - 1;
			}

			// No need to call Refresh() since it gets called when value changes
		}		

		//------------------
		//PLAYER ACCOUNTS:
		//------------------
		private void SaveData()
		{ 
			ServerData.UpdateCharacterProfile(currentCharacter, SaveDataSuccess, SaveDataError);
			PlayerPrefs.SetString("currentcharacter", JsonUtility.ToJson(currentCharacter));
			PlayerPrefs.Save();
		}

		public void SaveDataError(string msg)
		{
			//Log out on any error for the moment:
			GameData.IsLoggedIn = false;
			Logger.LogError(msg, Category.DatabaseAPI);
		}

		public void SaveDataSuccess(string msg)
		{
			Debug.Log("TODO: Turn on nav panel top");
		}

		//------------------
		//SAVE & CANCEL BUTTONS:
		//------------------

		public void OnApplyBtn()
		{
			DisplayErrorText("");
			try
			{
				currentCharacter.ValidateSettings();
			}
			catch (InvalidOperationException e)
			{
				Debug.Log("Invalid character settings. " + e.Message);
				DisplayErrorText(e.Message);
				return;
			}
			SaveData();
			LobbyManager.Instance.lobbyDialogue.gameObject.SetActive(true);
			if (GameData.IsLoggedIn)
			{
				LobbyManager.Instance.lobbyDialogue.ShowConnectionPanel();
			}
			else
			{
				LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
			}
			gameObject.SetActive(false);
		}

		public void OnCancelBtn()
		{
			currentCharacter = lastSettings;
			RefreshAll();
			LobbyManager.Instance.lobbyDialogue.gameObject.SetActive(true);
			if (GameData.IsLoggedIn)
			{
				LobbyManager.Instance.lobbyDialogue.ShowConnectionPanel();
			}
			else
			{
				LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
			}
			gameObject.SetActive(false);
		}

		private void DisplayErrorText(string message)
		{
			errorLabel.text = message;
		}

		//------------------
		//NAME:
		//------------------
		private void RefreshName()
		{
			characterNameField.text = TruncateName(currentCharacter.Name);
		}

		public void RandomNameBtn()
		{
			if (currentCharacter.Gender == Gender.Male)
			{
				currentCharacter.Name = TruncateName(StringManager.GetRandomMaleName());
			}
			else
			{
				currentCharacter.Name = TruncateName(StringManager.GetRandomFemaleName());
			}
			RefreshName();
		}

		public void OnManualNameChange()
		{
			currentCharacter.Name = TruncateName(characterNameField.text);
			characterNameField.text = currentCharacter.Name;
		}

		private string TruncateName(string proposedName)
		{
			if (proposedName.Length >= CharacterSettings.MAX_NAME_LENGTH)
			{
				return proposedName.Substring(0, CharacterSettings.MAX_NAME_LENGTH);
			}
			return proposedName;
		}

		//------------------
		//GENDER:
		//------------------

		public void OnGenderChange()
		{
			int gender = (int) currentCharacter.Gender;
			gender++;
			if (gender == (int) Gender.Neuter)
			{
				gender = 0;
			}
			currentCharacter.Gender = (Gender) gender;

			// Repopulate underwear and facial hair dropdown boxes
			PopulateDropdown(SpriteManager.UnderwearCollection, underwearDropdown, true);
			PopulateDropdown(SpriteManager.FacialHairCollection, facialHairDropdown, true);
			
			// Set underwear and facial hair to default setting (nude, and shaved)
			SetDropdownValue(underwearDropdown, "Nude");
			SetDropdownValue(facialHairDropdown, "Shaved");

			RefreshGender();
		}

		private void RefreshGender()
		{
			genderText.text = currentCharacter.Gender.ToString();
			currentCharacter.RefreshGenderBodyParts();
			headSpriteController.reference = currentCharacter.headSpriteIndex;
			torsoSpriteController.reference = currentCharacter.torsoSpriteIndex;
			headSpriteController.UpdateSprite();
			torsoSpriteController.UpdateSprite();	
		}

		//------------------
		//AGE:
		//------------------

		private void RefreshAge()
		{
			ageField.text = currentCharacter.Age.ToString();
		}

		public void OnAgeChange()
		{
			int tryInt = 22;
			int.TryParse(ageField.text, out tryInt);
			tryInt = Mathf.Clamp(tryInt, 18, 99);
			currentCharacter.Age = tryInt;
			RefreshAge();
		}
		//------------------
		//EYE COLOR:
		//------------------

		public void OpenEyesColorPicker()
		{
			if (!colorPicker.gameObject.activeInHierarchy)
			{
				OpenColorPicker(eyeColor.color, EyeColorChange, 364f);
			}
		}

		private void EyeColorChange(Color newColor)
		{
			currentCharacter.eyeColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
			RefreshEyes();
		}

		private void RefreshEyes()
		{
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.eyeColor, out setColor);
			eyesSpriteController.image.color = setColor;
			eyeColor.color = setColor;
		}

		//------------------
		//HAIR:
		//------------------

		public void HairDropdownChange(int index)
		{
			currentCharacter.LoadHairSetting(SpriteManager.HairCollection[index]);
			RefreshHair();
		}

		//Scrolls have been disabled for time being:

		// public void HairColorScrollRight()
		// {
		// 	int tryNext = availableHairColors.IndexOf(currentCharacter.hairColor);
		// 	tryNext++;
		// 	if (tryNext == availableHairColors.Count)
		// 	{
		// 		tryNext = 0;
		// 	}
		// 	currentCharacter.hairColor = availableHairColors[tryNext];
		// 	RefreshHair();
		// }

		// public void HairColorScrollLeft()
		// {
		// 	int tryNext = availableHairColors.IndexOf(currentCharacter.hairColor);
		// 	tryNext--;
		// 	if (tryNext < 0)
		// 	{
		// 		tryNext = availableHairColors.Count - 1;
		// 	}
		// 	currentCharacter.hairColor = availableHairColors[tryNext];
		// 	RefreshHair();
		// }

		public void OpenHairColorPicker()
		{
			if (!colorPicker.gameObject.activeInHierarchy)
			{
				OpenColorPicker(hairColor.color, HairColorChange, 374f);
			}
		}

		private void HairColorChange(Color newColor)
		{
			hairColor.color = newColor;
			currentCharacter.hairColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
			RefreshHair();
		}

		private void RefreshHair()
		{
			hairSpriteController.reference = currentCharacter.hairStyleOffset;
			hairSpriteController.UpdateSprite();
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.hairColor, out setColor);
			hairSpriteController.image.color = setColor;
			hairColor.color = setColor;
		}

		//------------------
		//FACIAL HAIR:
		//------------------

		public void FacialHairDropdownChange(int index)
		{
			SpriteAccessory newFacialHair = SpriteManager.FacialHairCollection.Find(item => item.name == facialHairDropdown.options[index].text);
			if (newFacialHair.name != null)
			{
				currentCharacter.LoadFacialHairSetting(newFacialHair);	
			}
			else
			{
				Logger.LogError($"Unable to find {facialHairDropdown.options[index].text} in UnderwearCollection!", Category.UI);
			}
			RefreshFacialHair();
		}

		private void RefreshFacialHair()
		{
			facialHairSpriteController.reference = currentCharacter.facialHairOffset;
			facialHairSpriteController.UpdateSprite();
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.facialHairColor, out setColor);
			facialHairSpriteController.image.color = setColor;
			facialColor.color = setColor;
		}

		public void OpenFacialHairPicker()
		{
			if (!colorPicker.gameObject.activeInHierarchy)
			{
				OpenColorPicker(facialColor.color, FacialHairColorChange, 334f);
			}
		}

		private void FacialHairColorChange(Color newColor)
		{
			currentCharacter.facialHairColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
			RefreshFacialHair();
		}

		//------------------
		//SKIN TONE:
		//------------------
		private void RefreshSkinColor()
		{
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.skinTone, out setColor);
			skinColor.color = setColor;
			for (int i = 0; i < skinControllers.Length; i++)
			{
				skinControllers[i].image.color = setColor;
			}
		}

		public void SkinToneScrollRight()
		{
			int index = availableSkinColors.IndexOf(currentCharacter.skinTone);
			index++;
			if (index == availableSkinColors.Count)
			{
				index = 0;
			}
			currentCharacter.skinTone = availableSkinColors[index];
			RefreshSkinColor();
		}

		public void SkinToneScrollLeft()
		{
			int index = availableSkinColors.IndexOf(currentCharacter.skinTone);
			index--;
			if (index < 0)
			{
				index = availableSkinColors.Count - 1;
			}
			currentCharacter.skinTone = availableSkinColors[index];
			RefreshSkinColor();
		}

		//------------------
		//UNDERWEAR:
		//------------------

		public void UnderwearDropdownChange(int index)
		{
			SpriteAccessory newUnderwear = SpriteManager.UnderwearCollection.Find(item => item.name == underwearDropdown.options[index].text);
			if (newUnderwear.name != null)
			{
				currentCharacter.LoadUnderwearSetting(newUnderwear);	
			}
			else
			{
				Logger.LogError($"Unable to find {underwearDropdown.options[index].text} in UnderwearCollection!", Category.UI);
			}
			RefreshUnderwear();
		}
		private void RefreshUnderwear()
		{
			underwearSpriteController.reference = currentCharacter.underwearOffset;
			underwearSpriteController.UpdateSprite();
		}

		//------------------
		//SOCKS:
		//------------------

		public void SocksDropdownChange(int index)
		{
			currentCharacter.LoadSocksSetting(SpriteManager.SocksCollection[index]);
			RefreshSocks();
		}
		private void RefreshSocks()
		{
			socksSpriteController.reference = currentCharacter.socksOffset;
			socksSpriteController.UpdateSprite();
		}

		//------------------
		//COLOR SELECTOR:
		//------------------

		private void OpenColorPicker(Color currentColor, Action<Color> _colorChangeEvent, float yPos)
		{
			var rectT = colorPicker.gameObject.GetComponent<RectTransform>();
			var anchoredPos = rectT.anchoredPosition;
			anchoredPos.y = yPos;
			rectT.anchoredPosition = anchoredPos;
			colorPicker.gameObject.SetActive(true);
			colorChangedEvent = _colorChangeEvent;
			beforeEditingColor = currentColor;
			colorPicker.CurrentColor = currentColor;
		}

		private Action<Color> colorChangedEvent;
		private Color beforeEditingColor;
		private void OnColorChange(Color newColor)
		{
			colorChangedEvent.Invoke(newColor);
		}

		public void CancelColorPicker()
		{
			colorChangedEvent.Invoke(beforeEditingColor);
			CloseColorPicker();
		}

		public void CloseColorPicker()
		{
			colorChangedEvent = null;
			colorPicker.gameObject.SetActive(false);
		}
	}
}

[Serializable]
public class CharacterSettings
{
	public const int MAX_NAME_LENGTH = 28; //Arbitrary limit, but it seems reasonable
	public string username;
	public string Name = "Cuban Pete";
	public Gender Gender = Gender.Male;
	public int Age = 22;
	public int hairStyleOffset = -1;
	public string hairStyleName = "Bald";
	public int hairCollectionIndex = 4;
	public string hairColor = "black";
	public string eyeColor = "black";
	public int facialHairOffset = -1;
	public string facialHairName = "Shaved";
	public int facialHairCollectionIndex = 4;
	public string facialHairColor = "black";
	public string skinTone = "#ffe0d1";
	public int underwearOffset = 20;
	public string underwearName = "Mankini";
	public int underwearCollectionIndex = 1;
	public int socksOffset = 376;
	public string socksName = "Knee-High (Freedom)";
	public int socksCollectionIndex = 3;

	int maleHeadIndex = 20;
	int femaleHeadIndex = 24;
	int maleTorsoIndex = 28;
	int femaleTorsoIndex = 32;

	public int headSpriteIndex = 20;
	public int torsoSpriteIndex = 28;

	public void LoadHairSetting(SpriteAccessory hair)
	{
		hairStyleOffset = hair.spritePos;
		hairStyleName = hair.name;
		hairCollectionIndex = SpriteManager.HairCollection.IndexOf(hair);
	}

	public void LoadFacialHairSetting(SpriteAccessory facialHair)
	{
		facialHairOffset = facialHair.spritePos;
		facialHairName = facialHair.name;
		facialHairCollectionIndex = SpriteManager.FacialHairCollection.IndexOf(facialHair);
	}

	public void LoadUnderwearSetting(SpriteAccessory underwear)
	{
		underwearOffset = underwear.spritePos;
		underwearName = underwear.name;
		underwearCollectionIndex = SpriteManager.UnderwearCollection.IndexOf(underwear);
	}

	public void LoadSocksSetting(SpriteAccessory socks)
	{
		socksOffset = socks.spritePos;
		socksName = socks.name;
		socksCollectionIndex = SpriteManager.SocksCollection.IndexOf(socks);
	}

	public void RefreshGenderBodyParts()
	{
		if (Gender == Gender.Male)
		{
			torsoSpriteIndex = maleTorsoIndex;
			headSpriteIndex = maleHeadIndex;
		}
		else
		{
			torsoSpriteIndex = femaleTorsoIndex;
			headSpriteIndex = femaleHeadIndex;
		}
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


}

public enum Gender
{
	Male,
	Female,
	Neuter //adding anymore genders will break things do not edit
}