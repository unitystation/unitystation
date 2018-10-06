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

		[SerializeField]
		public List<string> availableHairColors = new List<string>();
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
		}

		public void RollRandomCharacter()
		{
			currentCharacter.Name = StringManager.GetRandomMaleName();
			currentCharacter.LoadHairSetting(SpriteManager.HairCollection[
				UnityEngine.Random.Range(0, SpriteManager.HairCollection.Count - 1)]);
			currentCharacter.LoadFacialHairSetting(SpriteManager.FacialHairCollection[
				UnityEngine.Random.Range(0, SpriteManager.FacialHairCollection.Count - 1)]);
			currentCharacter.LoadUnderwearSetting(SpriteManager.UnderwearCollection[
				UnityEngine.Random.Range(0, SpriteManager.UnderwearCollection.Count - 1)]);
			currentCharacter.LoadSocksSetting(SpriteManager.SocksCollection[
				UnityEngine.Random.Range(0, SpriteManager.SocksCollection.Count - 1)]);

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
			currentCharacter.Name = StringManager.GetRandomMaleName();
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
		}

		//------------------
		//AGE:
		//------------------

		//------------------
		//EYE COLOR:
		//------------------

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

		public void HairColorScrollRight()
		{
			int tryNext = availableHairColors.IndexOf(currentCharacter.hairColor);
			tryNext++;
			if (tryNext == availableHairColors.Count)
			{
				tryNext = 0;
			}
			currentCharacter.hairColor = availableHairColors[tryNext];
			RefreshHair();
		}

		public void HairColorScrollLeft()
		{
			int tryNext = availableHairColors.IndexOf(currentCharacter.hairColor);
			tryNext--;
			if (tryNext < 0)
			{
				tryNext = availableHairColors.Count - 1;
			}
			currentCharacter.hairColor = availableHairColors[tryNext];
			RefreshHair();
		}

		public void OpenHairColorPicker()
		{
			if (!colorPicker.gameObject.activeInHierarchy)
			{
				OpenColorPicker(hairColor.color, HairColorChange);
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

		private void RefreshFacialHair()
		{
			facialHairSpriteController.reference = currentCharacter.facialHairOffset;
			facialHairSpriteController.UpdateSprite();
			facialHairText.text = currentCharacter.facialHairName;
		}

		//------------------
		//SKIN TONE:
		//------------------

		//------------------
		//UNDERWEAR:
		//------------------

		private void RefreshUnderwear()
		{
			underwearSpriteController.reference = currentCharacter.underwearOffset;
			underwearSpriteController.UpdateSprite();
			underwearText.text = currentCharacter.underwearName;
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

		//------------------
		//COLOR SELECTOR:
		//------------------

		private void OpenColorPicker(Color currentColor, Action<Color> _colorChangeEvent)
		{
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
	public Gender Gender;
	public int Age = 22;
	public int hairStyleOffset;
	public string hairStyleName;
	public int hairCollectionIndex;
	public string hairColor = "black";
	public string eyeColor;
	public int facialHairOffset;
	public string facialHairName;
	public int facialHairCollectionIndex;
	public string facialHairColor;
	public string skinTone;
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