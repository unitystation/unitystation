using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using HealthV2;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class CharacterCustomization : MonoBehaviour
	{
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

		public SpriteHandlerNorder BodyPartSpite;

		public InputField characterNameField;
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

		[SerializeField] public List<Color> availableSkinColors = new List<Color>();
		private CharacterSettings currentCharacter;

		public ColorPicker colorPicker;

		private CharacterSettings lastSettings;

		public Action onCloseAction;

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

		void OnEnable()
		{
			LoadSettings(PlayerManager.CurrentCharacterSettings);
			colorPicker.gameObject.SetActive(false);
			colorPicker.onValueChanged.AddListener(OnColorChange);
			var copyStr = JsonConvert.SerializeObject(currentCharacter);
			lastSettings = JsonConvert.DeserializeObject<CharacterSettings>(copyStr);
			DisplayErrorText("");

			//torsoSpriteController.sprites.SetSpriteSO(playerTextureData.Base.Torso);
			//headSpriteController.sprites.SetSpriteSO(playerTextureData.Base.Head);
			// RarmSpriteController.sprites.SetSpriteSO(playerTextureData.Base.ArmRight);
			// LarmSpriteController.sprites.SetSpriteSO(playerTextureData.Base.ArmLeft);
			// RlegSpriteController.sprites.SetSpriteSO(playerTextureData.Base.LegRight);
			// LlegSpriteController.sprites.SetSpriteSO(playerTextureData.Base.LegLeft);
			// RHandSpriteController.sprites.SetSpriteSO(playerTextureData.Base.HandRight);
			// LHandSpriteController.sprites.SetSpriteSO(playerTextureData.Base.HandLeft);
			// eyesSpriteController.sprites.SetSpriteSO(playerTextureData.Base.Eyes);
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

		private void LoadSettings(CharacterSettings inCharacterSettings)
		{
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
			SetUpSpeciesBody(SetRace);
			PopulateAllDropdowns(SetRace);
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

		public void BasedBodyPart(ObjectList GameObjectBody)
		{
			if (GameObjectBody == null) return;

			if (GameObjectBody.Elements.Count == 0) return;

			foreach (var Organ in GameObjectBody.Elements)
			{
				var Body_part = Organ.GetComponent<BodyPart>();
				SetUpBodyPart(Body_part);
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


		public void SetUpBodyPart(BodyPart Body_Part, bool addOrganReplacement = true)
		{
			//Body_Part.LimbSpriteData;

			//OpenBodyCustomisation[Body_Part.name] = new List<GameObject>();
			ParentDictionary[Body_Part] = new List<BodyPart>();

			//
			SetupBodyPartsSprites(Body_Part);
			if (Body_Part.LobbyCustomisation != null)
			{
				var newSprite = Instantiate(Body_Part.LobbyCustomisation, ScrollListBody.transform);
				newSprite.SetUp(this, Body_Part, ""); //Update path
				OpenBodyCustomisation[Body_Part.name] = (newSprite);
			}

			if (Body_Part.OptionalOrgans.Count > 0)
			{
				var Option = Instantiate(AdditionalOrgan, ScrollListBody.transform);
				Option.SetUp(this, Body_Part, "");
				OpenBodyCustomisation[Body_Part.name] = (Option);
			}

			if (addOrganReplacement)
			{
				if (Body_Part.OptionalReplacementOrgan.Count > 0)
				{
					var Option = Instantiate(ReplacementOrgan, ScrollListBody.transform);
					Option.SetUp(this, Body_Part, "");
					OpenBodyCustomisation[Body_Part.name] = (Option);
				}
			}


			//Setup sprite//
			//OpenBodySprites
			if (Body_Part?.storage?.Populater?.Contents != null)
			{
				foreach (var Organ in Body_Part.storage.Populater.Contents)
				{
					var SUbBody_part = Organ.GetComponent<BodyPart>();
					ParentDictionary[Body_Part].Add(SUbBody_part);
					SetUpBodyPart(SUbBody_part);
				}
			}
		}

		public void SetupBodyPartsSprites(BodyPart Body_Part)
		{
			OpenBodySprites[Body_Part] = new List<SpriteHandlerNorder>();
			var Sprites = Body_Part.GetBodyTypeSprites(ThisBodyType.bodyType); //Get the correct one


			if (Sprites != null)
			{
				if (Sprites?.Item1?.Orders == null || Sprites.Item1.Orders.Count == 0)
				{
					Logger.LogError("Rendering order not specified on " + Body_Part.name, Category.Character);
				}


				int i = 0;
				foreach (var SpriteData in Sprites.Item2)
				{
					var newSprite = Instantiate(BodyPartSpite, SpriteContainer.transform);
					newSprite.gameObject.transform.localPosition = Vector3.zero;
					newSprite.SetSpriteOrder(new SpriteOrder(Sprites.Item1), true);
					newSprite.SpriteOrder.Add(i);
					newSprite.SpriteHandler.SetSpriteSO(SpriteData);
					OpenBodySprites[Body_Part].Add(newSprite);
					i++;
				}

				if (Body_Part.isSurface)
				{
					SurfaceSprite.AddRange(OpenBodySprites[Body_Part]);
				}
			}
		}

		public void RemoveBodyPart(BodyPart Body_Part, bool removeBodyCustomisation = true)
		{
			if (Body_Part.isSurface)
			{
				foreach (var sp in OpenBodySprites[Body_Part])
				{
					SurfaceSprite.Remove(sp);
				}
			}

			foreach (var Keyv in ParentDictionary)
			{
				if (Keyv.Value.Contains(Body_Part))
				{
					Keyv.Value.Remove(Body_Part);
				}
			}

			if (OpenBodySprites.ContainsKey(Body_Part))
			{
				foreach (var Sprite in OpenBodySprites[Body_Part])
				{
					Destroy(Sprite.gameObject);
				}

				OpenBodySprites.Remove(Body_Part);
			}

			if (OpenBodyCustomisation.ContainsKey(Body_Part.name))
			{
				//removeBodyCustomisation

				if (removeBodyCustomisation == true &&
				    OpenBodyCustomisation[Body_Part.name].GetComponent<BodyPartDropDownReplaceOrgan>() == null)
				{
					Destroy(OpenBodyCustomisation[Body_Part.name]);
				}

				OpenBodyCustomisation.Remove(Body_Part.name);
			}

			if (ParentDictionary.ContainsKey(Body_Part))
			{
				foreach (var SubBody_part in ParentDictionary[Body_Part])
				{
					RemoveBodyPart(SubBody_part);
				}

				ParentDictionary.Remove(Body_Part);
			}
		}

		#endregion BodyPartsSprites

		//First time setting up this character etc?
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
			// Randomise gender
			var changeGender = (UnityEngine.Random.Range(0, 2) == 0);
			if (changeGender)
			{
				//OnGenderChange();
			}

			// Select a random value from each dropdown
			// hairDropdown.value = UnityEngine.Random.Range(0, hairDropdown.options.Count - 1);
			// facialHairDropdown.value = UnityEngine.Random.Range(0, facialHairDropdown.options.Count - 1);
			// underwearDropdown.value = UnityEngine.Random.Range(0, underwearDropdown.options.Count - 1);
			// socksDropdown.value = UnityEngine.Random.Range(0, socksDropdown.options.Count - 1);
				switch (currentCharacter.BodyType)
				{
					case BodyType.Male:
						currentCharacter.Name = StringManager.GetRandomMaleName();
						break;
					case BodyType.Female:
						currentCharacter.Name = StringManager.GetRandomFemaleName();
						break;
					default:
						currentCharacter.Name = StringManager.GetRandomName(Gender.NonBinary);  //probably should get gender neutral names? 
						break;																	//for now it will pick from both the male and female name pools
				}
			// Randomise rest of data
			// currentCharacter.EyeColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			// currentCharacter.HairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			// currentCharacter.SkinTone = availableSkinColors[UnityEngine.Random.Range(0, availableSkinColors.Count - 1)];
			currentCharacter.Age = UnityEngine.Random.Range(19, 78);

			//RefreshAll();
		}

		//------------------
		//DROPDOWN BOXES:
		//------------------
		private void PopulateAllDropdowns(PlayerHealthData Race)
		{
			foreach (var customisation in Race.Base.CustomisationSettings)
			{
				//customisationSubPart
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

			currentDir = (CharacterCustomization.CharacterDir) nextDir;
			SetRotation();
		}

		public void RightRotate()
		{
			int nextDir = (int) currentDir - 1;
			if (nextDir < 0)
			{
				nextDir = 3;
			}

			currentDir = (CharacterCustomization.CharacterDir) nextDir;
			SetRotation();
		}

		public void SetRotation()
		{
			int referenceOffset = 0;
			if (currentDir == CharacterCustomization.CharacterDir.down)
			{
				referenceOffset = 0;
			}

			if (currentDir == CharacterCustomization.CharacterDir.up)
			{
				referenceOffset = 1;
			}

			if (currentDir == CharacterCustomization.CharacterDir.right)
			{
				referenceOffset = 2;
			}

			if (currentDir == CharacterCustomization.CharacterDir.left)
			{
				referenceOffset = 3;
			}

			var Sprites = SpriteContainer.transform.GetComponentsInChildren<SpriteHandlerNorder>();

			var newSprites = Sprites.OrderByDescending(x => x.SpriteOrder.Orders[referenceOffset]).Reverse();

			int i = 0;
			foreach (var Sprite in newSprites)
			{
				(Sprite as SpriteHandlerNorder).gameObject.transform.SetSiblingIndex(i);
				i++;
			}

			foreach (var Customisation in OpenCustomisation)
			{
				Customisation.SetRotation(referenceOffset);
			}

			foreach (var PartSpites in OpenBodySprites)
			{
				foreach (var PartSpite in PartSpites.Value)
				{
					PartSpite.ChangeSpriteVariant(referenceOffset);
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
				var Body_part = Organ.GetComponent<BodyPart>();
				SubSetBodyPart(Body_part, "");
			}
		}


		public void SubSetBodyPart(BodyPart Body_Part, string path)
		{
			path = path + "/" + Body_Part.name;
			if (OpenBodyCustomisation.ContainsKey(Body_Part.name))
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
					var TCustomisation = OpenBodyCustomisation[Body_Part.name];
					TCustomisation.Deserialise(Customisation.Data.Replace("@£", "\""));
				}
			}

			if (Body_Part?.storage?.Populater?.Contents != null)
			{
				foreach (var Organ in Body_Part.storage.Populater.Contents)
				{
					var SUbBody_part = Organ.GetComponent<BodyPart>();
					SubSetBodyPart(SUbBody_part, path);
				}
			}
		}

		//------------------
		//PLAYER ACCOUNTS:
		//------------------
		[NaughtyAttributes.Button()]
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
			;
			currentCharacter.SerialisedBodyPartCustom = new List<CustomisationStorage>(bodyPartCustomisationStorage);
			;
			Logger.Log(JsonConvert.SerializeObject(bodyPartCustomisationStorage), Category.Character);
			Logger.Log(JsonConvert.SerializeObject(ExternalCustomisationStorage), Category.Character);

			PlayerManager.CurrentCharacterSettings = currentCharacter;
			ServerData.UpdateCharacterProfile(
				currentCharacter); // TODO Consider adding await. Otherwise this causes a compile warning.
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
				var Body_part = Organ.GetComponent<BodyPart>();
				SubSaveBodyPart(Body_part, "");
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

		public void SubSaveBodyPart(BodyPart Body_Part, string path)
		{
			path = path + "/" + Body_Part.name;
			if (OpenBodyCustomisation.ContainsKey(Body_Part.name))
			{
				var NewCustomisationStorage = new CustomisationStorage();
				NewCustomisationStorage.path = path;
				bodyPartCustomisationStorage.Add(NewCustomisationStorage);
				SaveCustomisations(NewCustomisationStorage, OpenBodyCustomisation[Body_Part.name]);
			}

			if (Body_Part?.storage?.Populater?.Contents != null)
			{
				foreach (var Organ in Body_Part.storage.Populater.Contents)
				{
					var SUbBody_part = Organ.GetComponent<BodyPart>();
					SubSaveBodyPart(SUbBody_part, path);
				}
			}
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
		//COLOR SELECTOR:
		//------------------

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
		//PRONOUN PREFERENCE:
		//------------------

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

		//------------------
		//RACE PREFERENCE:
		//------------------
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
			SaveData();
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
		}

		private void RefreshRace()
		{
			raceText.text = currentCharacter.Species.ToString();
		}


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