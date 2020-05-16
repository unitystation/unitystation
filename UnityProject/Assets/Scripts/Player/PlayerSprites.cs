using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using Mirror;
using UnityEngine;

/// <summary>
/// Handle displaying the sprites related to player, which includes underwear and the body.
/// Note that the clothing you put on (UniCloths) are handled in Equipment
/// Ghosts are handled in GhostSprites.
/// </summary>
[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(PlayerScript))]
public class PlayerSprites : MonoBehaviour
{
	private static GameObject ENGULFED_BURNING_OVERLAY_PREFAB;
	private static GameObject PARTIAL_BURNING_OVERLAY_PREFAB;
	private static GameObject ELECTROCUTED_OVERLAY_PREFAB;

	/// <summary>
	/// Threshold value where we switch from partial burning to fully engulfed sprite.
	/// </summary>
	private static readonly float FIRE_STACK_ENGULF_THRESHOLD = 3;

	public PlayerTextureData RaceTexture;

	//For character customization
	public ClothingItem[] characterSprites;

	public CharacterSettings ThisCharacter;

	//clothes for each clothing slot
	//TODO: don't use string as the dictionary key
	public readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

	private Directional directional;
	private PlayerDirectionalOverlay engulfedBurningOverlay;
	private PlayerDirectionalOverlay partialBurningOverlay;
	private PlayerDirectionalOverlay electrocutedOverlay;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript;
	private PlayerHealth playerHealth;
	private PlayerSync playerSync;

	private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;
	/// <summary>
	/// Define which piece of clothing are hidden (not rendering) right now
	/// </summary>
	public ClothingHideFlags HideClothingFlags => hideClothingFlags;

	[Tooltip("Muzzle flash, should be on a child of the player gameobject")]
	public LightSprite muzzleFlash;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();

		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
			// add listener in case clothing was changed
			c.OnClothingEquiped += OnClothingEquipped;
		}

		//TODO: Remove Resources.Load calls, change to prefab references stored somewhere
		if (ENGULFED_BURNING_OVERLAY_PREFAB == null)
		{
			ENGULFED_BURNING_OVERLAY_PREFAB = Resources.Load<GameObject>("EngulfedBurningPlayer");
			PARTIAL_BURNING_OVERLAY_PREFAB = Resources.Load<GameObject>("PartialBurningPlayer");
			ELECTROCUTED_OVERLAY_PREFAB = Resources.Load<GameObject>("ElectrocutedHumanoid");
		}

		AddOverlayGameObjects();

		directional.OnDirectionChange.AddListener(OnDirectionChange);
		livingHealthBehaviour.OnClientFireStacksChange.AddListener(OnClientFireStacksChange);
		OnClientFireStacksChange(livingHealthBehaviour.FireStacks);
	}

	/// <summary>
	/// Instantiate and attach the sprite overlays if they don't exist
	/// </summary>
	private void AddOverlayGameObjects()
	{
		if (engulfedBurningOverlay == null)
		{
			engulfedBurningOverlay = Instantiate(ENGULFED_BURNING_OVERLAY_PREFAB, transform).GetComponent<PlayerDirectionalOverlay>();
			engulfedBurningOverlay.enabled = true;
			engulfedBurningOverlay.StopOverlay();
		}
		if (partialBurningOverlay == null)
		{
			partialBurningOverlay = Instantiate(PARTIAL_BURNING_OVERLAY_PREFAB, transform).GetComponent<PlayerDirectionalOverlay>();
			partialBurningOverlay.enabled = true;
			partialBurningOverlay.StopOverlay();
		}
		if (electrocutedOverlay == null)
		{
			electrocutedOverlay = Instantiate(ELECTROCUTED_OVERLAY_PREFAB, transform).GetComponent<PlayerDirectionalOverlay>();
			electrocutedOverlay.enabled = true;
			electrocutedOverlay.StopOverlay();
		}
	}

	public void SetupCharacterData(CharacterSettings Character)
	{
		ThisCharacter = Character;
		RaceTexture = Spawn.RaceData["human"];
		SetupBodySpritesByGender();
		SetupAllCustomisationSprites();
		OnDirectionChange(directional.CurrentDirection);
	}

	/// <summary>
	/// Sets up the sprites for all customisations (Hair and underclothes)
	/// </summary>
	public void SetupAllCustomisationSprites()
	{
		SetupCustomisationSprite(CustomisationType.Underwear, "underwear", ThisCharacter.UnderwearName);
		SetupCustomisationSprite(CustomisationType.Socks, "socks", ThisCharacter.SocksName);
		SetupCustomisationSprite(CustomisationType.FacialHair, "beard", ThisCharacter.FacialHairName, ThisCharacter.FacialHairColor);
		SetupCustomisationSprite(CustomisationType.HairStyle, "Hair", ThisCharacter.HairStyleName, ThisCharacter.HairColor);
	}

	/// <summary>
	/// Sets up the sprite for a specific customisation
	/// </summary>
	private void SetupCustomisationSprite(CustomisationType customisationType, string customisationKey, string customisationName, string htmlColor)
	{
		if (ColorUtility.TryParseHtmlString(htmlColor, out var newColor))
		{
			SetupCustomisationSprite(customisationType, customisationKey, customisationName, newColor);
		}
		else
		{
			SetupCustomisationSprite(customisationType, customisationKey, customisationName);
		}
	}

	/// <summary>
	/// Sets up the sprite for a specific customisation
	/// </summary>
	private void SetupCustomisationSprite(CustomisationType customisationType, string customisationKey, string customisationName, Color? color = null)
	{
		if (customisationName != "None")
		{
			SpriteSheetAndData spriteSheetAndData = PlayerCustomisationDataSOs.Instance.Get(customisationType, ThisCharacter.Gender, customisationName).Equipped;
			SetupSprite(spriteSheetAndData, customisationKey, color);
		}
	}

	public void SetupBodySpritesByGender()
	{
		SetupAllBodySprites(RaceTexture.Base);
		if (ThisCharacter.Gender == Gender.Female)
		{
			SetupAllBodySprites(RaceTexture.Female);
		}
		else
		{
			SetupAllBodySprites(RaceTexture.Male);
		}
	}

	/// <summary>
	/// Sets up the sprites for all body parts using the given RaceVariantTextureData
	/// </summary>
	public void SetupAllBodySprites(RaceVariantTextureData Variant)
	{
		Color? newSkinColor = null;
		if (ColorUtility.TryParseHtmlString(ThisCharacter.SkinTone, out var tempSkinColor))
		{
			newSkinColor = tempSkinColor;
		}

		SetupBodySprite(Variant.Torso, "body_torso", newSkinColor);
		SetupBodySprite(Variant.LegRight, "body_rightleg", newSkinColor);
		SetupBodySprite(Variant.LegLeft, "body_leftleg", newSkinColor);
		SetupBodySprite(Variant.ArmRight, "body_rightarm", newSkinColor);
		SetupBodySprite(Variant.ArmLeft, "body_leftarm", newSkinColor);
		SetupBodySprite(Variant.Head, "body_head", newSkinColor);
		SetupBodySprite(Variant.HandRight, "body_right_hand", newSkinColor);
		SetupBodySprite(Variant.HandLeft, "body_left_hand", newSkinColor);

		Color? newEyeColor = null;
		if (ColorUtility.TryParseHtmlString(ThisCharacter.EyeColor, out var tempEyeColor))
		{
			newEyeColor = tempEyeColor;
		}

		SetupBodySprite(Variant.Eyes, "eyes", newEyeColor);
	}

	/// <summary>
	/// Sets up the sprite for a specific body part
	/// </summary>
	private void SetupBodySprite(SpriteSheetAndData variantBodypart, string bodypartKey, Color? color = null)
	{
		if (variantBodypart.Texture != null)
		{
			SetupSprite(variantBodypart, bodypartKey, color);
		}
	}

	private void SetupSprite(SpriteSheetAndData spriteSheetAndData, string clothesDictKey, Color? color = null)
	{
		clothes[clothesDictKey].spriteHandler.spriteData = new SpriteData();
		clothes[clothesDictKey].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(spriteSheetAndData));

		if (color != null)
		{
			clothes[clothesDictKey].spriteHandler.SetColor(color.Value);
		}

		clothes[clothesDictKey].spriteHandler.PushTexture();
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

		UpdateBurningOverlays(livingHealthBehaviour.FireStacks, direction);
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

	private void UpdateElectrocutionOverlay(Orientation currentFacing)
	{
		if (electrocutedOverlay.OverlayActive)
		{
			electrocutedOverlay.StartOverlay(currentFacing);
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
		if (characterSettings == null)
		{
			characterSettings = new CharacterSettings();
		}

		SetupCharacterData(characterSettings);
		// FIXME: this probably shouldn't send ALL of the character settings to everyone
		PlayerCustomisationMessage.SendToAll(gameObject, characterSettings);
	}

	public void NotifyPlayer(NetworkConnection recipient, bool clothItems = false)
	{
		if (!clothItems)
		{
			PlayerCustomisationMessage.SendTo(gameObject, recipient, ThisCharacter);
		}
		else
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

	private void OnClothingEquipped(ClothingV2 clothing, bool isEquiped)
	{
		//Logger.Log($"Clothing {clothing} was equipped {isEquiped}!", Category.Inventory);

		// if new clothes equiped, add new hide flags
		if (isEquiped)
			hideClothingFlags |= clothing.HideClothingFlags;
		// if player get off old clothes, we need to remove old flags
		else
		{
			for (int i = 0; i < 11; i++) //repeat 11 times, once for each bit
			{
				//get a bit from the byte
				ulong bit = ((ulong)clothing.HideClothingFlags >> i) & 1U;
				if (bit == 1)
				{
					//disable the enabled bit
					ulong bytechange = (ulong)hideClothingFlags;
					bytechange &= ~(1UL << i);
					hideClothingFlags = (ClothingHideFlags)bytechange;
				}
			}
		}
		// Update hide flags
		ValidateHideFlags();
	}

	private void ValidateHideFlags()
	{
		// Need to check all flags with their gameobject names...
		// TODO: it should be done much easier
		ValidateHideFlag(ClothingHideFlags.HIDE_GLOVES, "hands");
		ValidateHideFlag(ClothingHideFlags.HIDE_JUMPSUIT, "uniform");
		ValidateHideFlag(ClothingHideFlags.HIDE_SHOES, "feet");
		ValidateHideFlag(ClothingHideFlags.HIDE_MASK, "mask");
		ValidateHideFlag(ClothingHideFlags.HIDE_EARS, "ear");
		ValidateHideFlag(ClothingHideFlags.HIDE_EYES, "eyes");
		ValidateHideFlag(ClothingHideFlags.HIDE_FACE, "face");
		ValidateHideFlag(ClothingHideFlags.HIDE_HAIR, "Hair");
		ValidateHideFlag(ClothingHideFlags.HIDE_FACIALHAIR, "beard");
		ValidateHideFlag(ClothingHideFlags.HIDE_NECK, "neck");

		// TODO: Not implemented yet?
		//ValidateHideFlag(ClothingHideFlags.HIDE_SUITSTORAGE, "suit_storage");
	}

	private void ValidateHideFlag(ClothingHideFlags hideFlag, string name)
	{
		// Check if dictionary has entry about such clothing item name
		if (!clothes.ContainsKey(name))
		{
			Logger.LogError($"Can't find {name} clothingItem linked to {hideFlag}");
			return;
		}

		// Enable or disable based on hide flag
		var isVisible = !hideClothingFlags.HasFlag(hideFlag);
		clothes[name].gameObject.SetActive(isVisible);
	}
}