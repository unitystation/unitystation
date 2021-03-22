using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Systems.Clothing;
using Light2D;
using Mirror;
using UnityEngine;
using Effects.Overlays;
using HealthV2;
using Lobby;
using Messages.Server;
using Newtonsoft.Json;

/// <summary>
/// Handle displaying the sprites related to player, which includes underwear and the body.
/// Note that the clothing you put on (UniCloths) are handled in Equipment
/// Ghosts are handled in GhostSprites.
/// </summary>
[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(PlayerScript))]
public class PlayerSprites : MonoBehaviour
{
	#region Inspector fields

	public PlayerHealthData RaceBodyparts;

	[Tooltip("Assign the prefab responsible for the partial burning overlay.")] [SerializeField]
	private GameObject partialBurningPrefab = default;

	[Tooltip("Assign the prefab responsible for the engulfed burning overlay.")] [SerializeField]
	private GameObject engulfedBurningPrefab = default;

	[Tooltip("Assign the prefab responsible for the electrocuted overlay.")] [SerializeField]
	private GameObject electrocutedPrefab = default;

	[Tooltip("Muzzle flash, should be on a child of the player gameobject")] [SerializeField]
	private LightSprite muzzleFlash = default;


	[Tooltip("The place that all sprites and body part components go")] [SerializeField]
	private GameObject BodyParts = default;

	#endregion Inspector fields


	public LivingHealthMasterBase livingHealthMasterBase;

	/// <summary>
	/// Threshold value where we switch from partial burning to fully engulfed sprite.
	/// </summary>
	private static readonly float FIRE_STACK_ENGULF_THRESHOLD = 3;

	//For character customization
	public ClothingItem[] characterSprites;

	public CharacterSettings ThisCharacter;

	//clothes for each clothing slot
	//TODO: don't use string as the dictionary key
	public readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

	public readonly List<BodyPartSprites> Addedbodypart = new List<BodyPartSprites>();

	public readonly List<SpriteHandlerNorder> OpenSprites = new List<SpriteHandlerNorder>();

	public readonly List<BodyPartSprites> SurfaceSprite = new List<BodyPartSprites>();

	public readonly Dictionary<string, GameObject> bodyParts = new Dictionary<string, GameObject>();

	private Directional directional;
	private PlayerDirectionalOverlay engulfedBurningOverlay;
	private PlayerDirectionalOverlay partialBurningOverlay;
	private PlayerDirectionalOverlay electrocutedOverlay;
	private PlayerScript playerScript;
	private PlayerHealthV2 playerHealth;
	private PlayerSync playerSync;

	private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;
	private ulong overflow = 0;

	/// <summary>
	/// Define which piece of clothing are hidden (not rendering) right now
	/// </summary>
	public ClothingHideFlags HideClothingFlags => hideClothingFlags;

	public SpriteHandlerNorder ToInstantiateSpriteCustomisation;

	public GameObject CustomisationSprites;

	public List<uint> InternalNetIDs;

	public bool RootBodyPartsLoaded = false;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		playerHealth = GetComponent<PlayerHealthV2>();

		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
			// add listener in case clothing was changed
			c.OnClothingEquipped += OnClothingEquipped;
		}

		for (int i = 0; i < BodyParts.transform.childCount; i++)
		{
			//TODO: Do we need to add listeners for implant removalv
			bodyParts[BodyParts.transform.GetChild(i).name] = BodyParts.transform.GetChild(i).gameObject;
		}


		AddOverlayGameObjects();

		directional.OnDirectionChange.AddListener(OnDirectionChange);
		//TODO: Need to reimplement fire stacks on players.
		playerHealth.EnsureInit();
		playerHealth.OnClientFireStacksChange.AddListener(OnClientFireStacksChange);
		OnClientFireStacksChange(playerHealth.FireStacks);
	}

	/// <summary>
	/// Instantiate and attach the sprite overlays if they don't exist
	/// </summary>
	private void AddOverlayGameObjects()
	{
		if (engulfedBurningOverlay == null)
		{
			engulfedBurningOverlay =
				Instantiate(engulfedBurningPrefab, transform).GetComponent<PlayerDirectionalOverlay>();
			engulfedBurningOverlay.enabled = true;
			engulfedBurningOverlay.StopOverlay();
		}

		if (partialBurningOverlay == null)
		{
			partialBurningOverlay =
				Instantiate(partialBurningPrefab, transform).GetComponent<PlayerDirectionalOverlay>();
			partialBurningOverlay.enabled = true;
			partialBurningOverlay.StopOverlay();
		}

		if (electrocutedOverlay == null)
		{
			electrocutedOverlay = Instantiate(electrocutedPrefab, transform).GetComponent<PlayerDirectionalOverlay>();
			electrocutedOverlay.enabled = true;
			electrocutedOverlay.StopOverlay();
		}
	}


	public void SetUpCharacter(CharacterSettings Character)
	{
		//TODOH Get race file from character settings
		if (CustomNetworkManager.Instance._isServer)
		{
			InstantiateAndSetUp(RaceBodyparts.Base.Head, bodyParts["Head"].transform);
			InstantiateAndSetUp(RaceBodyparts.Base.Torso, bodyParts["Chest"].transform);
			InstantiateAndSetUp(RaceBodyparts.Base.ArmLeft, bodyParts["LeftArm"].transform);
			InstantiateAndSetUp(RaceBodyparts.Base.ArmRight, bodyParts["RightArm"].transform);
			InstantiateAndSetUp(RaceBodyparts.Base.LegLeft, bodyParts["LeftLeg"].transform);
			InstantiateAndSetUp(RaceBodyparts.Base.LegRight, bodyParts["RightLeg"].transform);
		}
	}


	public void SubSetBodyPart(BodyPart Body_Part, string path)
	{
		path = path + "/" + Body_Part.name;


		CustomisationStorage customisationStorage = null;
		foreach (var Custom in ThisCharacter.SerialisedBodyPartCustom)
		{
			if (path == Custom.path)
			{
				customisationStorage = Custom;
				break;
			}
		}

		if (customisationStorage != null)
		{
			bool pass = true;
			if (Body_Part.OptionalOrgans.Count > 0)
			{
				pass = false;
				BodyPartDropDownOrgans.OnPlayerBodyDeserialise(Body_Part, null,
					customisationStorage.Data.Replace("@£", "\""),
					livingHealthMasterBase);
			}

			if (Body_Part.OptionalReplacementOrgan.Count > 0 && pass)
			{
				pass = false;
				BodyPartDropDownReplaceOrgan.OnPlayerBodyDeserialise(Body_Part,
					customisationStorage.Data.Replace("@£", "\""),
					livingHealthMasterBase);
			}

			if (pass)
			{
				Body_Part.LobbyCustomisation.OnPlayerBodyDeserialise(Body_Part,
					customisationStorage.Data.Replace("@£", "\""),
					livingHealthMasterBase);
			}
		}


		foreach (var Limb in Body_Part.ContainBodyParts)
		{
			SubSetBodyPart(Limb, path);
		}
	}

	public IEnumerator WaitForPlayerinitialisation()
	{
		yield return null;
		SetupsSprites();
	}

	public void SetupCharacterData(CharacterSettings Character)
	{
		ThisCharacter = Character;
		StartCoroutine(WaitForPlayerinitialisation());

	}

	public void SetupsSprites()
	{
		foreach (var Root in livingHealthMasterBase.RootBodyPartContainers)
		{
			CustomisationStorage customisationStorage = null;
			foreach (var Custom in ThisCharacter.SerialisedBodyPartCustom)
			{
				if (Root.name == Custom.path)
				{
					customisationStorage = Custom;
					break;
				}
			}

			if (customisationStorage != null)
			{
				BodyPartDropDownOrgans.OnPlayerBodyDeserialise(null, Root, customisationStorage.Data,
					livingHealthMasterBase);
			}

			foreach (var Limb in Root.ContainsLimbs)
			{
				SubSetBodyPart(Limb, "");
			}
		}

		PlayerHealthData SetRace = null;
		foreach (var Race in RaceSOSingleton.Instance.Races)
		{
			if (Race.name == ThisCharacter.Species)
			{
				SetRace = Race;
			}
		}

		List<uint> ToClient = new List<uint>();
		foreach (var Customisation in SetRace.Base.CustomisationSettings)
		{
			ExternalCustomisation externalCustomisation = null;
			foreach (var EC in ThisCharacter.SerialisedExternalCustom)
			{
				if (EC.Key == Customisation.CustomisationGroup.name)
				{
					externalCustomisation = EC;
				}
			}

			if (externalCustomisation == null) continue;

			var SpriteHandlerNorder =
				Spawn.ServerPrefab(ToInstantiateSpriteCustomisation.gameObject, null, CustomisationSprites.transform)
					.GameObject.GetComponent<SpriteHandlerNorder>();

			ToClient.Add(SpriteHandlerNorder.GetComponent<NetworkIdentity>().netId);
			OpenSprites.Add(SpriteHandlerNorder);
			//SubSetBodyPart
			foreach (var Sprite_s in Customisation.CustomisationGroup.PlayerCustomisations)
			{
				if (Sprite_s.Name == externalCustomisation.SerialisedValue.SelectedName)
				{
					SpriteHandlerNorder.SpriteHandler.SetSpriteSO(Sprite_s.SpriteEquipped);
					SpriteHandlerNorder.SetSpriteOrder(new SpriteOrder(Customisation.CustomisationGroup.SpriteOrder));
					Color setColor = Color.black;
					ColorUtility.TryParseHtmlString(externalCustomisation.SerialisedValue.Colour, out setColor);
					SpriteHandlerNorder.SpriteHandler.SetColor(setColor);
				}
			}
		}

		this.GetComponent<RootBodyPartController>().UpdateCustomisations("", JsonConvert.SerializeObject(ToClient));

		//TODOH

		//Fetch Race Health Pack
		// if(Character.Race == Race.Human)
		// {
		//
		// }

		//this is Target zone

		//Instantiates Sprites
		//TODOH
		//Race data contain sprites order
		//healing/Damage splits across body parts
		//needs a generate sprites dynamically


		//RaceBodyparts.Base.LegLeft.GetComponent<ItemStorage>()


		//RaceTexture = Spawn.RaceData["human"];

		//Loop through dimrphic body parts
		//SetupBodySpritesByGender();
		SetSurfaceColour();
		OnDirectionChange(directional.CurrentDirection);
	}

	public void InstantiateAndSetUp(ObjectList ListToSpawn, Transform InWhere)
	{
		if (ListToSpawn != null && ListToSpawn.Elements.Count > 0)
		{
			// var Set = ToSpawn.GetComponent<RootBodyPartContainer>();
			// Set.PlayerSprites = this;
			// Set.healthMaster = playerHealth;
			// var InSpawnResult = Spawn.ServerPrefab(ToSpawn, Vector3.zero, InWhere, AutoOnSpawnServerHook: false);
			// InSpawnResult.GameObject.transform.localPosition = Vector3.zero;

			var rootBodyPartContainer = InWhere.GetComponent<RootBodyPartContainer>();
			//rootBodyPartContainer.ItemStorage

			//rootBodyPartContainer.ItemStorage.ServerTryAdd()
			livingHealthMasterBase.RootBodyPartContainers.Add(rootBodyPartContainer);
			rootBodyPartContainer.PlayerSprites = this;

			foreach (var ToSpawn in ListToSpawn.Elements)
			{
				var InSpawnResult = Spawn.ServerPrefab(ToSpawn).GameObject;
				rootBodyPartContainer.ItemStorage.ServerTryAdd(InSpawnResult);
			}

			//Spawn._ServerFireClientServerSpawnHooks(InSpawnResult);
		}

		// var rootBodyPartContainer = Spawn.ServerPrefab(ToSpawn, null ,InWhere).GameObject.GetComponent<RootBodyPartContainer>();
		// if (rootBodyPartContainer != null)
		// {
		// rootBodyPartContainer.name = ToSpawn.name;
		// livingHealthMasterBase.RootBodyPartContainers.Add(rootBodyPartContainer);
		// rootBodyPartContainer.PlayerSprites = this;
		// rootBodyPartContainer.healthMaster = playerHealth;
		// }
	}

	public void SetSurfaceColour()
	{
		Color CurrentSurfaceColour = Color.white;
		if (RaceBodyparts.Base.SkinColours.Count > 0)
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.SkinTone, out CurrentSurfaceColour);

			if (RaceBodyparts.Base.SkinColours.Contains(CurrentSurfaceColour) == false)
			{
				CurrentSurfaceColour = RaceBodyparts.Base.SkinColours[0];
			}
		}
		else
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.SkinTone, out CurrentSurfaceColour);
		}

		foreach (var sp in SurfaceSprite)
		{
			sp.baseSpriteHandler.SetColor(CurrentSurfaceColour);
		}
	}

	private void SetupSprite(SpriteDataSO spriteSheetAndData, string clothesDictKey, Color? color = null)
	{
		clothes[clothesDictKey].spriteHandler.SetSpriteSO(spriteSheetAndData, color.GetValueOrDefault(Color.white));
	}

	private void OnClientFireStacksChange(float newStacks)
	{
		UpdateBurningOverlays(newStacks, directional.CurrentDirection);
	}

	private void OnDirectionChange(Orientation direction)
	{
		//update the clothing sprites
		foreach (ClothingItem c in clothes.Values)
		{
			c.Direction = direction;
		}

		foreach (var Sprite in OpenSprites)
		{
			Sprite.OnDirectionChange(direction);
		}

		foreach (var bodypart in Addedbodypart)
		{
			bodypart.OnDirectionChange(direction);
		}


		//TODO: Reimplement player fire sprites.
		UpdateBurningOverlays(playerHealth.FireStacks, direction);
		UpdateElectrocutionOverlay(direction);
	}

	/// <summary>
	/// Toggle the electrocuted overlay for the player's mob. Assumes player mob is humanoid.
	/// </summary>
	public void ToggleElectrocutedOverlay()
	{
		if (electrocutedOverlay.OverlayActive)
		{
			electrocutedOverlay.StopOverlay();
		}
		else
		{
			electrocutedOverlay.StartOverlay(directional.CurrentDirection);
		}
	}

	/// <summary>
	/// Enables the electrocuted overlay for the player's mob.
	/// </summary>
	/// <param name="time">If provided and greater than zero, how long until the electrocuted overlay is disabled.</param>
	public void EnableElectrocutedOverlay(float time = -1)
	{
		electrocutedOverlay.StartOverlay(directional.CurrentDirection);

		if (time > 0)
		{
			StartCoroutine(StopElectrocutedOverlayAfter(time));
		}
	}

	/// <summary>
	/// Disables the electrocuted overlay for the player's mob.
	/// </summary>
	public void DisableElectrocutedOverlay()
	{
		electrocutedOverlay.StopOverlay();
	}

	private void UpdateElectrocutionOverlay(Orientation currentFacing)
	{
		if (electrocutedOverlay.OverlayActive)
		{
			electrocutedOverlay.StartOverlay(currentFacing);
		}
		else
		{
			electrocutedOverlay.StopOverlay();
		}
	}

	/// <summary>
	/// Updates whether burning sprites are showing and sets their facing
	/// </summary>
	private void UpdateBurningOverlays(float fireStacks, Orientation currentFacing)
	{
		if (fireStacks <= 0)
		{
			engulfedBurningOverlay.StopOverlay();
			partialBurningOverlay.StopOverlay();
		}
		else if (fireStacks < FIRE_STACK_ENGULF_THRESHOLD)
		{
			partialBurningOverlay.StartOverlay(currentFacing);
			engulfedBurningOverlay.StopOverlay();
		}
		else
		{
			engulfedBurningOverlay.StartOverlay(currentFacing);
			partialBurningOverlay.StopOverlay();
		}
	}

	public void OnCharacterSettingsChange(CharacterSettings characterSettings)
	{
		if (RootBodyPartsLoaded == false)
		{
			RootBodyPartsLoaded = true;
			if (characterSettings == null)
			{
				characterSettings = new CharacterSettings();
			}

			ThisCharacter = characterSettings;

			PlayerHealthData SetRace = null;
			foreach (var Race in RaceSOSingleton.Instance.Races)
			{
				if (Race.name == ThisCharacter.Species)
				{
					RaceBodyparts = Race;
				}
			}


			SetUpCharacter(characterSettings);
			livingHealthMasterBase.CirculatorySystem.SetBloodType(RaceBodyparts.Base.BloodType);
			SetupCharacterData(characterSettings);
		}
	}

	public void NotifyPlayer(NetworkConnection recipient, bool clothItems = false)
	{
		if (clothItems)
		{
			for (int i = 0; i < characterSprites.Length; i++)
			{
				var clothItem = characterSprites[i];
				PlayerAppearanceMessage.SendTo(gameObject, i, recipient, clothItem.GameObjectReference, true, true);
			}
		}
	}

	/// <summary>
	/// Display the muzzle flash animation
	/// </summary>
	public void ShowMuzzleFlash()
	{
		StartCoroutine(AnimateMuzzleFlash());
	}

	private IEnumerator AnimateMuzzleFlash()
	{
		muzzleFlash.gameObject.SetActive(true);
		yield return WaitFor.Seconds(0.1f);
		muzzleFlash.gameObject.SetActive(false);
	}

	/// <summary>
	/// Returns true if this playersprites has a clothing item for the specified named slot
	/// </summary>
	public bool HasClothingItem(NamedSlot? namedSlot)
	{
		return characterSprites.FirstOrDefault(ci => ci.Slot == namedSlot) != null;
	}

	private void OnClothingEquipped(ClothingV2 clothing, bool isEquipped)
	{
		//Logger.Log($"Clothing {clothing} was equipped {isEquiped}!", Category.Inventory);

		// if new clothes equiped, add new hide flags
		if (isEquipped)
		{
			for (int n = 0; n < 11; n++)
			{
				//if both bits are enabled set the n'th bit in the overflow string
				if (IsBitSet((ulong) clothing.HideClothingFlags, n) && (IsBitSet((ulong) hideClothingFlags, n)))
				{
					overflow |= 1UL << n;
				}
				else if (IsBitSet((ulong) clothing.HideClothingFlags, n)) //check if n'th bit is set to 1
				{
					ulong bytechange = (ulong) hideClothingFlags;
					bytechange |= 1UL << n; //set n'th bit to 1
					hideClothingFlags = (ClothingHideFlags) bytechange;
				}
			}
		}
		// if player get off old clothes, we need to remove old flags
		else
		{
			for (int n = 0; n < 11; n++) //repeat 11 times, once for each hide flag
			{
				if (IsBitSet(overflow, n)) //check if n'th bit in overflow is set to 1
				{
					overflow &= ~(1UL << n); //set n'th bit to 0
				}
				else if (IsBitSet((ulong) clothing.HideClothingFlags, n)) //check if n'th bit is set to 1
				{
					ulong bytechange = (ulong) hideClothingFlags;
					bytechange &= ~(1UL << n); //set n'th bit to 0
					hideClothingFlags = (ClothingHideFlags) bytechange;
				}
			}
		}

		// Update hide flags
		ValidateHideFlags();
	}

	private bool IsBitSet(ulong b, int pos)
	{
		return ((b >> pos) & 1) != 0;
	}

	private void ValidateHideFlags()
	{
		foreach (var Norder in Addedbodypart)
		{
			if (Norder.ClothingHide != ClothingHideFlags.HIDE_NONE)
			{
				var isVisible = !hideClothingFlags.HasFlag(Norder.ClothingHide);
				Norder.gameObject.SetActive(isVisible);
			}
		}

		// Need to check all flags with their gameobject names...
		// TODO: it should be done much easier
		ValidateHideFlag(ClothingHideFlags.HIDE_GLOVES, "hands");
		ValidateHideFlag(ClothingHideFlags.HIDE_JUMPSUIT, "uniform");
		ValidateHideFlag(ClothingHideFlags.HIDE_SHOES, "feet");
		ValidateHideFlag(ClothingHideFlags.HIDE_MASK, "mask");
		ValidateHideFlag(ClothingHideFlags.HIDE_EARS, "ear");
		ValidateHideFlag(ClothingHideFlags.HIDE_EYES, "eyes");
		ValidateHideFlag(ClothingHideFlags.HIDE_NECK, "neck");

		// TODO: Not implemented yet?
		//ValidateHideFlag(ClothingHideFlags.HIDE_SUITSTORAGE, "suit_storage");
	}

	private void ValidateHideFlag(ClothingHideFlags hideFlag, string name)
	{
		// Check if dictionary has entry about such clothing item name
		if (!clothes.ContainsKey(name))
		{
			Logger.LogError($"Can't find {name} clothingItem linked to {hideFlag}", Category.PlayerInventory);
			return;
		}

		// Enable or disable based on hide flag
		var isVisible = !hideClothingFlags.HasFlag(hideFlag);
		clothes[name].gameObject.SetActive(isVisible);
	}

	private IEnumerator StopElectrocutedOverlayAfter(float seconds)
	{
		yield return WaitFor.Seconds(seconds);
		DisableElectrocutedOverlay();
	}

	public void UpdateChildren(List<uint> NewInternalNetIDs)
	{
		OpenSprites.Clear();
		List<SpriteHandler> SHS = new List<SpriteHandler>();
		InternalNetIDs = NewInternalNetIDs;
		foreach (var ID in InternalNetIDs)
		{
			if (NetworkIdentity.spawned.ContainsKey(ID) && NetworkIdentity.spawned[ID] != null)
			{
				var OB = NetworkIdentity.spawned[ID].gameObject.transform;

				var SH = OB.GetComponent<SpriteHandler>();

				var Net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(SH.gameObject);
				SpriteHandlerManager.UnRegisterHandler(Net, SH);
				SHS.Add(SH);
				SH.name = Regex.Replace(SH.name, @"\(.*\)", "").Trim();
				OB.parent = CustomisationSprites.transform;
				OB.localScale = Vector3.one;
				OB.localPosition = Vector3.zero;
				OB.localRotation = Quaternion.identity;

				SpriteHandlerManager.RegisterHandler(Net, SH);
				var SNO = OB.GetComponent<SpriteHandlerNorder>();

				if (OpenSprites.Contains(SNO) == false)
				{
					OpenSprites.Add(SNO);
				}
			}
		}

		RequestForceSpriteUpdate.Send(SpriteHandlerManager.Instance, SHS);
	}
}