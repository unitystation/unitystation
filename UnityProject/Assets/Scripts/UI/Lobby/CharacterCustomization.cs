using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
		public CharacterSprites RarmSpriteController;
		public CharacterSprites LarmSpriteController;
		public CharacterSprites RlegSpriteController;
		public CharacterSprites LlegSpriteController;

		public CharacterSprites RHandSpriteController;
		public CharacterSprites LHandSpriteController;

		public PlayerTextureData playerTextureData;

		[SerializeField]
		public List<string> availableSkinColors = new List<string>();
		private CharacterSettings currentCharacter;

		public ColorPicker colorPicker;

		private CharacterSettings lastSettings;

		public Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>> playerCustomisationData = new Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>>();

		public Action onCloseAction;

		void OnEnable()
		{
			LoadSettings();
			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.AddListener(OnColorChange);
			var copyStr = JsonUtility.ToJson(currentCharacter);
			lastSettings = JsonUtility.FromJson<CharacterSettings>(copyStr);
			DisplayErrorText("");

			torsoSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.Torso);
			headSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.Head);
			RarmSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.ArmRight);
			LarmSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.ArmLeft);
			RlegSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.LegRight);
			LlegSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.LegLeft);
			RHandSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.HandRight);
			LHandSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.HandLeft);
			eyesSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Base.Eyes);
		}

		void OnDisable()
		{
			colorPicker.onValueChanged.RemoveListener(OnColorChange);
		}
		private void LoadSettings()
		{
			currentCharacter = PlayerManager.CurrentCharacterSettings;
			//If we are playing locally offline, init character settings if they're null
			if (currentCharacter == null)
			{
				currentCharacter = new CharacterSettings();
				PlayerManager.CurrentCharacterSettings = currentCharacter;
			}
			PopulateAllDropdowns();
			DoInitChecks();
		}

		//First time setting up this character etc?
		private void DoInitChecks()
		{
			if (string.IsNullOrEmpty(currentCharacter.username))
			{
				currentCharacter.username = ServerData.Auth.CurrentUser.DisplayName;
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
			currentCharacter.Gender = (Gender)UnityEngine.Random.Range(0, 2);

			// Repopulate underwear and facialhair dropdown boxes incase gender changes
			if (playerCustomisationData.ContainsKey(PlayerCustomisation.FacialHair))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.FacialHair], facialHairDropdown); }

			if (playerCustomisationData.ContainsKey(PlayerCustomisation.Underwear))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.Underwear], underwearDropdown); }


			// Select a random value from each dropdown
			hairDropdown.value = UnityEngine.Random.Range(0, hairDropdown.options.Count - 1);
			facialHairDropdown.value = UnityEngine.Random.Range(0, facialHairDropdown.options.Count - 1);
			underwearDropdown.value = UnityEngine.Random.Range(0, underwearDropdown.options.Count - 1);
			socksDropdown.value = UnityEngine.Random.Range(0, socksDropdown.options.Count - 1);

			// Do gender specific randomisation
			if (currentCharacter.Gender == Gender.Male)
			{
				currentCharacter.Name = StringManager.GetRandomMaleName();
				currentCharacter.facialHairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			}
			else
			{
				currentCharacter.Name = StringManager.GetRandomFemaleName();
			}

			// Randomise rest of data
			currentCharacter.eyeColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			currentCharacter.hairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			currentCharacter.skinTone = availableSkinColors[UnityEngine.Random.Range(0, availableSkinColors.Count - 1)];
			currentCharacter.Age = UnityEngine.Random.Range(19, 78);

			RefreshAll();
		}

		//------------------
		//DROPDOWN BOXES:
		//------------------
		private void PopulateAllDropdowns()
		{
			if (playerCustomisationData.ContainsKey(PlayerCustomisation.HairStyle))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.HairStyle], hairDropdown); }

			if (playerCustomisationData.ContainsKey(PlayerCustomisation.FacialHair))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.FacialHair], facialHairDropdown); }

			if (playerCustomisationData.ContainsKey(PlayerCustomisation.Underwear))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.Underwear], underwearDropdown); }

			if (playerCustomisationData.ContainsKey(PlayerCustomisation.Socks))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.Socks], socksDropdown); }

		}

		private void PopulateDropdown(Dictionary<string, PlayerCustomisationData> itemCollection, Dropdown itemDropdown, bool constrainGender = false)
		{
			// Clear out old options
			itemDropdown.ClearOptions();

			// Make a list of all available options which can then be passed to the dropdown box
			List<string> itemOptions = new List<string>();

			foreach (var item in itemCollection)
			{
				// Check if options are being constrained by gender, only add valid gender options if so
				if (constrainGender)
				{
					if (item.Value.gender == currentCharacter.Gender || item.Value.gender == Gender.Neuter)
					{
						itemOptions.Add(item.Key);
					}
				}
				else
				{
					itemOptions.Add(item.Key);
				}

			}
			itemOptions.Sort();
			// Ensure "None" is at the top of the option lists
			itemOptions.Insert(0, "None");
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
			int settingIndex = itemDropdown.options.FindIndex(option => option.text == currentSetting);

			if (settingIndex != -1)
			{
				// Make sure FindIndex is successful before changing value
				itemDropdown.value = settingIndex;
			}
			else
			{
				Logger.LogWarning($"Unable to find index of {currentSetting}! Using default", Category.UI);
				itemDropdown.value = 0;
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
			ServerData.UpdateCharacterProfile(JsonUtility.ToJson(currentCharacter)); // TODO Consider adding await. Otherwise this causes a compile warning.
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
				Logger.LogFormat("Invalid character settings: {0}", Category.UI, e.Message);
				DisplayErrorText(e.Message);
				return;
			}
			SaveData();
			LobbyManager.Instance.lobbyDialogue.gameObject.SetActive(true);
			if (ServerData.Auth.CurrentUser != null)
			{
				if (onCloseAction != null)
				{
					onCloseAction.Invoke();
					onCloseAction = null;
				}
				else
				{
					LobbyManager.Instance.lobbyDialogue.ShowConnectionPanel();
				}
			}
			else
			{
				Logger.LogWarning("User is not logged in! Returning to login screen.");
				LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
			}
			gameObject.SetActive(false);
		}

		public void OnCancelBtn()
		{
			currentCharacter = lastSettings;
			RefreshAll();
			LobbyManager.Instance.lobbyDialogue.gameObject.SetActive(true);
			if (ServerData.Auth.CurrentUser != null)
			{
				if (onCloseAction != null)
				{
					onCloseAction.Invoke();
					onCloseAction = null;
				}
				else
				{
					LobbyManager.Instance.lobbyDialogue.ShowConnectionPanel();
				}
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
			int gender = (int)currentCharacter.Gender;
			gender++;
			if (gender == (int)Gender.Neuter)
			{
				gender = 0;
			}
			currentCharacter.Gender = (Gender)gender;

			// Repopulate underwear and facial hair dropdown boxes
			if (playerCustomisationData.ContainsKey(PlayerCustomisation.FacialHair))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.FacialHair], facialHairDropdown); }

			if (playerCustomisationData.ContainsKey(PlayerCustomisation.Underwear))
			{ PopulateDropdown(playerCustomisationData[PlayerCustomisation.Underwear], underwearDropdown); }

			// Set underwear and facial hair to default setting (None)
			SetDropdownValue(underwearDropdown, "None");
			SetDropdownValue(facialHairDropdown, "None");

			RefreshGender();
		}

		private void RefreshGender()
		{
			genderText.text = currentCharacter.Gender.ToString();
			currentCharacter.RefreshGenderBodyParts();


			if (currentCharacter.Gender == Gender.Female)
			{
				headSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Female.Head);
				torsoSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Female.Torso);
			}
			else
			{
				headSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Male.Head);
				torsoSpriteController.sprites = SpriteFunctions.CompleteSpriteSetup(playerTextureData.Male.Torso);
			}

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
			currentCharacter.LoadHairSetting(hairDropdown.options[index].text);
			RefreshHair();
		}

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
			if (playerCustomisationData[PlayerCustomisation.HairStyle].ContainsKey(currentCharacter.hairStyleName))
			{
				hairSpriteController.sprites =
										SpriteFunctions.CompleteSpriteSetup(playerCustomisationData[PlayerCustomisation.HairStyle][currentCharacter.hairStyleName].Equipped);
			}
			else
			{
				hairSpriteController.sprites = null;
			}
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

			currentCharacter.LoadFacialHairSetting(facialHairDropdown.options[index].text);
			RefreshFacialHair();
		}

		private void RefreshFacialHair()
		{
			if (playerCustomisationData[PlayerCustomisation.FacialHair].ContainsKey(currentCharacter.facialHairName))
			{
				facialHairSpriteController.sprites =
				SpriteFunctions.CompleteSpriteSetup(playerCustomisationData[PlayerCustomisation.FacialHair][currentCharacter.facialHairName].Equipped);
			}
			else { facialHairSpriteController.sprites = null; }
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
			currentCharacter.LoadUnderwearSetting(underwearDropdown.options[index].text);
			RefreshUnderwear();
		}
		private void RefreshUnderwear()
		{
			if (playerCustomisationData[PlayerCustomisation.Underwear].ContainsKey(currentCharacter.underwearName)){
				underwearSpriteController.sprites =
				SpriteFunctions.CompleteSpriteSetup(playerCustomisationData[PlayerCustomisation.Underwear][currentCharacter.underwearName].Equipped);}
			else
			{underwearSpriteController.sprites = null;}

			underwearSpriteController.UpdateSprite();
		}

		//------------------
		//SOCKS:
		//------------------

		public void SocksDropdownChange(int index)
		{
			currentCharacter.LoadSocksSetting(socksDropdown.options[index].text);
			RefreshSocks();
		}
		private void RefreshSocks()
		{

			if (playerCustomisationData[PlayerCustomisation.Socks].ContainsKey(currentCharacter.socksName)){
				socksSpriteController.sprites =
				SpriteFunctions.CompleteSpriteSetup(playerCustomisationData[PlayerCustomisation.Socks][currentCharacter.socksName].Equipped);}

			else { socksSpriteController.sprites = null; }
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
			colorChangedEvent = _colorChangeEvent;
			colorPicker.CurrentColor = currentColor;
			colorPicker.gameObject.SetActive(true);
		}

		private Action<Color> colorChangedEvent;
		private void OnColorChange(Color newColor)
		{
			colorChangedEvent.Invoke(newColor);
		}
	}
}

[Serializable]
public class CharacterSettings
{							//stuff  Marked with # is  no longer used and can be removed
	public const int MAX_NAME_LENGTH = 28; //Arbitrary limit, but it seems reasonable
	public string username;
	public string Name = "Cuban Pete";
	public Gender Gender = Gender.Male;
	public int Age = 22;
	public int hairStyleOffset = -1;  //#
	public string hairStyleName = "None";
	public int hairCollectionIndex = 4; //#
	public string hairColor = "black";
	public string eyeColor = "black";
	public int facialHairOffset = -1; //#
	public string facialHairName = "None";
	public int facialHairCollectionIndex = 4; //#
	public string facialHairColor = "black";
	public string skinTone = "#ffe0d1";
	public int underwearOffset = 20; //#
	public string underwearName = "Mankini";
	public int underwearCollectionIndex = 1; //#
	public int socksOffset = 376; //#
	public string socksName = "Knee-High (Freedom)";
	public int socksCollectionIndex = 3; //#

	int maleHeadIndex = 20; //#
	int femaleHeadIndex = 24; //#
	int maleTorsoIndex = 28; //#
	int femaleTorsoIndex = 32; //#

	public int headSpriteIndex = 20; //#
	public int torsoSpriteIndex = 28; //#
	public int rightLegSpriteIndex = 12; //#
	public int leftLegSpriteIndex = 16; //#
	public int rightArmSpriteIndex = 4; //#
	public int leftArmSpriteIndex = 8; //#
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

public enum Gender
{
	Male,
	Female,
	Neuter //adding anymore genders will break things do not edit
}