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

		private CharacterSettings currentCharacter;

		void OnEnable()
		{
			LoadSettings();
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
				currentCharacter.Name = StringManager.GetRandomMaleName();
			}

			RefreshAll();
		}

		private void RefreshAll()
		{
			RefreshName();
			RefreshGender();
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
		//FACIAL HAIR:
		//------------------

		//------------------
		//SKIN TONE:
		//------------------

		//------------------
		//UNDERWEAR:
		//------------------

		//------------------
		//SOCKS:
		//------------------
	}
}

[Serializable]
public class CharacterSettings
{
	public string Name;
	public Gender Gender;
}

public enum Gender
{
	Male,
	Female,
	Neuter //adding anymore genders will break things do not edit
}