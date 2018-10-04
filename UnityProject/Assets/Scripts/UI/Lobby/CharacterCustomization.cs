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
		}

		private void RefreshName()
		{
			characterNameField.text = currentCharacter.Name;
		}

		public void RandomNameBtn()
		{
			currentCharacter.Name = StringManager.GetRandomMaleName();
			RefreshName();
		}
	}
}