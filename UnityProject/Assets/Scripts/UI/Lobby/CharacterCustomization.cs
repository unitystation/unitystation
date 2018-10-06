using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class CharacterCustomization : MonoBehaviour
	{
		public InputField characterNameField;
		public InputField ageField;
		public Text genderText;
		public Text hairStyleText;
		public Text facialHairText;
		public Text underwearText;
		public Text socksText;
		public Image hairColor;
		public Image eyeColor;
		public Image facialColor;
		public Image skinColor;

		public CharacterSprites hairSpriteController;
		public CharacterSprites facialHairSpriteController;
		public CharacterSprites underwearSpriteController;
		public CharacterSprites socksSpriteController;
		public CharacterSprites eyesSpriteController;
		public CharacterSprites[] skinControllers;

		[SerializeField]
		public List<string> availableHairColors = new List<string>();

		[SerializeField]
		public List<string> availableSkinColors = new List<string>();
		private CharacterSettings currentCharacter;

		public ColorPicker colorPicker;

		void OnEnable()
		{
			LoadSettings();
			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.AddListener(OnColorChange);
		}

		void OnDisable()
		{
			colorPicker.onValueChanged.RemoveListener(OnColorChange);
		}
		private async void LoadSettings()
		{
			await Task.Delay(2000);
			currentCharacter = PlayerManager.CurrentCharacterSettings;
			DoInitChecks();
		}

		//First time setting up this character etc?
		private void DoInitChecks()
		{
			if (string.IsNullOrEmpty(currentCharacter.Name))
			{
				RollRandomCharacter();
			}

			RefreshAll();
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
			if (currentCharacter.Gender == Gender.Male)
			{
				currentCharacter.Name = StringManager.GetRandomMaleName();
			}
			else
			{
				currentCharacter.Name = StringManager.GetRandomFemaleName();
			}
			currentCharacter.LoadHairSetting(SpriteManager.HairCollection[
				UnityEngine.Random.Range(0, SpriteManager.HairCollection.Count - 1)]);
			currentCharacter.LoadFacialHairSetting(SpriteManager.FacialHairCollection[
				SpriteManager.FacialHairCollection.Count - 1]);
			currentCharacter.LoadUnderwearSetting(SpriteManager.UnderwearCollection[
				UnityEngine.Random.Range(0, SpriteManager.UnderwearCollection.Count - 1)]);
			currentCharacter.LoadSocksSetting(SpriteManager.SocksCollection[
				UnityEngine.Random.Range(0, SpriteManager.SocksCollection.Count - 1)]);
			currentCharacter.skinTone = availableSkinColors[UnityEngine.Random.Range(0, availableSkinColors.Count - 1)];

			currentCharacter.Age = UnityEngine.Random.Range(19, 78);

			RefreshAll();
		}

		//------------------
		//NAME:
		//------------------
		private void RefreshName()
		{
			characterNameField.text = currentCharacter.Name;
		}

		public void RandomNameBtn()
		{
			if (currentCharacter.Gender == Gender.Male)
			{
				currentCharacter.Name = StringManager.GetRandomMaleName();
			}
			else
			{
				currentCharacter.Name = StringManager.GetRandomFemaleName();
			}
			RefreshName();
		}

		public void OnManualNameChange()
		{
			currentCharacter.Name = characterNameField.text;
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
			RefreshGender();
		}

		private void RefreshGender()
		{
			genderText.text = currentCharacter.Gender.ToString();
			DoGenderChecks();
		}

		private void DoGenderChecks()
		{
			//Check underwear:
			if(SpriteManager.UnderwearCollection[currentCharacter.underwearCollectionIndex].gender != currentCharacter.Gender){
				int indexSearch = currentCharacter.underwearCollectionIndex;
				while(SpriteManager.UnderwearCollection[indexSearch].gender != currentCharacter.Gender){
					indexSearch++;
					if(indexSearch == SpriteManager.UnderwearCollection.Count){
						indexSearch = 0;
					}
				}
				currentCharacter.LoadUnderwearSetting(SpriteManager.UnderwearCollection[indexSearch]);
				RefreshUnderwear();
			}
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
			currentCharacter.Age = tryInt;
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

		public void HairScrollRight()
		{
			int tryNext = currentCharacter.hairCollectionIndex + 1;
			if (tryNext >= SpriteManager.HairCollection.Count)
			{
				tryNext = 0;
			}
			currentCharacter.LoadHairSetting(SpriteManager.HairCollection[tryNext]);
			RefreshHair();
		}

		public void HairScrollLeft()
		{
			int tryNext = currentCharacter.hairCollectionIndex - 1;
			if (tryNext < 0)
			{
				tryNext = SpriteManager.HairCollection.Count - 1;
			}
			currentCharacter.LoadHairSetting(SpriteManager.HairCollection[tryNext]);
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
			hairStyleText.text = currentCharacter.hairStyleName;
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentCharacter.hairColor, out setColor);
			hairSpriteController.image.color = setColor;
			hairColor.color = setColor;
		}

		//------------------
		//FACIAL HAIR:
		//------------------

		public void FacialHairScrollRight()
		{
			int tryNext = currentCharacter.facialHairCollectionIndex + 1;
			if (tryNext >= SpriteManager.FacialHairCollection.Count)
			{
				tryNext = 0;
			}
			currentCharacter.LoadFacialHairSetting(SpriteManager.FacialHairCollection[tryNext]);
			RefreshFacialHair();
		}

		public void FacialHairScrollLeft()
		{
			int tryNext = currentCharacter.facialHairCollectionIndex - 1;
			if (tryNext < 0)
			{
				tryNext = SpriteManager.FacialHairCollection.Count - 1;
			}
			currentCharacter.LoadFacialHairSetting(SpriteManager.FacialHairCollection[tryNext]);
			RefreshFacialHair();
		}
		private void RefreshFacialHair()
		{
			facialHairSpriteController.reference = currentCharacter.facialHairOffset;
			facialHairSpriteController.UpdateSprite();
			facialHairText.text = currentCharacter.facialHairName;
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

		private void RefreshUnderwear()
		{
			underwearSpriteController.reference = currentCharacter.underwearOffset;
			underwearSpriteController.UpdateSprite();
			underwearText.text = currentCharacter.underwearName;
		}

		public void UnderwearScrollRight()
		{
			int index = currentCharacter.underwearCollectionIndex + 1;
			if (index == SpriteManager.UnderwearCollection.Count)
			{
				index = 0;
			}
			currentCharacter.LoadUnderwearSetting(SpriteManager.UnderwearCollection[index]);
			DoGenderChecks();
			RefreshUnderwear();
		}

		public void UnderwearScrollLeft()
		{
			int index = currentCharacter.underwearCollectionIndex - 1;
			if (index < 0)
			{
				index = SpriteManager.UnderwearCollection.Count - 1;
			}
			currentCharacter.LoadUnderwearSetting(SpriteManager.UnderwearCollection[index]);
			DoGenderChecks();
			RefreshUnderwear();
		}

		//------------------
		//SOCKS:
		//------------------

		private void RefreshSocks()
		{
			socksSpriteController.reference = currentCharacter.socksOffset;
			socksSpriteController.UpdateSprite();
			socksText.text = currentCharacter.socksName;
		}

		public void SocksScrollRight()
		{
			int index = currentCharacter.socksCollectionIndex + 1;
			if (index == SpriteManager.SocksCollection.Count)
			{
				index = 0;
			}
			currentCharacter.LoadSocksSetting(SpriteManager.SocksCollection[index]);
			RefreshSocks();
		}

		public void SocksScrollLeft()
		{
			int index = currentCharacter.socksCollectionIndex - 1;
			if (index < 0)
			{
				index = SpriteManager.SocksCollection.Count - 1;
			}
			currentCharacter.LoadSocksSetting(SpriteManager.SocksCollection[index]);
			RefreshSocks();
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
	public string Name;
	public Gender Gender = Gender.Male;
	public int Age = 22;
	public int hairStyleOffset;
	public string hairStyleName;
	public int hairCollectionIndex;
	public string hairColor = "black";
	public string eyeColor = "black";
	public int facialHairOffset;
	public string facialHairName;
	public int facialHairCollectionIndex;
	public string facialHairColor;
	public string skinTone = "#ffe0d1";
	public int underwearOffset;
	public string underwearName;
	public int underwearCollectionIndex;
	public int socksOffset;
	public string socksName;
	public int socksCollectionIndex;

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
}

public enum Gender
{
	Male,
	Female,
	Neuter //adding anymore genders will break things do not edit
}