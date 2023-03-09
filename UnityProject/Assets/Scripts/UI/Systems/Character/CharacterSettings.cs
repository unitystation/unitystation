using System;
using UnityEngine;
using UnityEngine.UI;
using Systems.Character;
using UI.CharacterCreator;

namespace UI.Character
{
	/// <summary>
	/// The root-level component on the character settings window.
	/// </summary>
	// TODO: this class also controls where the character sprite previewer object exists
	// (the character editor or character selector screens). Ideally character sprite previewer should be in its own component.
	public class CharacterSettings : MonoBehaviour
	{
		#region Inspector

		[SerializeField]
		private Text windowName;

		[SerializeField]
		private GameObject characterSelector;

		[SerializeField]
		private GameObject characterEditor;

		[SerializeField]
		private GameObject spriteContainer;

		[SerializeField]
		private GameObject characterCustomizationContent;

		[SerializeField]
		private GameObject characterSelectorPreviewContent;

		#endregion

		public CharacterSheet EditedCharacter { get; private set; }

		private CharacterManager CharacterManager => PlayerManager.CharacterManager;

		private CharacterCustomization characterEditorScript;

		private Vector3 spriteContainerOriginalPos;

		private int editedCharacterKey = -1;

		private void Awake()
		{
			characterEditorScript = characterEditor.GetComponent<CharacterCustomization>();
			ShowCharacterSelector();
		}

		public void SetWindowTitle(string title)
		{
			windowName.text = title;
		}

		public void ShowCharacterSelector()
		{
			GetOriginalLocalPositionForCharacterPreview();
			ShowCharacterPreviewOnCharacterSelector();
			characterEditor.SetActive(false);
			characterSelector.SetActive(true);
		}

		public void EditCharacter(int key)
		{
			editedCharacterKey = key;
			// Use a copy in case changes are discarded
			EditedCharacter = (CharacterSheet) CharacterManager.Get(key).Clone();
			ShowCharacterEditor(EditedCharacter);
		}

		public void ShowCharacterEditor(CharacterSheet character)
		{
			ReturnCharacterPreviewFromTheCharacterSelector();
			characterSelector.SetActive(false);
			characterEditor.SetActive(true);
			characterEditorScript.LoadCharacter(character);
		}

		public void SaveCharacter(CharacterSheet character)
		{
			CharacterManager.Set(editedCharacterKey, character);
			CharacterManager.SaveCharacters();
		}

		private void ShowCharacterPreviewOnCharacterSelector()
		{
			spriteContainer.transform.SetParent(characterSelectorPreviewContent.transform);
		}

		private void GetOriginalLocalPositionForCharacterPreview()
		{
			spriteContainerOriginalPos = spriteContainer.transform.localPosition;
		}

		private void ReturnCharacterPreviewFromTheCharacterSelector()
		{
			spriteContainer.transform.SetParent(characterCustomizationContent.transform, false);
			spriteContainer.transform.localPosition = spriteContainerOriginalPos;
		}
	}
}
