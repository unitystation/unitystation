using System;
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
using Messages.Server;
using Newtonsoft.Json;
using UI.CharacterCreator;

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
	public readonly Dictionary<NamedSlot, ClothingItem> clothes = new Dictionary<NamedSlot, ClothingItem>();

	public readonly List<BodyPartSprites> Addedbodypart = new List<BodyPartSprites>();

	public readonly List<SpriteHandlerNorder> OpenSprites = new List<SpriteHandlerNorder>();

	public readonly List<BodyPartSprites> SurfaceSprite = new List<BodyPartSprites>();

	private Directional directional;
	private PlayerDirectionalOverlay engulfedBurningOverlay;
	private PlayerDirectionalOverlay partialBurningOverlay;
	private PlayerDirectionalOverlay electrocutedOverlay;
	private PlayerScript playerScript;
	private PlayerHealthV2 playerHealth;
	private PlayerSync playerSync;

	/// <summary>
	/// Define which piece of clothing are hidden (not rendering) right now
	/// </summary>
	private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;

	private ulong overflow = 0;

	public SpriteHandlerNorder ToInstantiateSpriteCustomisation;

	public GameObject CustomisationSprites;
	public GameObject BodySprites;

	public List<IntName> InternalNetIDs = new List<IntName>();

	public bool RootBodyPartsLoaded;

	[SerializeField]
	private GameObject OverlaySprites;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		playerHealth = GetComponent<PlayerHealthV2>();

		foreach (var clothingItem in GetComponentsInChildren<ClothingItem>())
		{
			clothes.Add(clothingItem.Slot, clothingItem);
		}

		AddOverlayGameObjects();

		directional.OnDirectionChange.AddListener(OnDirectionChange);
		//TODO: Need to reimplement fire stacks on players.
		playerHealth.OnClientFireStacksChange.AddListener(OnClientFireStacksChange);
		var healthStateController = GetComponent<HealthStateController>();
		OnClientFireStacksChange(healthStateController.FireStacks);
	}

	private void OnDestroy()
	{
		playerHealth.OnClientFireStacksChange.RemoveListener(OnClientFireStacksChange);
	}

	/// <summary>
	/// Instantiate and attach the sprite overlays if they don't exist
	/// </summary>
	private void AddOverlayGameObjects()
	{
		if (engulfedBurningOverlay == null)
		{
			engulfedBurningOverlay =
				Instantiate(engulfedBurningPrefab, OverlaySprites.transform).GetComponent<PlayerDirectionalOverlay>();
			engulfedBurningOverlay.enabled = true;
			engulfedBurningOverlay.StopOverlay();
		}

		if (partialBurningOverlay == null)
		{
			partialBurningOverlay =
				Instantiate(partialBurningPrefab, OverlaySprites.transform).GetComponent<PlayerDirectionalOverlay>();
			partialBurningOverlay.enabled = true;
			partialBurningOverlay.StopOverlay();
		}

		if (electrocutedOverlay == null)
		{
			electrocutedOverlay = Instantiate(electrocutedPrefab, OverlaySprites.transform).GetComponent<PlayerDirectionalOverlay>();
			electrocutedOverlay.enabled = true;
			electrocutedOverlay.StopOverlay();
		}
	}

	public void SetUpCharacter()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			InstantiateAndSetUp(RaceBodyparts.Base.Head);
			InstantiateAndSetUp(RaceBodyparts.Base.Torso);
			InstantiateAndSetUp(RaceBodyparts.Base.ArmLeft);
			InstantiateAndSetUp(RaceBodyparts.Base.ArmRight);
			InstantiateAndSetUp(RaceBodyparts.Base.LegLeft);
			InstantiateAndSetUp(RaceBodyparts.Base.LegRight);
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
			var data = customisationStorage.Data.Replace("@£", "\"");
			if (Body_Part.OptionalOrgans.Count > 0)
			{
				BodyPartDropDownOrgans.OnPlayerBodyDeserialise(Body_Part, data, livingHealthMasterBase);
			}
			else if (Body_Part.OptionalReplacementOrgan.Count > 0)
			{
				BodyPartDropDownReplaceOrgan.OnPlayerBodyDeserialise(Body_Part, data);
			}
			else
			{
				if(Body_Part.LobbyCustomisation == null)
				{
					Logger.Log($"[PlayerSprites] - Could not find {Body_Part.name}'s characterCustomization script. Returns -> {Body_Part.LobbyCustomisation.characterCustomization}", Category.Character);
					return;
				}
				Body_Part.LobbyCustomisation.OnPlayerBodyDeserialise(Body_Part, data, livingHealthMasterBase);
			}
		}
	}

	public void SetupSprites()
	{

		CustomisationStorage customisationStorage = null;

		if (ThisCharacter.SerialisedBodyPartCustom == null)
		{
			//TODO : (Max) - Fix SerialisedBodyPartCustom being null on Dummy players
			Logger.LogError($"{gameObject} has spawned with null bodyPart customizations. This error should only appear for Dummy players only.");
			return;
		}

		foreach (var Custom in ThisCharacter.SerialisedBodyPartCustom)
		{
			if (livingHealthMasterBase.name == Custom.path)
			{
				customisationStorage = Custom;
				break;
			}
		}

		if (customisationStorage != null)
		{
			BodyPartDropDownOrgans.OnPlayerBodyDeserialise(null, customisationStorage.Data, livingHealthMasterBase);
		}


		foreach (var bodyPart in livingHealthMasterBase.BodyPartList)
		{
			SubSetBodyPart(bodyPart, "");

			//TODO: horrible, remove -- organ prefabs have bodyparts
			foreach (var organ in bodyPart.OrganList)
			{
				var bodyPartOrgan = organ.GetComponent<BodyPart>();
				SubSetBodyPart(bodyPartOrgan, $"/{bodyPart.name}");
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

		List<IntName> ToClient = new List<IntName>();
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

			var SpriteHandlerNorder = Spawn.ServerPrefab(ToInstantiateSpriteCustomisation.gameObject, null, CustomisationSprites.transform)
									.GameObject.GetComponent<SpriteHandlerNorder>();
			SpriteHandlerNorder.transform.localPosition = Vector3.zero;
			SpriteHandlerNorder.name = Customisation.CustomisationGroup.ThisType.ToString();

			var newone = new IntName();
			newone.Int =
				CustomNetworkManager.Instance.IndexLookupSpawnablePrefabs[ToInstantiateSpriteCustomisation.gameObject];

			newone.Name = Customisation.CustomisationGroup.ThisType.ToString();
			ToClient.Add(newone);

			OpenSprites.Add(SpriteHandlerNorder);
			//SubSetBodyPart
			foreach (var Sprite_s in Customisation.CustomisationGroup.PlayerCustomisations)
			{
				if (Sprite_s.Name == externalCustomisation.SerialisedValue.SelectedName)
				{
					SpriteHandlerNorder.SpriteHandler.SetSpriteSO(Sprite_s.SpriteEquipped);
					SpriteHandlerNorder.SetSpriteOrder(new SpriteOrder(Customisation.CustomisationGroup.SpriteOrder));
					newone.Data = JsonConvert.SerializeObject(new SpriteOrder(SpriteHandlerNorder.SpriteOrder));
					Color setColor = Color.black;
					ColorUtility.TryParseHtmlString(externalCustomisation.SerialisedValue.Colour, out setColor);
					setColor.a = 1;
					SpriteHandlerNorder.SpriteHandler.SetColor(setColor);
				}
			}
		}
		GetComponent<RootBodyPartController>().PlayerSpritesData = JsonConvert.SerializeObject(ToClient);

		SetSurfaceColour();
		OnDirectionChange(directional.CurrentDirection);
	}

	public void InstantiateAndSetUp(ObjectList ListToSpawn)
	{
		if (ListToSpawn != null && ListToSpawn.Elements.Count > 0)
		{
			foreach (var ToSpawn in ListToSpawn.Elements)
			{
				var bodyPartObject = Spawn.ServerPrefab(ToSpawn).GameObject;
				livingHealthMasterBase.BodyPartStorage.ServerTryAdd(bodyPartObject);
			}
		}
	}

	public void SetSurfaceColour()
	{
		Color CurrentSurfaceColour = Color.white;
		if (RaceBodyparts.Base.SkinColours.Count > 0)
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.SkinTone, out CurrentSurfaceColour);

			var hasColour = false;

			foreach (var color in RaceBodyparts.Base.SkinColours)
			{
				if (color.ColorApprox(CurrentSurfaceColour))
				{
					hasColour = true;
					break;
				}
			}

			if (hasColour == false)
			{
				CurrentSurfaceColour = RaceBodyparts.Base.SkinColours[0];
			}
		}
		else
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.SkinTone, out CurrentSurfaceColour);
		}

		CurrentSurfaceColour.a = 1;

		foreach (var sp in SurfaceSprite)
		{
			sp.baseSpriteHandler.SetColor(CurrentSurfaceColour);
		}
	}


	private void OnClientFireStacksChange(float newStacks)
	{
		UpdateBurningOverlays(newStacks, directional.CurrentDirection);
	}

	private void OnDirectionChange(Orientation direction)
	{
		//update the clothing sprites
		foreach (var clothingItem in clothes.Values)
		{
			clothingItem.Direction = direction;
		}

		foreach (var sprite in OpenSprites)
		{
			sprite.OnDirectionChange(direction);
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

			foreach (var Race in RaceSOSingleton.Instance.Races)
			{
				if (Race.name == ThisCharacter.Species)
				{
					RaceBodyparts = Race;
					break;
				}
			}
			SetUpCharacter();
			SetupSprites();
			livingHealthMasterBase.CirculatorySystem.SetBloodType(RaceBodyparts.Base.BloodType);
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

	public void OnClothingEquipped(ClothingV2 clothing, bool isEquipped)
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
		// ValidateHideFlag(ClothingHideFlags.HIDE_GLOVES, "hands");
		// ValidateHideFlag(ClothingHideFlags.HIDE_JUMPSUIT, "uniform");
		// ValidateHideFlag(ClothingHideFlags.HIDE_SHOES, "feet");
		// ValidateHideFlag(ClothingHideFlags.HIDE_MASK, "mask");
		// ValidateHideFlag(ClothingHideFlags.HIDE_EARS, "ear");
		// ValidateHideFlag(ClothingHideFlags.HIDE_EYES, "eyes");
		// ValidateHideFlag(ClothingHideFlags.HIDE_NECK, "neck");

		// TODO: Not implemented yet?
		//ValidateHideFlag(ClothingHideFlags.HIDE_SUITSTORAGE, "suit_storage");
	}
/*
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


*/
	public void UpdateChildren(List<IntName> NewInternalNetIDs)
	{
		List<SpriteHandler> SHS = new List<SpriteHandler>();
		foreach (var ID in NewInternalNetIDs)
		{
			bool Contains = false;
			foreach (var InetID in InternalNetIDs)
			{
				if (InetID.Name == ID.Name)
				{
					Contains = true;
				}
			}

			if (Contains == false)
			{
				if (CustomNetworkManager.Instance.allSpawnablePrefabs.Count > ID.Int)
				{
					var OB = Instantiate(CustomNetworkManager.Instance.allSpawnablePrefabs[ID.Int],this.gameObject.transform).transform;
					var Net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(OB.gameObject);
					var Handlers = OB.GetComponentsInChildren<SpriteHandler>();

					foreach (var SH in Handlers)
					{
						SpriteHandlerManager.UnRegisterHandler(Net, SH);
					}

					OB.SetParent(this.transform);
					OB.name = ID.Name;
					OB.localScale = Vector3.one;
					OB.localPosition = Vector3.zero;
					OB.localRotation = Quaternion.identity;



					var SNO = OB.GetComponent<SpriteHandlerNorder>();
					if (OpenSprites.Contains(SNO) == false)
					{
						OpenSprites.Add(SNO);
					}

					foreach (var SH in Handlers)
					{
						SHS.Add(SH);
						SpriteHandlerManager.RegisterHandler(Net, SH);
					}
				}
			}
		}

		foreach (var ID in InternalNetIDs)
		{
			bool Contains = false;
			foreach (var InetID in NewInternalNetIDs)
			{
				if (InetID.Name == ID.Name)
				{
					Contains = true;
				}
			}

			if (Contains == false)
			{
				foreach (var bodyPartSpritese in OpenSprites.ToArray())
				{
					if (bodyPartSpritese.name == ID.Name)
					{
						OpenSprites.Remove(bodyPartSpritese);
						Destroy(bodyPartSpritese.gameObject);
					}

				}
			}
		}

		foreach (var SNO in OpenSprites)
		{
			foreach (var internalNetID in NewInternalNetIDs)
			{
				if (internalNetID.Name == SNO.name)
				{
					SNO.UpdateData(internalNetID.Data);
				}
			}
		}

		InternalNetIDs = NewInternalNetIDs;

	}
}