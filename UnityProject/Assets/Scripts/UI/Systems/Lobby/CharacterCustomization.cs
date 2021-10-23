using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using HealthV2;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using NaughtyAttributes;
using System.Globalization;

namespace UI.CharacterCreator
{
	public class CharacterCustomization : MonoBehaviour
	{
		[Header("Character Customizer")]
		public GameObject SpriteContainer;

		public CustomisationSubPart customisationSubPart;

		public GameObject ScrollList;

		public GameObject ScrollListBody;

		public List<CustomisationSubPart> OpenCustomisation = new List<CustomisationSubPart>();

		public Dictionary<BodyPart, List<SpriteHandlerNorder>> OpenBodySprites =
			new Dictionary<BodyPart, List<SpriteHandlerNorder>>();

		public Dictionary<string, BodyPartCustomisationBase> OpenBodyCustomisation =
			new Dictionary<string, BodyPartCustomisationBase>();

		public Dictionary<BodyPart, List<BodyPart>> ParentDictionary = new Dictionary<BodyPart, List<BodyPart>>();

		public List<GameObject> RootCustomisations = new List<GameObject>();

		public List<BodyTypeName> AvailableBodyTypes = new List<BodyTypeName>();

		public int SelectedBodyType = 0;

		public BodyTypeName ThisBodyType => AvailableBodyTypes[SelectedBodyType];

		public SpriteHandlerNorder BodyPartSprite;

		public InputField characterNameField;
		public InputField characterAiNameField;
		public InputField ageField;
		public Text errorLabel;
		public Text genderText;
		public Text clothingText;
		public Text backpackText;
		public Text accentText;
		public Text raceText;
		public Text pronounText;

		public PlayerHealthData ThisSetRace = null;

		public CharacterSprites torsoSpriteController;
		public CharacterSprites headSpriteController;

		public PlayerTextureData playerTextureData;

		public CharacterDir currentDir;

		[SerializeField] private List<Color> availableSkinColors;
		private CharacterSettings currentCharacter;
		public CharacterSettings CurrentCharacter { get { return currentCharacter; } }

		public ColorPicker colorPicker;

		public System.Action onCloseAction;

		public BodyPartDropDownOrgans AdditionalOrgan;
		public BodyPartDropDownReplaceOrgan ReplacementOrgan;

		public int CurrentSurfaceInt = 0;

		public Color CurrentSurfaceColour = Color.white;

		/// <summary>
		/// Empty, blank sprite texture used for selecting null customizations
		/// (e.g. selecting or scrolling to "None" for hair, facial hair, underwear,
		/// or socks).
		/// </summary>
		public SpriteDataSO BobTheEmptySprite;

		public List<CustomisationStorage> bodyPartCustomisationStorage = new List<CustomisationStorage>();
		public List<ExternalCustomisation> ExternalCustomisationStorage = new List<ExternalCustomisation>();

		public List<SpriteHandlerNorder> SurfaceSprite = new List<SpriteHandlerNorder>();

		public int SelectedSpecies = 0;

		public InputField SerialiseData;

		[SerializeField] private GameObject CharacterCustomizationContent;
		[SerializeField] private GameObject CharacterSelectorPreviewContent;
		private Vector3 SpritesContainerOriginalPosition;

		[Header("Character Selector")]
		[SerializeField] private Text WindowName;

		[SerializeField] private TMPro.TMP_Dropdown CharacterPreviewDropdown;
		[SerializeField] private Text CharacterPreviewRace;
		[SerializeField] private Text CharacterPreviewBodyType;

		[SerializeField] private GameObject CharacterPreviews;
		[SerializeField] private GameObject NoCharactersError;
		[SerializeField] private GameObject ConfirmDeleteCharacterObject;
		[SerializeField] private GameObject DeleteCharacterButton;
		[SerializeField] private GameObject GoBackButton;
		[SerializeField] private Button EditCharacterButton;

		[SerializeField] private GameObject CharacterSelectorPage;
		[SerializeField] private GameObject CharacterCreatorPage;

		public List<CharacterSettings> PlayerCharacters = new List<CharacterSettings>();

		private CharacterSettings lastSettings;
		private int currentCharacterIndex = 0;

		private TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

		#region Lifecycle

		void Awake()
		{
			GetSavedCharacters();
		}

		void OnEnable()
		{
			GetOriginalLocalPositionForCharacterPreview();
			GetSavedCharacters();
			ShowCharacterPreviewOnCharacterSelector();
			CheckIfCharacterListIsEmpty();
			WindowName.text = "Select your character";
			LoadSettings(PlayerManager.CurrentCharacterSettings);
			var copyStr = JsonConvert.SerializeObject(currentCharacter);
			lastSettings = JsonConvert.DeserializeObject<CharacterSettings>(copyStr);
			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.AddListener(OnColorChange);
			DisplayErrorText("");
			RefreshSelectorData();
		}

		void OnDisable()
		{
			colorPicker.onValueChanged.RemoveListener(OnColorChange);
			if (onCloseAction != null)
			{
				onCloseAction.Invoke();
				onCloseAction = null;
			}

			Cleanup();
		}

		public void Cleanup()
		{
			foreach (var C in OpenCustomisation)
			{
				Destroy(C.spriteHandlerNorder.gameObject);
				Destroy(C.gameObject);
			}

			ParentDictionary.Clear();


			foreach (var OpenBodySprite in OpenBodySprites)
			{
				foreach (var BodySprite in OpenBodySprite.Value)
				{
					Destroy(BodySprite.gameObject);
				}
			}

			foreach (var C in OpenBodyCustomisation)
			{
				Destroy(C.Value.gameObject);
			}

			OpenBodySprites.Clear();
			OpenBodyCustomisation.Clear();
			OpenCustomisation.Clear();
			SurfaceSprite.Clear();
		}

		#endregion

		private void ShowNoCharacterError()
		{
			ReturnCharacterPreviewFromTheCharacterSelector();
			GoBackButton.SetActive(false);
			CharacterPreviews.SetActive(false);
			NoCharactersError.SetActive(true);
			EditCharacterButton.SetActive(false);
			ConfirmDeleteCharacterObject.SetActive(false);
			DeleteCharacterButton.SetActive(false);
		}

		private void ShowCharacterCreator()
		{
			WindowName.text = "Character Settings";
			CharacterSelectorPage.SetActive(false);
			CharacterCreatorPage.SetActive(true);
			GoBackButton.SetActive(false);
			ReturnCharacterPreviewFromTheCharacterSelector();
			Cleanup();
			LoadSettings(currentCharacter);
			RefreshAll();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		private void ShowCharacterSelectorPage()
		{
			WindowName.text = "Select your character";
			ShowCharacterPreviewOnCharacterSelector();
			GoBackButton.SetActive(true);
			CharacterSelectorPage.SetActive(true);
			CharacterCreatorPage.SetActive(false);
			CheckIfCharacterListIsEmpty();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void ShowCharacterDeletionConfirmation()
		{
			DeleteCharacterButton.SetActive(false);
			ConfirmDeleteCharacterObject.SetActive(true);
		}

		public void HideCharacterDeletionConfirmation()
		{
			DeleteCharacterButton.SetActive(true);
			ConfirmDeleteCharacterObject.SetActive(false);
		}

		public void CreateCharacter()
		{
			if (currentCharacter != null)
			{
				lastSettings = currentCharacter;
			}
			CharacterSettings character = new CharacterSettings();
			PlayerCharacters.Add(character);
			currentCharacterIndex = PlayerCharacters.Count() - 1;
			LoadSettings(PlayerCharacters[currentCharacterIndex]);
			currentCharacter.Species = Race.Human.ToString();
			ShowCharacterCreator();
			ReturnCharacterPreviewFromTheCharacterSelector();
			RefreshAll();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void EditCharacter()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LoadSettings(PlayerCharacters[currentCharacterIndex]);
			lastSettings = PlayerCharacters[currentCharacterIndex];
			ReturnCharacterPreviewFromTheCharacterSelector();
			ShowCharacterCreator();
			RefreshAll();
		}

		public void HandleExitButton()
		{
			gameObject.SetActive(false);
		}

		public void DeleteCurrentCharacter()
		{
			DeleteCharacterFromCharactersList(currentCharacterIndex);
			HideCharacterDeletionConfirmation();
		}

		/// <summary>
		/// Responsible for refreshing all data in the character selector page.
		/// </summary>
		private void RefreshSelectorData()
		{
			CharacterPreviewRace.text = PlayerCharacters[currentCharacterIndex].Species;
			CharacterPreviewBodyType.text = PlayerCharacters[currentCharacterIndex].BodyType.ToString();
		}

		private void UpdateCharactersDropDown()
		{
			CharacterPreviewDropdown.ClearOptions();
			var itemOptions = PlayerCharacters.Select(pcd => pcd.Name).ToList();
			CharacterPreviewDropdown.AddOptions(itemOptions);
			CharacterPreviewDropdown.onValueChanged.AddListener(ItemChange);
		}

		/// <summary>
		/// Whenever the player changes his character via the dropdown menu we make sure that currentCharacterIndex is set accordingly
		/// And then we make sure that the currentCharacter is also loaded in.
		/// Note : to unify the way loading character data is; we mainly use ItemChange now for everything to make bug trackign less and code better.
		/// </summary>
		private void ItemChange(int newValue)
		{
			currentCharacterIndex = newValue;
			LoadSettings(PlayerCharacters[currentCharacterIndex]);
			PlayerManager.CurrentCharacterSettings = PlayerCharacters[currentCharacterIndex];
			SaveLastCharacterIndex();
			RefreshSelectorData();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		private void CheckIfCharacterListIsEmpty()
		{
			if (PlayerCharacters.Count == 0)
			{
				EditCharacterButton.SetActive(false);
				ShowNoCharacterError();
			}
			else
			{
				EditCharacterButton.SetActive(true);
				GoBackButton.SetActive(true);
				HideCharacterDeletionConfirmation();
			}
		}

		public void ScrollSelectorLeft()
		{
			if (currentCharacterIndex != 0)
			{
				currentCharacterIndex--;
			}
			else
			{
				currentCharacterIndex = PlayerCharacters.Count();
			}
			CharacterPreviewDropdown.value = currentCharacterIndex;
			RefreshSelectorData();
			RefreshAll();
			SaveLastCharacterIndex();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		public void ScrollSelectorRight()
		{
			if (currentCharacterIndex == PlayerCharacters.Count() || currentCharacterIndex == PlayerCharacters.Count() - 1)
			{
				currentCharacterIndex = 0;
			}
			else
			{
				currentCharacterIndex++;
			}
			CharacterPreviewDropdown.value = currentCharacterIndex;
			RefreshSelectorData();
			RefreshAll();
			SaveLastCharacterIndex();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		private void GetOriginalLocalPositionForCharacterPreview()
		{
			SpritesContainerOriginalPosition = SpriteContainer.transform.localPosition;
		}
		private void ShowCharacterPreviewOnCharacterSelector()
		{
			SpriteContainer.transform.SetParent(CharacterSelectorPreviewContent.transform);
		}

		private void ReturnCharacterPreviewFromTheCharacterSelector()
		{
			SpriteContainer.transform.SetParent(CharacterCustomizationContent.transform , false);
			SpriteContainer.transform.localPosition = SpritesContainerOriginalPosition;
		}

		private void LoadSettings(CharacterSettings inCharacterSettings)
		{
			Cleanup();
			currentCharacter = inCharacterSettings;
			//If we are playing locally offline, init character settings if they're null
			if (currentCharacter == null)
			{
				currentCharacter = new CharacterSettings();
				PlayerManager.CurrentCharacterSettings = currentCharacter;
			}

			PlayerHealthData SetRace = null;
			foreach (var Race in RaceSOSingleton.Instance.Races)
			{
				if (Race.name == currentCharacter.Species)
				{
					SetRace = Race;
				}
			}

			if (SetRace == null)
			{
				SetRace = RaceSOSingleton.Instance.Races.First();
			}

			//SelectedSpecies
			SelectedSpecies = 0;
			foreach (var Species in RaceSOSingleton.Instance.Races)
			{
				if (Species == SetRace)
				{
					break;
				}

				SelectedSpecies++;
			}

			AvailableBodyTypes = SetRace.Base.bodyTypeSettings.AvailableBodyTypes;
			ThisSetRace = SetRace;

			availableSkinColors = SetRace.Base.SkinColours;
			currentCharacter.SkinTone = inCharacterSettings.SkinTone;
			PlayerManager.CurrentCharacterSettings = currentCharacter;
			SetUpSpeciesBody(SetRace);
			PopulateAllDropdowns(SetRace);
			DoInitChecks();
			RefreshAll();
		}

		#region BodyPartsSprites

		public void SetUpSpeciesBody(PlayerHealthData Race)
		{
			BasedBodyPart(Race.Base.Torso);
			BasedBodyPart(Race.Base.Head);
			BasedBodyPart(Race.Base.ArmLeft);
			BasedBodyPart(Race.Base.ArmRight);
			BasedBodyPart(Race.Base.LegLeft);
			BasedBodyPart(Race.Base.LegRight);
		}

		public void BasedBodyPart(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;

			if (GameObjectBody.Elements.Count == 0) return;

			foreach (var Organ in GameObjectBody.Elements)
			{
				var bodyPart = Organ.GetComponent<BodyPart>();
				SetUpBodyPart(bodyPart);
			}


			// var DownOrgans = GameObjectBody.GetComponent<RootBodyPartContainer>();
			// if (DownOrgans != null)
			// {
			// if (DownOrgans.OptionalOrgans.Count > 0)
			// {
			// var Option = Instantiate(AdditionalOrgan, ScrollListBody.transform);
			// Option.SetUp(this, DownOrgans, "");
			// OpenBodyCustomisation[GameObjectBody.name] = Option;
			// }
			// }
		}

		public void SetUpBodyPart(BodyPart bodyPart, bool addOrganReplacement = true)
		{
			//bodyPart.LimbSpriteData;

			//OpenBodyCustomisation[bodyPart.name] = new List<GameObject>();
			ParentDictionary[bodyPart] = new List<BodyPart>();

			// This spawns the eyes.
			SetupBodyPartsSprites(bodyPart);
			if (bodyPart.LobbyCustomisation != null)
			{
				var newSprite = Instantiate(bodyPart.LobbyCustomisation, ScrollListBody.transform);
				newSprite.SetUp(this, bodyPart, ""); // Update path
				OpenBodyCustomisation[bodyPart.name] = (newSprite);
			}

			if (bodyPart.OptionalOrgans.Count > 0)
			{
				var Option = Instantiate(AdditionalOrgan, ScrollListBody.transform);
				Option.SetUp(this, bodyPart, "");
				OpenBodyCustomisation[bodyPart.name] = (Option);
			}

			if (addOrganReplacement)
			{
				if (bodyPart.OptionalReplacementOrgan.Count > 0)
				{
					var Option = Instantiate(ReplacementOrgan, ScrollListBody.transform);
					Option.SetUp(this, bodyPart, "");
					OpenBodyCustomisation[bodyPart.name] = (Option);
				}
			}

			//Setup sprite//
			//OpenBodySprites
			if (bodyPart?.OrganStorage?.Populater?.DeprecatedContents != null)
			{
				foreach (var Organ in bodyPart.OrganStorage.Populater.DeprecatedContents)
				{
					var subBodyPart = Organ.GetComponent<BodyPart>();
					ParentDictionary[bodyPart].Add(subBodyPart);
					SetUpBodyPart(subBodyPart);
				}
			}
		}

		public void SetupBodyPartsSprites(BodyPart bodyPart)
		{
			OpenBodySprites[bodyPart] = new List<SpriteHandlerNorder>();
			var Sprites = bodyPart.GetBodyTypeSprites(ThisBodyType.bodyType); //Get the correct one


			if (Sprites != null)
			{
				if (Sprites?.Item1?.Orders == null || Sprites.Item1.Orders.Count == 0)
				{
					Logger.LogError("Rendering order not specified on " + bodyPart.name, Category.Character);
				}


				int i = 0;
				foreach (var SpriteData in Sprites.Item2)
				{
					var newSprite = Instantiate(BodyPartSprite, SpriteContainer.transform);
					newSprite.gameObject.transform.localPosition = Vector3.zero;
					newSprite.SetSpriteOrder(new SpriteOrder(Sprites.Item1), true);
					newSprite.SpriteOrder.Add(i);
					newSprite.SpriteHandler.SetSpriteSO(SpriteData);
					OpenBodySprites[bodyPart].Add(newSprite);
					i++;
				}

				//Checks if the body part is not an internal organ (I.e: head, arm, etc.)
				//If it's not an internal organ, add it to the sprite manager to display the character.
				if (bodyPart.IsSurface)
				{
					SurfaceSprite.AddRange(OpenBodySprites[bodyPart]);
				}
			}
		}

		public void RemoveBodyPart(BodyPart bodyPart, bool removeBodyCustomisation = true)
		{
			if (bodyPart.IsSurface)
			{
				foreach (var sp in OpenBodySprites[bodyPart])
				{
					SurfaceSprite.Remove(sp);
				}
			}

			foreach (var Keyv in ParentDictionary)
			{
				if (Keyv.Value.Contains(bodyPart))
				{
					Keyv.Value.Remove(bodyPart);
				}
			}

			if (OpenBodySprites.ContainsKey(bodyPart))
			{
				foreach (var Sprite in OpenBodySprites[bodyPart])
				{
					Destroy(Sprite.gameObject);
				}

				OpenBodySprites.Remove(bodyPart);
			}

			if (OpenBodyCustomisation.ContainsKey(bodyPart.name))
			{
				//removeBodyCustomisation

				if (removeBodyCustomisation == true &&
				    OpenBodyCustomisation[bodyPart.name].GetComponent<BodyPartDropDownReplaceOrgan>() == null)
				{
					Destroy(OpenBodyCustomisation[bodyPart.name]);
				}

				OpenBodyCustomisation.Remove(bodyPart.name);
			}

			if (ParentDictionary.ContainsKey(bodyPart))
			{
				foreach (var subBodyPart in ParentDictionary[bodyPart])
				{
					RemoveBodyPart(subBodyPart);
				}

				ParentDictionary.Remove(bodyPart);
			}
		}

		#endregion BodyPartsSprites

		// First time setting up this character etc?
		private void DoInitChecks()
		{
			if (string.IsNullOrEmpty(currentCharacter.Username))
			{
				currentCharacter.Username = ServerData.Auth.CurrentUser.DisplayName;
				RollRandomCharacter();
				SaveData();
			}

			SetAllDropdowns();
			RefreshAll();
		}

		private void RefreshAll()
		{
			if (ThisSetRace.Base.SkinColours.Count > 0)
			{
				ColorUtility.TryParseHtmlString(currentCharacter.SkinTone, out CurrentSurfaceColour);

				bool match = false;
				foreach (var skinColour in ThisSetRace.Base.SkinColours)
				{
					if (Math.Abs(skinColour.a - CurrentSurfaceColour.a) < 0.01f
					    && Math.Abs(skinColour.r - CurrentSurfaceColour.r) < 0.01f
					    && Math.Abs(skinColour.g - CurrentSurfaceColour.g) < 0.01f
					    && Math.Abs(skinColour.b - CurrentSurfaceColour.b) < 0.01f)
					{
						match = true;
					}
				}

				if (match == false)
				{
					CurrentSurfaceColour = ThisSetRace.Base.SkinColours[0];
				}
			}
			else
			{
				ColorUtility.TryParseHtmlString(currentCharacter.SkinTone, out CurrentSurfaceColour);
			}

			SkinColourChange(CurrentSurfaceColour);

			int i = 0;
			bool Containing = false;
			foreach (var bodyType in AvailableBodyTypes)
			{
				if (bodyType.bodyType == currentCharacter.BodyType)
				{
					Containing = true;
					SelectedBodyType = i;
					break;
				}

				i++;
			}

			if (Containing == false)
			{
				currentCharacter.BodyType = AvailableBodyTypes[0].bodyType;
				SelectedBodyType = 0;
			}

			RefreshAge();
			RefreshName();
			RefreshAccent();
			RefreshBodyType();
			RefreshBackpack();
			RefreshClothing();
			RefreshPronoun();
			RefreshRace();
		}

		public void RollRandomCharacter()
		{
			currentCharacter = CharacterSettings.RandomizeCharacterSettings();

			//Randomises player accents. (Italian, Scottish, etc)


			//Randomises character skin tones.
			randomizeSkinTones();

			//Randomises character clothes, cat ears, moth wings, etc.
			randomizeAppearance();

			//Refresh the player character's sheet so they can see their new changes.
			RefreshAll();
		}

		private void randomizeAppearance()
		{
			//Randomizes hair, tails, etc
			foreach(var custom in GetComponentsInChildren<BodyPartCustomisationBase>())
			{
				custom.RandomizeValues();
			}
			//Randomizes clothes
			foreach(var customSubPart in GetComponentsInChildren<CustomisationSubPart>())
			{
				customSubPart.RandomizeValues();
			}
		}

		private void randomizeSkinTones()
		{
			//Checks to see if the player's race has specfic skin tones that it can use and picks it from that list
			//If there are none, randomly generate a new skin tone for the player.
			if (availableSkinColors.Count != 0)
			{
				currentCharacter.SkinTone = "#" +
					ColorUtility.ToHtmlStringRGB(availableSkinColors[UnityEngine.Random.Range(0, availableSkinColors.Count - 1)]);
			}
			else
			{
				currentCharacter.SkinTone = "#" +
					ColorUtility.ToHtmlStringRGBA(new Color(UnityEngine.Random.Range(0.1f, 1f),
					UnityEngine.Random.Range(0.1f, 1f),
					UnityEngine.Random.Range(0.1f, 1f), 1f));
			}
		}

		#region Dropdown Boxes

		private void PopulateAllDropdowns(PlayerHealthData Race)
		{
			//Checks what customisation settings the player's race has avaliable
			//Then spawns customisation dropdowns for cloth and other stuff.
			foreach (var customisation in Race.Base.CustomisationSettings)
			{
				var Customisation = Instantiate(customisationSubPart, ScrollList.transform);
				OpenCustomisation.Add(Customisation);
				Customisation.Setup(customisation.CustomisationGroup, this,
					customisation.CustomisationGroup.SpriteOrder);
			}
		}

		public void LeftRotate()
		{
			int nextDir = (int) currentDir + 1;
			if (nextDir > 3)
			{
				nextDir = 0;
			}

			currentDir = (CharacterDir) nextDir;
			SetRotation();
		}

		public void RightRotate()
		{
			int nextDir = (int) currentDir - 1;
			if (nextDir < 0)
			{
				nextDir = 3;
			}

			currentDir = (CharacterDir) nextDir;
			SetRotation();
		}

		public void SetRotation()
		{
			int referenceOffset = 0;
			if (currentDir == CharacterDir.down)
			{
				referenceOffset = 0;
			}

			if (currentDir == CharacterDir.up)
			{
				referenceOffset = 1;
			}

			if (currentDir == CharacterDir.right)
			{
				referenceOffset = 2;
			}

			if (currentDir == CharacterDir.left)
			{
				referenceOffset = 3;
			}

			var Sprites = SpriteContainer.transform.GetComponentsInChildren<SpriteHandlerNorder>();

			var newSprites = Sprites.OrderByDescending(x => x.SpriteOrder.Orders[referenceOffset]).Reverse();

			int i = 0;
			foreach (var Sprite in newSprites)
			{
				Sprite.gameObject.transform.SetSiblingIndex(i);
				i++;
			}

			foreach (var Customisation in OpenCustomisation)
			{
				Customisation.SetRotation(referenceOffset);
			}

			foreach (var PartSprites in OpenBodySprites)
			{
				foreach (var PartSprite in PartSprites.Value)
				{
					PartSprite.ChangeSpriteVariant(referenceOffset);
				}
			}
		}

		private void SetAllDropdowns()
		{
			if (currentCharacter.SerialisedBodyPartCustom == null)
			{
				currentCharacter.SerialisedBodyPartCustom = new List<CustomisationStorage>();
			}

			if (currentCharacter.SerialisedExternalCustom == null)
			{
				currentCharacter.SerialisedExternalCustom = new List<ExternalCustomisation>();
			}

			bodyPartCustomisationStorage = new List<CustomisationStorage>(currentCharacter.SerialisedBodyPartCustom);
			ExternalCustomisationStorage = new List<ExternalCustomisation>(currentCharacter.SerialisedExternalCustom);
			if (bodyPartCustomisationStorage == null)
			{
				bodyPartCustomisationStorage = new List<CustomisationStorage>();
			}

			if (ExternalCustomisationStorage == null)
			{
				ExternalCustomisationStorage = new List<ExternalCustomisation>();
			}


			foreach (var Customisation in OpenCustomisation)
			{
				ExternalCustomisation MatchedCustomisation = null;
				foreach (var customisation in ExternalCustomisationStorage)
				{
					if (customisation.Key == Customisation.thisCustomisations.name)
					{
						MatchedCustomisation = customisation;
					}
				}

				if (MatchedCustomisation != null)
				{
					Customisation.SetDropdownValue(MatchedCustomisation.SerialisedValue);
				}
			}


			SetDropDownBody(ThisSetRace.Base.Torso);
			SetDropDownBody(ThisSetRace.Base.Head);
			SetDropDownBody(ThisSetRace.Base.ArmLeft);
			SetDropDownBody(ThisSetRace.Base.ArmRight);
			SetDropDownBody(ThisSetRace.Base.LegLeft);
			SetDropDownBody(ThisSetRace.Base.LegRight);
		}

		public void SetDropDownBody(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;
			if (GameObjectBody.Elements.Count == 0) return;


			foreach (var Organ in GameObjectBody.Elements)
			{
				var bodyPart = Organ.GetComponent<BodyPart>();
				SubSetBodyPart(bodyPart, "");
			}
		}

		public void SubSetBodyPart(BodyPart bodyPart, string path)
		{
			path = path + "/" + bodyPart.name;
			if (OpenBodyCustomisation.ContainsKey(bodyPart.name))
			{
				CustomisationStorage Customisation = null;
				foreach (var bodyPartCustomisation in bodyPartCustomisationStorage)
				{
					if (bodyPartCustomisation.path == path)
					{
						Customisation = bodyPartCustomisation;
					}
				}

				if (Customisation != null)
				{
					var TCustomisation = OpenBodyCustomisation[bodyPart.name];
					TCustomisation.Deserialise(Customisation.Data.Replace("@£", "\""));
				}
			}

			if (bodyPart?.OrganStorage?.Populater?.DeprecatedContents != null)
			{
				foreach (var Organ in bodyPart.OrganStorage.Populater.DeprecatedContents)
				{
					var subBodyPart = Organ.GetComponent<BodyPart>();
					SubSetBodyPart(subBodyPart, path);
				}
			}
		}

		#endregion

		#region Player Accounts

		[Button()]
		private void SaveData()
		{
			ExternalCustomisationStorage.Clear();
			bodyPartCustomisationStorage.Clear();
			SaveBodyPart(ThisSetRace.Base.Torso);
			SaveBodyPart(ThisSetRace.Base.Head);
			SaveBodyPart(ThisSetRace.Base.ArmLeft);
			SaveBodyPart(ThisSetRace.Base.ArmRight);
			SaveBodyPart(ThisSetRace.Base.LegLeft);
			SaveBodyPart(ThisSetRace.Base.LegRight);

			SaveExternalCustomisations();
			currentCharacter.SerialisedExternalCustom = new List<ExternalCustomisation>(ExternalCustomisationStorage);

			currentCharacter.SerialisedBodyPartCustom = new List<CustomisationStorage>(bodyPartCustomisationStorage);

			Logger.Log(JsonConvert.SerializeObject(bodyPartCustomisationStorage), Category.Character);
			Logger.Log(JsonConvert.SerializeObject(ExternalCustomisationStorage), Category.Character);

			PlayerManager.CurrentCharacterSettings = currentCharacter;
			_ = ServerData.UpdateCharacterProfile(currentCharacter);
			SaveCharacters();
		}


		/// <summary>
		/// Remembers what was the last character the player chose in the character selector screen.
		/// </summary>
		private void SaveLastCharacterIndex()
		{
			PlayerPrefs.SetInt("lastCharacter", currentCharacterIndex);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// Save all characters in a json file.
		/// </summary>
		private void SaveCharacters()
		{
			var settings = new JsonSerializerSettings
			{
				PreserveReferencesHandling = PreserveReferencesHandling.All,
				NullValueHandling = NullValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
				Formatting = Formatting.Indented
			};
			string json;
			string path = Application.persistentDataPath + "characters.json";
			if (PlayerCharacters.Count == 0)
			{
				json = "";
			}
			else
			{
				json = JsonConvert.SerializeObject(PlayerCharacters, settings);
			}
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			File.WriteAllText(path, json);
			SaveLastCharacterIndex(); //Remember the current character index, prevents a bug for newly created characters.
		}

		/// <summary>
		/// Get all characters that are saved in %APPDATA%/Locallow/unitystation/characters.json
		/// </summary>
		public void GetSavedCharacters()
		{
			PlayerCharacters.Clear(); //Clear all entries so we don't have duplicates when re-opening the character page.
			string path = Application.persistentDataPath + "characters.json";

			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				if(json == "")
				{
					ShowNoCharacterError();
					return;
				}
				CharacterPreviews.SetActive(true);
				NoCharactersError.SetActive(false);
				var characters = JsonConvert.DeserializeObject<List<CharacterSettings>>(json);

				foreach (var c in characters)
				{
					PlayerCharacters.Add(c);
				}
				currentCharacterIndex = PlayerPrefs.GetInt("lastCharacter", currentCharacterIndex);
				UpdateCharactersDropDown();
				CharacterPreviewDropdown.value = currentCharacterIndex;
				RefreshSelectorData();
			}
			else
			{
				ShowNoCharacterError();
			}
		}

		/// <summary>
		/// Makes sure that the player spawns with the correct character.
		/// This is mainly meant for spawning and ensuring that the player doesn't get the wrong character index.
		/// </summary>
		public void ValidateCurrentCharacter()
		{
			PlayerPrefs.GetInt("lastCharacter", currentCharacterIndex);
			currentCharacter = PlayerCharacters[currentCharacterIndex];
		}

		private void DeleteCharacterFromCharactersList(int index)
		{
			PlayerCharacters.Remove(PlayerCharacters[index]);
			currentCharacterIndex -= 1;
			MakeSureCurrentCharacterIndexIsntABadValue();
			if (PlayerCharacters.Count == 0)
			{
				CheckIfCharacterListIsEmpty();
				SaveCharacters();
			}
			else
			{
				UpdateCharactersDropDown();
				CharacterPreviewDropdown.value = currentCharacterIndex;
				HideCharacterDeletionConfirmation();
				SaveCharacters();
				SaveLastCharacterIndex();
				RefreshSelectorData();
				RefreshAll();
				_ = ServerData.UpdateCharacterProfile(currentCharacter);
			}
		}

		private void MakeSureCurrentCharacterIndexIsntABadValue()
		{
			if (currentCharacterIndex <= -1)
			{
				currentCharacterIndex = 0;
				UpdateCharactersDropDown();
				CharacterPreviewDropdown.value = currentCharacterIndex;
			}
		}

		public void SaveExternalCustomisations()
		{
			foreach (var Customisation in OpenCustomisation)
			{
				var newExternalCustomisation = new ExternalCustomisation();
				newExternalCustomisation.Key = Customisation.thisCustomisations.name;
				newExternalCustomisation.SerialisedValue = Customisation.Serialise();
				ExternalCustomisationStorage.Add(newExternalCustomisation);
			}
		}

		public void SaveBodyPart(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;
			if (GameObjectBody.Elements.Count == 0) return;


			foreach (var Organ in GameObjectBody.Elements)
			{
				var bodyPart = Organ.GetComponent<BodyPart>();
				SubSaveBodyPart(bodyPart, "");
			}
		}

		public void SaveCustomisations(CustomisationStorage CustomisationStorage,
			BodyPartCustomisationBase CustomisationObject)
		{
			var Customisations = CustomisationObject.GetComponent<BodyPartCustomisationBase>();

			CustomisationStorage.Data = Customisations.Serialise();
			CustomisationStorage.Data = CustomisationStorage.Data.Replace("\"", "@£");


			//CustomisationStorage
			//SavingDataStorage
		}

		public void SubSaveBodyPart(BodyPart bodyPart, string path)
		{
			path = path + "/" + bodyPart.name;
			if (OpenBodyCustomisation.ContainsKey(bodyPart.name))
			{
				var NewCustomisationStorage = new CustomisationStorage();
				NewCustomisationStorage.path = path;
				bodyPartCustomisationStorage.Add(NewCustomisationStorage);
				SaveCustomisations(NewCustomisationStorage, OpenBodyCustomisation[bodyPart.name]);
			}

			if (bodyPart?.OrganStorage?.Populater?.DeprecatedContents != null)
			{
				foreach (var Organ in bodyPart.OrganStorage.Populater.DeprecatedContents)
				{
					var subBodyPart = Organ.GetComponent<BodyPart>();
					SubSaveBodyPart(subBodyPart, path);
				}
			}
		}

		#endregion

		#region Save and Cancel Buttons

		public void OnApplyBtn()
		{
			OnApplyBtnLogic();
		}

		private void OnApplyBtnLogic()
		{

			DisplayErrorText("");
			try
			{
				currentCharacter.Name = TruncateName(currentCharacter.Name);
				currentCharacter.AiName = TruncateName(currentCharacter.AiName);
				currentCharacter.ValidateSettings();
			}
			catch (InvalidOperationException e)
			{
				Logger.LogFormat("Invalid character settings: {0}", Category.Character, e.Message);
				_ = SoundManager.Play(CommonSounds.Instance.AccessDenied);
				DisplayErrorText(e.Message);
				return;
			}

			PlayerCharacters[currentCharacterIndex] = currentCharacter;

			//Ensure that the character skin tone is assigned when saving the character
			string skintone = currentCharacter.SkinTone = "#" + ColorUtility.ToHtmlStringRGB(CurrentSurfaceColour);
			PlayerCharacters[currentCharacterIndex].SkinTone = skintone;

			SaveData();
			ShowCharacterSelectorPage();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			gameObject.SetActive(false);
		}

		public void OnCancelBtn()
		{
			PlayerManager.CurrentCharacterSettings = lastSettings;
			LoadSettings(lastSettings);
			RefreshAll();
			ReturnCharacterPreviewFromTheCharacterSelector();
			ShowCharacterSelectorPage();
		}

		#endregion

		private void DisplayErrorText(string message)
		{
			errorLabel.text = message;
		}

		#region Name

		private void RefreshName()
		{
			characterNameField.text = TruncateName(currentCharacter.Name);
			characterAiNameField.text = TruncateName(currentCharacter.AiName);
		}

		public void RandomNameBtn()
		{
			switch (currentCharacter.BodyType)
			{
				case BodyType.Male:
					currentCharacter.Name = StringManager.GetRandomMaleName();
					break;
				case BodyType.Female:
					currentCharacter.Name = StringManager.GetRandomFemaleName();
					break;
				default:
					currentCharacter.Name = StringManager.GetRandomName(Gender.NonBinary);
					break;
			}

			RefreshName();
		}

		public void OnManualNameChange()
		{
			currentCharacter.Name = TruncateName(characterNameField.text);
			characterNameField.text = currentCharacter.Name;
		}

		public void OnManualAiNameChange()
		{
			currentCharacter.AiName = TruncateName(characterAiNameField.text);
			characterAiNameField.text = currentCharacter.AiName;
		}

		private string TruncateName(string proposedName)
		{
			proposedName = textInfo.ToTitleCase(proposedName.ToLower());
			if (proposedName.Length >= CharacterSettings.MAX_NAME_LENGTH)
			{
				return proposedName.Substring(0, CharacterSettings.MAX_NAME_LENGTH);
			}

			return proposedName;
		}

		#endregion

		#region Gender

		public void OnBodyTypeChange()
		{
			// ThisBodyType
			// AvailableBodyTypes
			SelectedBodyType++;
			if (SelectedBodyType >= AvailableBodyTypes.Count)
			{
				SelectedBodyType = 0;
			}

			SurfaceSprite.Clear();
			var Copy = new Dictionary<BodyPart, List<SpriteHandlerNorder>>(OpenBodySprites);
			foreach (var KVP in Copy)
			{
				foreach (var Sprite in KVP.Value)
				{
					Destroy(Sprite.gameObject);
				}

				SetupBodyPartsSprites(KVP.Key);
			}

			foreach (var BodyCustomisation in OpenBodyCustomisation)
			{
				BodyCustomisation.Value.Refresh();
			}

			foreach (var Customisation in OpenCustomisation)
			{
				Customisation.Refresh();
			}

			currentCharacter.BodyType = AvailableBodyTypes[SelectedBodyType].bodyType;
			SkinColourChange(CurrentSurfaceColour);
			SetRotation();
			RefreshBodyType();
		}

		private void RefreshBodyType()
		{
			genderText.text = ThisBodyType.Name;
		}

		#endregion

		#region Age

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

		#endregion

		#region Colour Selector

		public void OpenColorPicker(Color currentColor, Action<Color> _colorChangeEvent, float yPos)
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
			RefreshAllSkinSharedSkinColoredBodyParts();
		}

		#endregion

		#region Clothing Preference

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

		#endregion

		#region Backpack Preference

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

		#endregion

		#region Pronoun Preference

		public void OnPronounChange()
		{
			int pronoun = (int) currentCharacter.PlayerPronoun;
			pronoun++;
			if (pronoun == (int) PlayerPronoun.None)
			{
				pronoun = 0;
			}
			currentCharacter.PlayerPronoun = (PlayerPronoun) pronoun;
			RefreshPronoun();
		}

		private void RefreshPronoun()
		{
			pronounText.text = currentCharacter.PlayerPronoun.ToString().Replace("_", "/");
		}

		#endregion

		#region Accent Preference
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

		#endregion

		public void OnSurfaceColourChange()
		{
			if (availableSkinColors.Count > 0)
			{
				CurrentSurfaceInt++;
				if (CurrentSurfaceInt >= availableSkinColors.Count)
				{
					CurrentSurfaceInt = 0;
				}

				CurrentSurfaceColour = availableSkinColors[CurrentSurfaceInt];
			}

			else
			{
				OpenColorPicker(CurrentSurfaceColour, SkinColourChange, 32f);
			}

			SkinColourChange(CurrentSurfaceColour);
		}

		public void SkinColourChange(Color color)
		{
			CurrentSurfaceColour = color;
			foreach (var SP in SurfaceSprite)
			{
				SP.SpriteHandler.SetColor(CurrentSurfaceColour);
			}

			currentCharacter.SkinTone = "#" + ColorUtility.ToHtmlStringRGB(CurrentSurfaceColour);
		}

		public void RefreshAllSkinSharedSkinColoredBodyParts()
		{
			foreach (var Customisation in GetComponentsInChildren<BodyPartCustomisationBase>())
			{
				Customisation.Refresh();
			}
		}

		#region Race Preference
		// This will be a temporal thing until we have proper character traits

		public void OnRaceChange()
		{
			SelectedSpecies++;
			if (SelectedSpecies >= RaceSOSingleton.Instance.Races.Count)
			{
				SelectedSpecies = 0;
			}

			currentCharacter.Species = RaceSOSingleton.Instance.Races[SelectedSpecies].name;

			Cleanup();
			var SetRace = RaceSOSingleton.Instance.Races[SelectedSpecies];
			availableSkinColors = SetRace.Base.SkinColours;
			SetUpSpeciesBody(SetRace);
			PopulateAllDropdowns(SetRace);
			DoInitChecks();

			foreach (var BodyCustomisation in OpenBodyCustomisation)
			{
				BodyCustomisation.Value.Refresh();
			}

			foreach (var Customisation in OpenCustomisation)
			{
				Customisation.Refresh();
			}


			RefreshRace();

			OnSurfaceColourChange();
		}

		private void RefreshRace()
		{
			raceText.text = currentCharacter.Species.ToString();

			foreach (var Race in RaceSOSingleton.Instance.Races)
			{
				if (Race.name == currentCharacter.Species)
				{
					ThisSetRace = Race;
				}
			}
		}

		#endregion

		public void InputSerialiseData()
		{
			SaveData();
			SerialiseData.text = JsonConvert.SerializeObject(currentCharacter);
		}

		public void LoadSerialisedData()
		{
			var inCharacter = JsonConvert.DeserializeObject<CharacterSettings>(SerialiseData.text);
			if (inCharacter != null)
			{
				currentCharacter = inCharacter;
				currentCharacter.Username = ServerData.Auth.CurrentUser.DisplayName;
				Cleanup();
				LoadSettings(currentCharacter);
			}
		}

		public enum CharacterDir
		{
			down,
			left,
			up,
			right
		}
	}

	public class ExternalCustomisation
	{
		public string Key;
		public CharacterSettings.CustomisationClass SerialisedValue;
	}

	public class CustomisationStorage
	{
		public string path;
		public string Data;
	}

	public class DataAndType
	{
		public CustomisationType CustomisationType;
		public string data;
	}

	public enum CustomisationType
	{
		Custom,
		Replace,
		Additional
	}
}
