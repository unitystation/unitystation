using System;
using System.Collections.Generic;
using System.Linq;
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
		public Text clothingText;
		public Text backpackText;
		public Text accentText;
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

		[SerializeField] public List<string> availableSkinColors = new List<string>();
		private CharacterSettings currentCharacter;

		public ColorPicker colorPicker;

		private CharacterSettings lastSettings;

		public Action onCloseAction;

		public SpriteDataSO BobTheEmptySprite;

		void OnEnable()
		{
			LoadSettings();
			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.AddListener(OnColorChange);
			var copyStr = JsonUtility.ToJson(currentCharacter);
			lastSettings = JsonUtility.FromJson<CharacterSettings>(copyStr);
			DisplayErrorText("");

			//torsoSpriteController.sprites.SetSpriteSO(playerTextureData.Base.Torso);
			//headSpriteController.sprites.SetSpriteSO(playerTextureData.Base.Head);
			RarmSpriteController.sprites.SetSpriteSO(playerTextureData.Base.ArmRight);
			LarmSpriteController.sprites.SetSpriteSO(playerTextureData.Base.ArmLeft);
			RlegSpriteController.sprites.SetSpriteSO(playerTextureData.Base.LegRight);
			LlegSpriteController.sprites.SetSpriteSO(playerTextureData.Base.LegLeft);
			RHandSpriteController.sprites.SetSpriteSO(playerTextureData.Base.HandRight);
			LHandSpriteController.sprites.SetSpriteSO(playerTextureData.Base.HandLeft);
			eyesSpriteController.sprites.SetSpriteSO(playerTextureData.Base.Eyes);
		}

		void OnDisable()
		{
			colorPicker.onValueChanged.RemoveListener(OnColorChange);
			if (onCloseAction != null)
			{
				onCloseAction.Invoke();
				onCloseAction = null;
			}
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
			if (string.IsNullOrEmpty(currentCharacter.Username))
			{
				currentCharacter.Username = ServerData.Auth.CurrentUser.DisplayName;
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
			RefreshClothing();
			RefreshBackpack();
			RefreshHair();
			RefreshFacialHair();
			RefreshUnderwear();
			RefreshSocks();
			RefreshEyes();
			RefreshSkinColor();
			RefreshAge();
			RefreshAccent();
		}

		public void RollRandomCharacter()
		{
			// Randomise gender
			var changeGender = (UnityEngine.Random.Range(0, 2) == 0);
			if (changeGender)
			{
				OnGenderChange();
			}

			// Select a random value from each dropdown
			hairDropdown.value = UnityEngine.Random.Range(0, hairDropdown.options.Count - 1);
			facialHairDropdown.value = UnityEngine.Random.Range(0, facialHairDropdown.options.Count - 1);
			underwearDropdown.value = UnityEngine.Random.Range(0, underwearDropdown.options.Count - 1);
			socksDropdown.value = UnityEngine.Random.Range(0, socksDropdown.options.Count - 1);

			// Do gender specific randomisation
			if (currentCharacter.Gender == Gender.Male)
			{
				currentCharacter.Name = StringManager.GetRandomMaleName();
				currentCharacter.FacialHairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			}
			else
			{
				currentCharacter.Name = StringManager.GetRandomFemaleName();
			}

			// Randomise rest of data
			currentCharacter.EyeColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			currentCharacter.HairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			currentCharacter.SkinTone = availableSkinColors[UnityEngine.Random.Range(0, availableSkinColors.Count - 1)];
			currentCharacter.Age = UnityEngine.Random.Range(19, 78);

			RefreshAll();
		}

		//------------------
		//DROPDOWN BOXES:
		//------------------
		private void PopulateAllDropdowns()
		{
			PopulateDropdown(CustomisationType.HairStyle, hairDropdown);
			PopulateDropdown(CustomisationType.FacialHair, facialHairDropdown);
			PopulateDropdown(CustomisationType.Underwear, underwearDropdown);
			PopulateDropdown(CustomisationType.Socks, socksDropdown);
		}

		private void PopulateDropdown(CustomisationType type, Dropdown itemDropdown)
		{
			// Clear out old options
			itemDropdown.ClearOptions();

			// Make a list of all available options which can then be passed to the dropdown box
			var itemOptions = PlayerCustomisationDataSOs.Instance.GetAll(type, currentCharacter.Gender)
				.Select(pcd => pcd.Name).ToList();
			itemOptions.Sort();

			// Ensure "None" is at the top of the option lists
			itemOptions.Insert(0, "None");
			itemDropdown.AddOptions(itemOptions);
		}

		private void SetAllDropdowns()
		{
			SetDropdownValue(hairDropdown, currentCharacter.HairStyleName);
			SetDropdownValue(facialHairDropdown, currentCharacter.FacialHairName);
			SetDropdownValue(underwearDropdown, currentCharacter.UnderwearName);
			SetDropdownValue(socksDropdown, currentCharacter.SocksName);
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
				Logger.LogWarning($"Unable to find index of {currentSetting}! Using default", Category.Character);
				itemDropdown.value = 0;
				// Needs to be called manually since value is probably already 0, so onValueChanged might not be invoked
				itemDropdown.onValueChanged.Invoke(0);
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
			ServerData.UpdateCharacterProfile(
				currentCharacter); // TODO Consider adding await. Otherwise this causes a compile warning.
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
				Logger.LogFormat("Invalid character settings: {0}", Category.Character, e.Message);
				DisplayErrorText(e.Message);
				return;
			}

			SaveData();
			gameObject.SetActive(false);
		}

		public void OnCancelBtn()
		{
			PlayerManager.CurrentCharacterSettings = lastSettings;
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
			PopulateDropdown(CustomisationType.FacialHair, facialHairDropdown);
			PopulateDropdown(CustomisationType.Underwear, underwearDropdown);

			// Set underwear and facial hair to default setting (None)
			FacialHairDropdownChange(0);
			UnderwearDropdownChange(0);

			RefreshGender();
		}

		private void RefreshGender()
		{
			genderText.text = currentCharacter.Gender.ToString();
			if (currentCharacter.Gender == Gender.Female)
			{
				headSpriteController.sprites.SetSpriteSO(playerTextureData.Female.Head);
				torsoSpriteController.sprites.SetSpriteSO(playerTextureData.Female.Torso);
			}
			else
			{
				headSpriteController.sprites.SetSpriteSO(playerTextureData.Male.Head);
				torsoSpriteController.sprites.SetSpriteSO(playerTextureData.Male.Torso);
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
			int.TryParse(ageField.text, out int tryInt);
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
				OpenColorPicker(eyeColor.color, EyeColorChange, 182f);
			}
		}

		private void EyeColorChange(Color newColor)
		{
			currentCharacter.EyeColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
			RefreshEyes();
		}

		private void RefreshEyes()
		{
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.EyeColor, out setColor);
			eyesSpriteController.sprites.SetColor(setColor);
			eyeColor.color = setColor;
		}

		//------------------
		//HAIR:
		//------------------

		public void HairDropdownChange(int index)
		{
			currentCharacter.HairStyleName = hairDropdown.options[index].text;
			RefreshHair();
		}

		public void OpenHairColorPicker()
		{
			if (!colorPicker.gameObject.activeInHierarchy)
			{
				OpenColorPicker(hairColor.color, HairColorChange, 276f);
			}
		}

		private void HairColorChange(Color newColor)
		{
			hairColor.color = newColor;
			currentCharacter.HairColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
			RefreshHair();
		}

		private void RefreshHair()
		{
			var pcd = PlayerCustomisationDataSOs.Instance.Get(
				CustomisationType.HairStyle,
				currentCharacter.Gender,
				currentCharacter.HairStyleName
			);
			if (pcd == null)
			{
				hairSpriteController.sprites.SetSpriteSO(pcd.SpriteEquipped);
			}
			else
			{
				hairSpriteController.sprites.SetSpriteSO(pcd.SpriteEquipped);
				hairSpriteController.UpdateSprite();
			}


			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.HairColor, out setColor);
			hairSpriteController.sprites.SetColor(setColor);
			hairColor.color = setColor;
		}

		//------------------
		//FACIAL HAIR:
		//------------------

		public void FacialHairDropdownChange(int index)
		{
			currentCharacter.FacialHairName = facialHairDropdown.options[index].text;
			RefreshFacialHair();
		}

		private void RefreshFacialHair()
		{
			var pcd = PlayerCustomisationDataSOs.Instance.Get(
				CustomisationType.FacialHair,
				currentCharacter.Gender,
				currentCharacter.FacialHairName
			);

			if (pcd == null)
			{
				facialHairSpriteController.sprites.SetSpriteSO(BobTheEmptySprite);
			}
			else
			{
				facialHairSpriteController.image.enabled = true;
				facialHairSpriteController.sprites.SetSpriteSO(pcd.SpriteEquipped);
				facialHairSpriteController.UpdateSprite();
			}


			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.FacialHairColor, out setColor);
			facialHairSpriteController.sprites.SetColor(setColor);
			facialColor.color = setColor;
		}

		public void OpenFacialHairPicker()
		{
			if (!colorPicker.gameObject.activeInHierarchy)
			{
				OpenColorPicker(facialColor.color, FacialHairColorChange, 64f);
			}
		}

		private void FacialHairColorChange(Color newColor)
		{
			currentCharacter.FacialHairColor = "#" + ColorUtility.ToHtmlStringRGB(newColor);
			RefreshFacialHair();
		}

		//------------------
		//SKIN TONE:
		//------------------
		private void RefreshSkinColor()
		{
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.SkinTone, out setColor);
			skinColor.color = setColor;
			for (int i = 0; i < skinControllers.Length; i++)
			{
				skinControllers[i].sprites.SetColor(setColor);
			}
		}

		public void SkinToneScrollRight()
		{
			int index = availableSkinColors.IndexOf(currentCharacter.SkinTone);
			index++;
			if (index == availableSkinColors.Count)
			{
				index = 0;
			}

			currentCharacter.SkinTone = availableSkinColors[index];
			RefreshSkinColor();
		}

		public void SkinToneScrollLeft()
		{
			int index = availableSkinColors.IndexOf(currentCharacter.SkinTone);
			index--;
			if (index < 0)
			{
				index = availableSkinColors.Count - 1;
			}

			currentCharacter.SkinTone = availableSkinColors[index];
			RefreshSkinColor();
		}

		//------------------
		//UNDERWEAR:
		//------------------

		public void UnderwearDropdownChange(int index)
		{
			currentCharacter.UnderwearName = underwearDropdown.options[index].text;
			RefreshUnderwear();
		}

		private void RefreshUnderwear()
		{
			var pcd = PlayerCustomisationDataSOs.Instance.Get(
				CustomisationType.Underwear,
				currentCharacter.Gender,
				currentCharacter.UnderwearName
			);
			if (pcd == null)
			{
				underwearSpriteController.sprites.SetSpriteSO(BobTheEmptySprite);
			}
			else
			{
				underwearSpriteController.image.enabled = true;
				underwearSpriteController.sprites.SetSpriteSO(pcd.SpriteEquipped);
				underwearSpriteController.UpdateSprite();
			}
		}

		//------------------
		//SOCKS:
		//------------------

		public void SocksDropdownChange(int index)
		{
			currentCharacter.SocksName = socksDropdown.options[index].text;
			RefreshSocks();
		}

		private void RefreshSocks()
		{
			var pcd = PlayerCustomisationDataSOs.Instance.Get(
				CustomisationType.Socks,
				currentCharacter.Gender,
				currentCharacter.SocksName
			);
			if (pcd == null)
			{
				socksSpriteController.sprites.SetSpriteSO(pcd.SpriteEquipped);
			}
			else
			{
				socksSpriteController.image.enabled = true;
				socksSpriteController.sprites.SetSpriteSO(pcd.SpriteEquipped);
				socksSpriteController.UpdateSprite();
			}
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

		//------------------
		//CLOTHING PREFERENCE:
		//------------------

		public void OnClothingChange()
		{
			int clothing = (int) currentCharacter.ClothingStyle;
			clothing++;
			if (clothing == (int) ClothingStyle.None)
			{
				clothing = 0;
			}

			currentCharacter.ClothingStyle = (ClothingStyle) clothing;
			RefreshClothing();
		}

		private void RefreshClothing()
		{
			clothingText.text = currentCharacter.ClothingStyle.ToString();
		}

		//------------------
		//BACKPACK PREFERENCE:
		//------------------

		public void OnBackpackChange()
		{
			int backpack = (int) currentCharacter.BagStyle;
			backpack++;
			if (backpack == (int) BagStyle.None)
			{
				backpack = 0;
			}

			currentCharacter.BagStyle = (BagStyle) backpack;
			RefreshBackpack();
		}

		private void RefreshBackpack()
		{
			backpackText.text = currentCharacter.BagStyle.ToString();
		}

		//------------------
		//ACCENT PREFERENCE:
		//------------------
		// This will be a temporal thing until we have proper character traits

		public void OnAccentChange()
		{
			int accent = (int) currentCharacter.Speech;
			accent++;
			if (accent == (int) Speech.Unintelligible)
			{
				accent = 0;
			}

			currentCharacter.Speech = (Speech) accent;
			RefreshAccent();
		}

		private void RefreshAccent()
		{
			accentText.text = currentCharacter.Speech.ToString();
		}
	}
}