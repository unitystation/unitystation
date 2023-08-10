using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Core.Utils;
using Initialisation;
using Systems.Character;
using UI.CharacterCreator;

namespace UI.Character
{
	/// <summary>
	/// Scripting associated with the character selector pane of the character settings window.
	/// </summary>
	public class CharacterSelector : MonoBehaviour
	{
		#region Inspector fields

		[SerializeField] private CharacterSettings characterSettingsWindow;
		[SerializeField] private CharacterCustomization characterCustomization;
		[SerializeField] private TMPro.TMP_Dropdown characterPreviewDropdown;
		[SerializeField] private Text characterPreviewRace;
		[SerializeField] private Text characterPreviewBodyType;
		[SerializeField] private GameObject characterPreviews;
		[SerializeField] private GameObject noCharactersError;
		[SerializeField] private GameObject confirmDeleteCharacterObject;

		[Header("Buttons")]
		[SerializeField] private Button nextCharacterBtn;
		[SerializeField] private Button previousCharacterBtn;
		[SerializeField] private Button createCharacterBtn;
		[SerializeField] private Button editCharacterBtn;
		[SerializeField] private Button deleteCharacterBtn;
		[SerializeField] private Button selectCharacterBtn;
		[SerializeField] private Button cancelDeleteCharacterBtn;
		[SerializeField] private Button confirmDeleteCharacterBtn;

		#endregion

		private CharacterManager CharacterManager => PlayerManager.CharacterManager;
		private CharacterSheet PreviewedCharacter => CharacterManager.Get(previewedCharacterKey);

		private int previewedCharacterKey = -1;

		private void OnEnable()
		{
			nextCharacterBtn.onClick.AddListener(OnNextCharacterBtn);
			previousCharacterBtn.onClick.AddListener(OnPreviousCharacterBtn);
			createCharacterBtn.onClick.AddListener(OnCreateCharacterBtn);
			editCharacterBtn.onClick.AddListener(OnEditCharacterBtn);
			deleteCharacterBtn.onClick.AddListener(OnDeleteCharacterBtn);
			selectCharacterBtn.onClick.AddListener(OnSelectCharacterBtn);
			cancelDeleteCharacterBtn.onClick.AddListener(OnCancelDeleteCharacterBtn);
			confirmDeleteCharacterBtn.onClick.AddListener(OnConfirmDeleteCharacterBtn);

			characterSettingsWindow.SetWindowTitle("Select your character");

			HideCharacterDeletionConfirmation();
			UpdateCharactersDropDown();

			// Only set the key if not already set, in case we came from the character editor
			// (we'd like to preview the character we just edited, even if it is not selected as the active character yet)
			if (previewedCharacterKey == -1)
			{
				previewedCharacterKey = CharacterManager.ActiveCharacterKey;
			}

			if (TryShowOptions())
			{
				LoadManager.RegisterActionDelayed(() => { PreviewCharacterByIndex(previewedCharacterKey); }, 10);
			}
		}

		private void OnDisable()
		{
			characterPreviewDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
			nextCharacterBtn.onClick.RemoveListener(OnNextCharacterBtn);
			previousCharacterBtn.onClick.RemoveListener(OnPreviousCharacterBtn);
			createCharacterBtn.onClick.RemoveListener(OnCreateCharacterBtn);
			editCharacterBtn.onClick.RemoveListener(OnEditCharacterBtn);
			deleteCharacterBtn.onClick.RemoveListener(OnDeleteCharacterBtn);
			selectCharacterBtn.onClick.RemoveListener(OnSelectCharacterBtn);
			cancelDeleteCharacterBtn.onClick.RemoveListener(OnCancelDeleteCharacterBtn);
			confirmDeleteCharacterBtn.onClick.RemoveListener(OnConfirmDeleteCharacterBtn);
		}

		private void UpdateCharactersDropDown()
		{
			characterPreviewDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
			characterPreviewDropdown.ClearOptions();
			var itemOptions = CharacterManager.Characters.Select(pcd => pcd.Name).ToList();
			characterPreviewDropdown.AddOptions(itemOptions);
			characterPreviewDropdown.onValueChanged.AddListener(OnDropdownChanged);
		}

		private bool TryShowOptions()
		{
			bool hasCharacters = CharacterManager.Characters.Count > 0;

			characterPreviews.SetActive(hasCharacters);
			selectCharacterBtn.SetActive(hasCharacters);
			editCharacterBtn.SetActive(hasCharacters);
			deleteCharacterBtn.SetActive(CharacterManager.Characters.Count > 1);

			noCharactersError.SetActive(hasCharacters == false);

			return hasCharacters;
		}

		private void PreviewCharacterByIndex(int index)
		{
			// Map a circular index (-1 => end, length + 1 => start)
			previewedCharacterKey = MathUtils.Mod(index, CharacterManager.Characters.Count);

			characterPreviewDropdown.value = previewedCharacterKey;
			characterPreviewRace.text = PreviewedCharacter.Species;
			characterPreviewBodyType.text = PreviewedCharacter.BodyType.ToString();

			// characterCustomization also manages the character sprite previewer, for now.
			characterCustomization.LoadCharacter(PreviewedCharacter);
		}

		private void CreateCharacter()
		{
			var character = CharacterSheet.GenerateRandomCharacter();
			CharacterManager.Add(character);
			CharacterManager.SaveCharacters();

			UpdateCharactersDropDown();

			if (TryShowOptions())
			{
				PreviewCharacterByIndex(CharacterManager.Characters.Count - 1);
			}
		}

		private void DeletePreviewedCharacter()
		{
			CharacterManager.Remove(previewedCharacterKey);
			CharacterManager.SaveCharacters();

			previewedCharacterKey -= 1;

			UpdateCharactersDropDown();

			if (TryShowOptions())
			{
				PreviewCharacterByIndex(previewedCharacterKey);
			}
		}

		private void ShowCharacterDeletionConfirmation()
		{
			deleteCharacterBtn.SetActive(false);
			confirmDeleteCharacterObject.SetActive(true);
		}

		private void HideCharacterDeletionConfirmation()
		{
			deleteCharacterBtn.SetActive(true);
			confirmDeleteCharacterObject.SetActive(false);
		}

		#region UI Event Handlers

		private void OnDropdownChanged(int newValue)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			PreviewCharacterByIndex(newValue);
		}

		private void OnNextCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			PreviewCharacterByIndex(previewedCharacterKey + 1);
		}

		private void OnPreviousCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			PreviewCharacterByIndex(previewedCharacterKey - 1);
		}

		private void OnCreateCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			CreateCharacter();
		}

		private void OnEditCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			characterSettingsWindow.EditCharacter(previewedCharacterKey);
		}

		private void OnDeleteCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			ShowCharacterDeletionConfirmation();
		}

		private void OnSelectCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			CharacterManager.SetActiveCharacter(previewedCharacterKey);
			CharacterManager.SetLastCharacterKey(previewedCharacterKey);
			characterSettingsWindow.SetActive(false);
		}

		private void OnCancelDeleteCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			HideCharacterDeletionConfirmation();
		}

		private void OnConfirmDeleteCharacterBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			HideCharacterDeletionConfirmation();
			DeletePreviewedCharacter();
		}

		#endregion
	}
}
