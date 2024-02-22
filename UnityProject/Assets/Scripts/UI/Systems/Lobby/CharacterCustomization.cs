using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using NaughtyAttributes;
using HealthV2;
using Logs;
using SecureStuff;
using Systems.Character;
using TMPro;
using UI.Character;
using UI.Systems.Lobby;
using Util.Independent.FluentRichText;

namespace UI.CharacterCreator
{
	public class CharacterCustomization : MonoBehaviour
	{
		#region Inspector fields

		[Header("References")]

		[SerializeField]
		private CharacterSettings characterSettingsWindow;

		[field: SerializeField]
		public GameObject SpriteContainer { get; private set; }

		[SerializeField]
		private CustomisationSubPart customisationSubPart;

		[SerializeField] private GameObject ScrollList;
		[SerializeField] private GameObject ScrollListBody;
		[SerializeField] private Transform infoPage;
		[SerializeField] private Transform appearancePage;
		[SerializeField] private SpriteHandlerNorder BodyPartSprite;
		[SerializeField] private ColorPicker colorPicker;
		[SerializeField] private InputField characterNameField;
		[SerializeField] private InputField characterAiNameField;
		[SerializeField] private InputField ageField;
		[SerializeField] private Text errorLabel;
		[SerializeField] private InputField SerialiseData;
		[SerializeField] private BodyPartDropDownOrgans AdditionalOrgan;
		[SerializeField] private BodyPartDropDownReplaceOrgan ReplacementOrgan;
		[SerializeField] private TMP_Dropdown genderChoice;
		[SerializeField] private TMP_Dropdown speciesChoice;
		[SerializeField] private TMP_Dropdown accentChoice;
		[SerializeField] private TMP_Dropdown pronounChoice;
		[SerializeField] private TMP_Dropdown skinColorChoice;
		[SerializeField] private TMP_Dropdown backpackChoice;
		[SerializeField] private TMP_Dropdown clothChoice;
		[SerializeField] private Button skinColorPicker;
		[SerializeField] private BagStyleIcons iconsForBags;
		[SerializeField] private ClothingSyleIcons iconsForCloth;

		[Header("Play Mode Only")]

		[SerializeField, PlayModeOnly]
		private List<BodyTypeName> AvailableBodyTypes = new();

		/// <summary>
		/// The list of unique setting instances that are relevant for the current character's race, like a lizard's tail type.
		/// </summary>
		[SerializeField, PlayModeOnly]
		private List<CustomisationSubPart> OpenCustomisation = new();

		#endregion

		public Dictionary<BodyPart, List<SpriteHandlerNorder>> OpenBodySprites { get; } = new();

		public Dictionary<BodyPart, List<BodyPart>> ParentDictionary { get; } = new();

		private CharacterSheet currentCharacter { get; set; }
		public CharacterSheet CurrentCharacter => currentCharacter;

		public PlayerHealthData ThisSetRace { get; private set; }

		private int SelectedBodyType;
		private BodyTypeName ThisBodyType
		{
			get
			{
				if (AvailableBodyTypes.Count <= SelectedBodyType)
				{
					SelectedBodyType = (AvailableBodyTypes.Count - 1);
				}

				return AvailableBodyTypes[SelectedBodyType];
			}
		}

		private List<CustomisationStorage> bodyPartCustomisationStorage = new();
		private List<ExternalCustomisation> ExternalCustomisationStorage = new();
		private Dictionary<string, BodyPartCustomisationBase> OpenBodyCustomisation = new();

		private List<PlayerHealthData> allSpecies = new List<PlayerHealthData>();

		public List<PlayerHealthData> AllSpecies
		{
			get
			{
				if (allSpecies.Count == 0)
				{
					if (RaceSOSingleton.Instance == null || RaceSOSingleton.Instance.Races.Count == 0)
					{
						Loggy.LogError("UNABLE TO GRAB ALL SPECIES!! CHARACTER CREATION SCREEN IS SURELY GOING TO BE BROKEN!!!");
						return null;
					}
					allSpecies = RaceSOSingleton.GetPlayerSpecies();
				}
				return allSpecies;
			}
		}
		private int SelectedSpecies;

		private List<Color> availableSkinColors;

		private int CurrentSurfaceInt;
		private Color CurrentSurfaceColour = Color.white;
		private readonly List<SpriteHandlerNorder> SurfaceSprite = new();

		private CharacterDir currentDir;

		private readonly TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

		private System.Action onCloseAction;

		#region Lifecycle

		private void OnEnable()
		{
			characterSettingsWindow.SetWindowTitle("Character Editor");

			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.RemoveAllListeners();
			colorPicker.onValueChanged.AddListener(OnColorChange);
			DisplayErrorText("");
		}

		private void OnDisable()
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

		public void LoadCharacter(CharacterSheet inCharacterSettings)
		{
			Cleanup();
			currentCharacter = inCharacterSettings;

			PlayerHealthData SetRace = currentCharacter.GetRaceSo();

			if (SetRace == null)
			{
				SetRace = AllSpecies.First();
			}

			InitiateFresh(SetRace);
			currentCharacter.SkinTone = inCharacterSettings.SkinTone;
		}

		private void InitiateFresh(PlayerHealthData setRace)
		{
			Cleanup();
			//SelectedSpecies
			SelectedSpecies = 0;
			foreach (var species in AllSpecies.TakeWhile(species => species != setRace))
			{
				SelectedSpecies++;
			}

			AvailableBodyTypes = setRace.Base.bodyTypeSettings.AvailableBodyTypes;
			ThisSetRace = setRace;

			availableSkinColors = setRace.Base.SkinColours;

			SetUpSpeciesBody(setRace);
			PopulateAllDropdowns(setRace);
			RefreshAll();
			DoInitChecks();
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

		private void BasedBodyPart(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;

			if (GameObjectBody.Elements.Count == 0) return;

			foreach (var organ in GameObjectBody.Elements)
			{
				if (organ.TryGetComponent<BodyPart>(out var bodyPart) == false)
				{
					Loggy.LogError("[CharacterCustomization/BasedBodyPart] - Unable to grab bodyPart component on object!!");
					continue;
				}
				SetUpBodyPart(bodyPart);
			}
		}

		public void SetUpBodyPart(BodyPart bodyPart, bool instantiateCustomisations = true)
		{
			if (bodyPart == null)
			{
				Loggy.LogWarning("[CharacterCustomization/SetupBodyPart] - Given bodyPart was null, skipping...");
				return;
			}
			//bodyPart.LimbSpriteData;

			//OpenBodyCustomisation[bodyPart.name] = new List<GameObject>();
			ParentDictionary[bodyPart] = new List<BodyPart>();

			// This spawns the eyes.
			SetupBodyPartsSprites(bodyPart);
			if (instantiateCustomisations)
			{
				if (bodyPart.LobbyCustomisation != null)
				{
					var newSprite = Instantiate(bodyPart.LobbyCustomisation, ScrollListBody.transform);
					newSprite.SetUp(this, bodyPart, ""); // Update path
					OpenBodyCustomisation[bodyPart.name] = (newSprite);
				}

				if (bodyPart.OptionalOrgans.Count > 0)
				{
					var option = Instantiate(AdditionalOrgan, ScrollListBody.transform);
					option.SetUp(this, bodyPart, "");
					OpenBodyCustomisation[bodyPart.name] = (option);
				}


				if (bodyPart.OptionalReplacementOrgan.Count > 0)
				{
					var option = Instantiate(ReplacementOrgan, ScrollListBody.transform);
					option.SetUp(this, bodyPart, "");
					OpenBodyCustomisation[bodyPart.name] = (option);
				}


				//Setup sprite//
				//OpenBodySprites
				if (bodyPart.OrNull()?.OrganStorage.OrNull()?.Populater?.DeprecatedContents != null)
				{
					foreach (var organ in bodyPart.OrganStorage.Populater.DeprecatedContents)
					{
						if (organ == null)
						{
							Loggy.LogError($"[CharacterCustomization/SetUpBodyPart/Setup Sprites] - " + "Organ was detected as null!");
							continue;
						}
						if (organ.TryGetComponent<BodyPart>(out var subBodyPart) == false) return;
						ParentDictionary[bodyPart].Add(subBodyPart);
						SetUpBodyPart(subBodyPart);
					}
				}

				if (bodyPart.OrNull()?.OrganStorage.OrNull()?.Populater?.SlotContents != null)
				{
					foreach (var organ in bodyPart.OrganStorage.Populater.SlotContents)
					{
						if (organ == null || organ.Prefab == null) continue;

						if (organ.Prefab.TryGetComponent<BodyPart>(out var subBodyPart) == false) return;

						if (organ.namedSlotPopulatorEntrys.Count > 0)
						{
							Loggy.LogError($"[CharacterCustomization/SetUpBodyPart/Setup Sprites] - " + ".namedSlotPopulatorEntrys Is not supported in character customisation yet!!!");
						}


						ParentDictionary[bodyPart].Add(subBodyPart);
						SetUpBodyPart(subBodyPart);
					}
				}
			}
		}

		private void SetupBodyPartsSprites(BodyPart bodyPart)
		{
			if (ThisBodyType == null || bodyPart == null)
			{
				Loggy.LogError("[CharacterCustomization/SetupBodyPartSprites] - Unable to find a body! Are you sure you got one setup?");
				return;
			}
			Tuple<SpriteOrder, List<SpriteDataSO>> Sprites = null;

			try
			{
				Sprites = bodyPart.GetBodyTypeSprites(ThisBodyType.bodyType); //Get the correct one
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}
			OpenBodySprites[bodyPart] = new List<SpriteHandlerNorder>();

			if (Sprites == null) return;
			if (Sprites?.Item1?.Orders == null || Sprites.Item1.Orders.Count == 0)
			{
				Loggy.LogError("Rendering order not specified on " + bodyPart.name, Category.Character);
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

			if (removeBodyCustomisation)
			{
				if (OpenBodyCustomisation.ContainsKey(bodyPart.name))
				{
					Destroy(OpenBodyCustomisation[bodyPart.name].gameObject);
					OpenBodyCustomisation.Remove(bodyPart.name);
				}

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
			SetAllDropdowns();
			RefreshAll();
		}

		private void RefreshAll()
		{
			if (ThisSetRace.Base.SkinColours.Count > 0)
			{
				skinColorChoice.gameObject.SetActive(true);
				skinColorPicker.gameObject.SetActive(false);
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

				skinColorChoice.ClearOptions();
				foreach (var colorToAdd in ThisSetRace.Base.SkinColours)
				{
					TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
					data.text = ColorUtility.ToHtmlStringRGBA(colorToAdd);
					skinColorChoice.options.Add(data);
				}
			}
			else
			{
				ColorUtility.TryParseHtmlString(currentCharacter.SkinTone, out CurrentSurfaceColour);
				skinColorChoice.gameObject.SetActive(false);
				skinColorPicker.gameObject.SetActive(true);
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
			RefreshRotation();
		}

		public void RollRandomCharacter()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			currentCharacter = CharacterSheet.GenerateRandomCharacter(AllSpecies);

			//Refresh the player character's sheet so they can see their new changes.
			InitiateFresh(currentCharacter.GetRaceSo());

			//Randomises character clothes, cat ears, moth wings, etc.
			randomizeAppearance();
		}

		private void randomizeAppearance()
		{
			//Randomizes hair, tails, etc
			foreach (var custom in OpenBodyCustomisation.Values)
			{
				custom.RandomizeCharacterCreatorValues();
			}

			//Randomizes clothes
			foreach (var customSubPart in OpenCustomisation)
			{
				customSubPart.RandomizeValues();
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

			UpdateAllInfoDropdowns();
		}

		private void UpdateAllInfoDropdowns()
		{
			genderChoice.ClearOptions();
			speciesChoice.ClearOptions();
			accentChoice.ClearOptions();
			backpackChoice.ClearOptions();
			clothChoice.ClearOptions();
			foreach (var bodyType in ThisSetRace.Base.bodyTypeSettings.AvailableBodyTypes)
			{
				TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
				data.text = bodyType.bodyType.ToString();
				genderChoice.options.Add(data);
			}

			foreach (var specie in RaceSOSingleton.Instance.Races)
			{
				if (specie.Base.CanBePlayerChosen == false) continue;
				TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
				data.text = specie.name;
				speciesChoice.options.Add(data);
			}

			foreach (var accent in Enum.GetNames(typeof(Speech)))
			{
				TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
				data.text = accent;
				accentChoice.options.Add(data);
			}

			pronounChoice.ClearOptions();
			foreach (var pronoun in Enum.GetNames(typeof(PlayerPronoun)))
			{
				if (pronoun == "None") continue;
				TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
				data.text = pronoun.Replace("_", "/");;
				pronounChoice.options.Add(data);
			}

			foreach (var style in Enum.GetNames(typeof(BagStyle)))
			{
				TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
				foreach (var iconData in iconsForBags.Icons)
				{
					if (iconData.Style.ToString() == style)
					{
						data.image = iconData.Icon;
						data.text = style;
					}
				}
				backpackChoice.options.Add(data);
			}

			foreach (var style in Enum.GetNames(typeof(ClothingStyle)))
			{
				TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
				foreach (var iconData in iconsForCloth.Icons)
				{
					if (iconData.Style.ToString() == "None") continue;
					if (iconData.Style.ToString() == style)
					{
						data.image = iconData.Icon;
						data.text = style;
					}
				}
				clothChoice.options.Add(data);
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
			RefreshRotation();
		}

		public void RightRotate()
		{
			int nextDir = (int) currentDir - 1;
			if (nextDir < 0)
			{
				nextDir = 3;
			}

			currentDir = (CharacterDir) nextDir;
			RefreshRotation();
		}

		public void RefreshRotation()
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

		private void SetDropDownBody(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;
			if (GameObjectBody.Elements.Count == 0) return;


			foreach (var Organ in GameObjectBody.Elements)
			{
				if (Organ.TryGetComponent<BodyPart>(out var bodyPart) == false)
				{
					Loggy.LogError("[CharacterCustomization/SetDropdownBody] - Organ had no body part component, cannot do subsets.");
					continue;
				}
				SubSetBodyPart(bodyPart, "");
			}
		}

		private void SubSetBodyPart(BodyPart bodyPart, string path)
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

			if (bodyPart.OrNull()?.OrganStorage.OrNull()?.Populater?.DeprecatedContents != null)
			{
				foreach (var organ in bodyPart.OrganStorage.Populater.DeprecatedContents)
				{
					if (organ == null)
					{
						Loggy.LogError($"[CharacterCustomization/SetUpBodyPart/Setup Sprites] - " + "Organ was detected as null!");
						continue;
					}
					if (organ.TryGetComponent<BodyPart>(out var subBodyPart) == false) continue;
					SubSetBodyPart(subBodyPart, path);
				}
			}

			if (bodyPart.OrNull()?.OrganStorage.OrNull()?.Populater?.SlotContents != null)
			{

				foreach (var organ in bodyPart.OrganStorage.Populater.SlotContents)
				{
					if (organ == null || organ.Prefab == null) continue;

					if (organ.Prefab.TryGetComponent<BodyPart>(out var subBodyPart) == false) return;

					if (subBodyPart == null)
					{
						Loggy.LogError($"[CharacterCustomization/SetUpBodyPart/Setup Sprites] - " + "Organ was detected as null!");
						continue;
					}

					SubSetBodyPart(subBodyPart, path);
				}
			}
		}

		#endregion

		#region Player Accounts

		[Button]
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

			Loggy.LogTrace(JsonConvert.SerializeObject(bodyPartCustomisationStorage), Category.Character);
			Loggy.LogTrace(JsonConvert.SerializeObject(ExternalCustomisationStorage), Category.Character);

			characterSettingsWindow.SaveCharacter(currentCharacter);
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

		private void SaveBodyPart(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;
			if (GameObjectBody.Elements.Count == 0) return;


			foreach (var organ in GameObjectBody.Elements)
			{
				if (organ.TryGetComponent<BodyPart>(out var bodyPart) == false)
				{
					Loggy.LogError("[CharacterCustomization/SaveBodyPart] - Attempted to save an organ but did not have a body part script!");
					continue;
				}
				SubSaveBodyPart(bodyPart, "");
			}
		}

		private static void SaveCustomisations(CustomisationStorage customisationStorage,
			BodyPartCustomisationBase CustomisationObject)
		{
			if (CustomisationObject.TryGetComponent<BodyPartCustomisationBase>(out var Customisations) == false) return;
			customisationStorage.Data = Customisations.Serialise();
			customisationStorage.Data = customisationStorage.Data.Replace("\"", "@£");
			//CustomisationStorage
			//SavingDataStorage
		}

		private void SubSaveBodyPart(BodyPart bodyPart, string path)
		{
			path = path + "/" + bodyPart.name;
			foreach (var Customisation in OpenBodyCustomisation)
			{
				if (Customisation.Value.RelatedBodyPart == bodyPart)
				{
					var NewCustomisationStorage = new CustomisationStorage();
					NewCustomisationStorage.path = path;
					bodyPartCustomisationStorage.Add(NewCustomisationStorage);
					SaveCustomisations(NewCustomisationStorage, OpenBodyCustomisation[bodyPart.name]);
					break;
				}
			}


			if (bodyPart.OrNull()?.OrganStorage.OrNull()?.Populater?.DeprecatedContents != null)
			{
				foreach (var organ in bodyPart.OrganStorage.Populater.DeprecatedContents)
				{
					if (organ == null)
					{
						Loggy.LogError("[CharacterCustomization/SaveBodyPart] - Attempted to save an organ but did not have a body part script!");
						continue;
					}
					if(organ.TryGetComponent<BodyPart>(out var subBodyPart) == false) continue;
					SubSaveBodyPart(subBodyPart, path);
				}
			}

			if (bodyPart.OrNull()?.OrganStorage.OrNull()?.Populater?.SlotContents != null)
			{
				foreach (var organ in bodyPart.OrganStorage.Populater.SlotContents)
				{
					if (organ == null || organ.Prefab == null) continue;

					if (organ.Prefab.TryGetComponent<BodyPart>(out var subBodyPart) == false) return;

					if (subBodyPart == null)
					{
						Loggy.LogError($"[CharacterCustomization/SaveBodyPart] - " + "Attempted to save an organ but did not have a body part script!");
						continue;
					}

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
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			DisplayErrorText("");
			try
			{
				currentCharacter.Name = TruncateName(currentCharacter.Name);
				currentCharacter.AiName = TruncateName(currentCharacter.AiName);
				currentCharacter.ValidateSettings();
				currentCharacter.ValidateSpeciesCanBePlayerChosen();
			}
			catch (InvalidOperationException e)
			{
				Loggy.LogFormat("Invalid character settings: {0}", Category.Character, e.Message);
				_ = SoundManager.Play(CommonSounds.Instance.AccessDenied);
				DisplayErrorText(e.Message);
				return;
			}

			SaveData();
			characterSettingsWindow.ShowCharacterSelector();
		}

		public void OnCancelBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			characterSettingsWindow.ShowCharacterSelector();
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
			currentCharacter.Name = StringManager.GetRandomName(currentCharacter.GetGender(), currentCharacter.Species);
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
			if (proposedName.Length >= CharacterSheet.MAX_NAME_LENGTH)
			{
				return proposedName.Substring(0, CharacterSheet.MAX_NAME_LENGTH);
			}
			return proposedName.Capitalize();
		}

		#endregion

		#region Gender

		public void OnBodyTypeChange()
		{
			// ThisBodyType
			// AvailableBodyTypes
			SelectedBodyType = genderChoice.value;
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
			RefreshRotation();
			RefreshBodyType();
		}

		private void RefreshBodyType()
		{
			errorLabel.text = "";
			if (AvailableBodyTypes[SelectedBodyType] == null || genderChoice.options.Count < SelectedBodyType)
			{
				errorLabel.text = "BodyType out of bounds.";
			}
			genderChoice.SetValueWithoutNotify(SelectedBodyType);
			if (SelectedBodyType == 0)
			{
				genderChoice.captionText.text = AvailableBodyTypes[SelectedBodyType].Name;
			}
		}

		#endregion

		#region Age

		private void RefreshAge()
		{
			ageField.text = currentCharacter.Age.ToString();
		}

		public void OnAgeChange()
		{
			if (int.TryParse(ageField.text, out int tryInt))
			{
				tryInt = Mathf.Clamp(tryInt, 18, 99);
				currentCharacter.Age = tryInt;
				RefreshAge();
			}
			else
			{
				RefreshAge();
			}
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
			currentCharacter.ClothingStyle = (ClothingStyle) clothChoice.value;
			RefreshClothing();
		}

		private void RefreshClothing()
		{
			clothChoice.SetValueWithoutNotify((int) currentCharacter.ClothingStyle);
			clothChoice.captionImage.sprite = clothChoice.options[clothChoice.value].image;
		}

		#endregion

		#region Backpack Preference

		public void OnBackpackChange()
		{
			currentCharacter.BagStyle = (BagStyle) backpackChoice.value;
			RefreshBackpack();
		}

		private void RefreshBackpack()
		{
			backpackChoice.SetValueWithoutNotify((int) currentCharacter.BagStyle);
			backpackChoice.captionImage.sprite = backpackChoice.options[backpackChoice.value].image;
		}

		#endregion

		#region Pronoun Preference

		public void OnPronounChange()
		{
			int pronoun = pronounChoice.value;
			if (pronoun == (int) PlayerPronoun.None)
			{
				pronoun = 0;
			}
			currentCharacter.PlayerPronoun = (PlayerPronoun) pronoun;
			RefreshPronoun();
		}

		private void RefreshPronoun()
		{
			pronounChoice.SetValueWithoutNotify((int) currentCharacter.PlayerPronoun);
			if (pronounChoice.value == 0)
			{
				pronounChoice.captionText.text = pronounChoice.options[0].text;
			}
		}

		#endregion

		#region Accent Preference

		// This will be a temporal thing until we have proper character traits

		public void OnAccentChange()
		{
			var index = accentChoice.value;
			var accent = (Speech)index;
			Loggy.Log($"accent is {accent} on index {index}");
			if (accent == Speech.Unintelligible)
			{
				accent = Speech.None;
			}

			currentCharacter.Speech = accent;
			RefreshAccent();
		}

		private void RefreshAccent()
		{
			accentChoice.SetValueWithoutNotify((int) currentCharacter.Speech);
			if (accentChoice.value == 0)
			{
				accentChoice.captionText.text = accentChoice.options[0].text;
			}
		}

		#endregion

		public void OnSurfaceColourChange()
		{
			OpenColorPicker(CurrentSurfaceColour, SkinColourChange, 32f);
			SkinColourChange(CurrentSurfaceColour);
		}

		public void OnSurfaceColourDropdown()
		{
			if (ColorUtility.TryParseHtmlString("#" + skinColorChoice.options[skinColorChoice.value].text, out var newColor))
			{
				SkinColourChange(newColor);
			}
		}

		public void SkinColourChange(Color color)
		{
			CurrentSurfaceColour = color;
			foreach (var SP in SurfaceSprite)
			{
				SP.SpriteHandler.SetColor(CurrentSurfaceColour);
			}
			currentCharacter.SkinTone = "#" + ColorUtility.ToHtmlStringRGB(CurrentSurfaceColour);
			skinColorChoice.image.color = color;
			skinColorPicker.image.color = color;
		}

		private void RefreshAllSkinSharedSkinColoredBodyParts()
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
			SelectedSpecies = speciesChoice.value;
			currentCharacter.Species = AllSpecies[SelectedSpecies].name;
			Cleanup();
			var setRace = AllSpecies[SelectedSpecies];
			currentCharacter.ValidateSpeciesCanBePlayerChosen();
			InitiateFresh(setRace);
			RefreshRace();
		}

		private void RefreshRace()
		{
			ThisSetRace = currentCharacter.GetRaceSo();
			speciesChoice.SetValueWithoutNotify(SelectedSpecies);
			if (speciesChoice.value == 0)
			{
				speciesChoice.captionText.text = speciesChoice.options[0].text;
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
			DisplayErrorText(string.Empty);

			var inCharacter = JsonConvert.DeserializeObject<CharacterSheet>(SerialiseData.text);

			if (inCharacter == null)
			{
				DisplayErrorText("Provided JSON couldn't be deserialised.");
				return;
			}

			try
			{
				inCharacter.ValidateSettings();
			}
			catch (InvalidOperationException e)
			{
				DisplayErrorText($"Deserialised JSON failed character validation. {e.Message}.");
				return;
			}

			Cleanup();
			LoadCharacter(inCharacter);
		}

		public void ShowInfoPage()
		{
			infoPage.gameObject.SetActive(true);
			appearancePage.gameObject.SetActive(false);
		}

		public void ShowAppearancePage()
		{
			infoPage.gameObject.SetActive(false);
			appearancePage.gameObject.SetActive(true);
		}

		public enum CharacterDir
		{
			down,
			left,
			up,
			right
		}
	}

	[System.Serializable]
	public class ExternalCustomisation
	{
		public string Key;
		public CharacterSheet.CustomisationClass SerialisedValue;
	}

	[System.Serializable]
	public class CustomisationStorage
	{
		public string path;
		public string Data;
	}
}
