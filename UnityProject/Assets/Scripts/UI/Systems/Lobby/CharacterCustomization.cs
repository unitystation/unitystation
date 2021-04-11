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
using Newtonsoft;

namespace Lobby
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

		public ColorPicker colorPicker;

		

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

		[SerializeField] private GameObject snapCapturer;

		[Header("Character Selector")]
		[SerializeField] private Text WindowName;

		[SerializeField] private Text SlotsUsed;
		[SerializeField] private Text CharacterPreviewName;
		[SerializeField] private Text CharacterPreviewRace;
		[SerializeField] private Text CharacterPreviewBodyType;
		[SerializeField] private RawImage CharacterPreviewImg;

		[SerializeField] private GameObject CharacterPreviews;
		[SerializeField] private GameObject NoCharactersError;
		[SerializeField] private GameObject NoPreviewError;
		[SerializeField] private GameObject GoBackButton;

		[SerializeField] private GameObject CharacterSelectorPage;
		[SerializeField] private GameObject CharacterCreatorPage;

		public List<CharacterSettings> PlayerCharacters = new List<CharacterSettings>();

		private CharacterSettings lastSettings;
		private int currentCharacterIndex = 0;
		private bool savingPictures = false;


		void OnEnable()
		{
			GetSavedCharacters();
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

		private void ShowNoCharacterError()
		{
			CharacterPreviews.SetActive(false);
			NoCharactersError.SetActive(true);
		}

		private void ShowCharacterCreator()
		{
			WindowName.text = "Character Settings";
			CharacterSelectorPage.SetActive(false);
			CharacterCreatorPage.SetActive(true);
			GoBackButton.SetActive(true);
			Cleanup();
			LoadSettings(currentCharacter);
			RefreshAll();
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
		}

		public void ShowCharacterSelectorPage()
		{
			WindowName.text = "Select your character";
			CharacterSelectorPage.SetActive(true);
			CharacterCreatorPage.SetActive(false);
			GoBackButton.SetActive(false);
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
		}

		public void CreateCharacter()
		{
			CharacterSettings character = new CharacterSettings();
			PlayerCharacters.Add(character);
			currentCharacterIndex = PlayerCharacters.Count() - 1;
			currentCharacter = PlayerCharacters[currentCharacterIndex];
			ShowCharacterCreator();
			DoInitChecks();
			RefreshAll();
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
		}

		public void EditCharacter()
		{
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
			ShowCharacterCreator();
			RefreshAll();
		}


		/// <summary>
		/// Responsible for refreshing all data in the character selector page.
		/// </summary>
		private void RefreshSelectorData()
		{
			CharacterPreviewName.text = PlayerCharacters[currentCharacterIndex].Name;
			CharacterPreviewRace.text = PlayerCharacters[currentCharacterIndex].Species;
			CharacterPreviewBodyType.text = PlayerCharacters[currentCharacterIndex].BodyType.ToString();
			SlotsUsed.text = $"{currentCharacterIndex + 1} / {PlayerCharacters.Count()}";
			CheckPreviewImage();
		}


		/// <summary>
		/// If there is no sprite, show an error.
		/// If there is a sprite, display it.
		/// </summary>
		private void CheckPreviewImage()
		{
			string path = Application.persistentDataPath + "/" +
				$"{PlayerCharacters[currentCharacterIndex].Username}/" + PlayerCharacters[currentCharacterIndex].Name;
			if (Directory.Exists(path))
			{
				CharacterPreviewImg.SetActive(true);
				NoPreviewError.SetActive(false);
				Debug.Log(path + $"/down_{PlayerCharacters[currentCharacterIndex].Name}.PNG");
				StartCoroutine(GetPreviewImage(path + $"/down_{PlayerCharacters[currentCharacterIndex].Name}.PNG"));
			}
			else
			{
				CharacterPreviewImg.SetActive(false);
				NoPreviewError.SetActive(true);
			}
		}

		/// <summary>
		/// Loads an image from a localpath or from a server.
		/// </summary>
		/// <param name="path">The path to the image that will be used as a texture.</param>
		/// <returns></returns>
		private IEnumerator<UnityWebRequestAsyncOperation> GetPreviewImage(string path)
		{
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
			{
				yield return uwr.SendWebRequest();

				if (uwr.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(uwr.error);
					CharacterPreviewImg.SetActive(false);
					NoPreviewError.SetActive(true);
				}
				else
				{
					CharacterPreviewImg.texture = DownloadHandlerTexture.GetContent(uwr);
				}
			}
		}

		public void ScrollSelectorLeft()
		{
			if (currentCharacterIndex != 0)
			{
				currentCharacterIndex--;
				currentCharacter = PlayerCharacters[currentCharacterIndex];
			}
			else
			{
				currentCharacterIndex = PlayerCharacters.Count() - 1;
				currentCharacter = PlayerCharacters[currentCharacterIndex];
			}
			RefreshSelectorData();
			RefreshAll();
			SaveLastCharacterIndex();
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
		}
		public void ScrollSelectorRight()
		{
			if (currentCharacterIndex < PlayerCharacters.Count() - 1)
			{
				currentCharacterIndex++;
				currentCharacter = PlayerCharacters[currentCharacterIndex];
			}
			else
			{
				currentCharacterIndex = 0;
				currentCharacter = PlayerCharacters[currentCharacterIndex];
			}
			RefreshSelectorData();
			RefreshAll();
			SaveLastCharacterIndex();
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
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
			currentCharacter.SkinTone = inCharacterSettings.SkinTone;
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

			//This spawns the eyes.
			SetupBodyPartsSprites(bodyPart);
			if (bodyPart.LobbyCustomisation != null)
			{
				var newSprite = Instantiate(bodyPart.LobbyCustomisation, ScrollListBody.transform);
				newSprite.SetUp(this, bodyPart, ""); //Update path
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
			if (bodyPart?.Storage?.Populater?.Contents != null)
			{
				foreach (var Organ in bodyPart.Storage.Populater.Contents)
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
			Type gender = typeof(BodyType);
			Array genders = gender.GetEnumValues();
			int index = UnityEngine.Random.Range(0,3);
			currentCharacter.BodyType = (BodyType)genders.GetValue(index);

			//Randomises player name and age.
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
			currentCharacter.Age = UnityEngine.Random.Range(19, 78);

			//Randomises player accents. (Italian, Scottish, etc)
			randomizeAccent();


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

		private void randomizeAccent()
		{
			int accentChance = UnityEngine.Random.Range(0, 100);
			if(accentChance <= 35)
			{
				Type accent = typeof(Speech);
				Array accents = accent.GetEnumValues();
				int index = UnityEngine.Random.Range(0, 7);
				currentCharacter.Speech = (Speech)accents.GetValue(index);
			}
			else
			{
				currentCharacter.Speech = Speech.None;
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


		//------------------
		//DROPDOWN BOXES:
		//------------------
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

			if (bodyPart?.Storage?.Populater?.Contents != null)
			{
				foreach (var Organ in bodyPart.Storage.Populater.Contents)
				{
					var subBodyPart = Organ.GetComponent<BodyPart>();
					SubSetBodyPart(subBodyPart, path);
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
			// TODO Consider adding await. Otherwise this causes a compile warning.
			ServerData.UpdateCharacterProfile(currentCharacter);
			SaveCharacters();
		}

		/// <summary>
		/// Takes 4 pictures of the character from all sides and stores them in %AppData%/Locallow/unitystation
		/// </summary>
		private IEnumerator<WaitForEndOfFrame> SaveCurrentCharacterSnaps()
		{
			savingPictures = true;
			Util.CaptureUI capture = snapCapturer.GetComponent<Util.CaptureUI>();
			int dir = 0;
			capture.Path = $"/{currentCharacter.Username}/{currentCharacter.Name}"; //Note, we need to add IDs for currentCharacters later to avoid characters who have the same name overriding themselves.
			while(dir < 4)
			{
				RightRotate();
				capture.FileName = $"{currentDir}_{currentCharacter.Name}.PNG";
				//Wait for 3 frames to make sure that all sprites have been loaded and layered correctly when rotating.
				yield return WaitFor.EndOfFrame;
				yield return WaitFor.EndOfFrame;
				yield return WaitFor.EndOfFrame;
				capture.TakeScreenShot();
				dir++;
			}
			savingPictures = false;
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
			SaveLastCharacterIndex(); //Remember the current character index, prevents a bug for newly created characters.
			string json = JsonConvert.SerializeObject(PlayerCharacters, settings);
			string path = Application.persistentDataPath + "characters.json";
			if(File.Exists(path))
			{
				File.Delete(path);
			}
			File.WriteAllText(path, json);
		}

		/// <summary>
		/// Get all characters that are saved in %APPDATA%/Locallow/unitystation/characters.json
		/// </summary>
		private void GetSavedCharacters()
		{
			PlayerCharacters.Clear(); //Clear all entries so we don't have duplicates when re-opening the character page.
			string path = Application.persistentDataPath;

			if(File.Exists(path + "characters.json"))
			{
				CharacterPreviews.SetActive(true);
				NoCharactersError.SetActive(false);
				string json = File.ReadAllText(path + "characters.json");
				var characters = JsonConvert.DeserializeObject<List<CharacterSettings>>(json);

				foreach (var c in characters)
				{
					PlayerCharacters.Add(c);
				}
				currentCharacterIndex = PlayerPrefs.GetInt("lastCharacter", currentCharacterIndex);
				RefreshSelectorData();
			}
			else
			{
				ShowNoCharacterError();
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

			if (bodyPart?.Storage?.Populater?.Contents != null)
			{
				foreach (var Organ in bodyPart.Storage.Populater.Contents)
				{
					var subBodyPart = Organ.GetComponent<BodyPart>();
					SubSaveBodyPart(subBodyPart, path);
				}
			}
		}

		//------------------
		//SAVE & CANCEL BUTTONS:
		//------------------

		public void OnApplyBtn()
		{
			OnApplyBtnLogic();
		}

		private async Task OnApplyBtnLogic()
		{
			StartCoroutine(SaveCurrentCharacterSnaps());

			DisplayErrorText("Saving..");
			//A hacky solution to be able to get character snaps before the UI shuts itself and hides/deletes the player. 
			await Task.Delay(500);

			DisplayErrorText("");
			try
			{
				currentCharacter.ValidateSettings();
			}
			catch (InvalidOperationException e)
			{
				Logger.LogFormat("Invalid character settings: {0}", Category.Character, e.Message);
				SoundManager.Play(SingletonSOSounds.Instance.AccessDenied);
				DisplayErrorText(e.Message);
				return;
			}



			PlayerCharacters[currentCharacterIndex] = currentCharacter;

			//Ensure that the character skin tone is assigned when saving the character
			string skintone = currentCharacter.SkinTone = "#" + ColorUtility.ToHtmlStringRGB(CurrentSurfaceColour);
			PlayerCharacters[currentCharacterIndex].SkinTone = skintone;

			SaveData();
			ShowCharacterSelectorPage();
			SoundManager.Play(SingletonSOSounds.Instance.Click01);
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
		//BACKPACK PREFERENCE
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