using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
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
	private BurningDirectionalOverlay engulfedBurningOverlay;
	private BurningDirectionalOverlay partialBurningOverlay;
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
		if (ENGULFED_BURNING_OVERLAY_PREFAB == null)
		{
			ENGULFED_BURNING_OVERLAY_PREFAB = Resources.Load<GameObject>("EngulfedBurningPlayer");
			PARTIAL_BURNING_OVERLAY_PREFAB = Resources.Load<GameObject>("PartialBurningPlayer");
		}

		if (engulfedBurningOverlay == null)
		{
			engulfedBurningOverlay = GameObject.Instantiate(ENGULFED_BURNING_OVERLAY_PREFAB, transform)
				.GetComponent<BurningDirectionalOverlay>();
			engulfedBurningOverlay.enabled = true;
			engulfedBurningOverlay.StopBurning();
			partialBurningOverlay = GameObject.Instantiate(PARTIAL_BURNING_OVERLAY_PREFAB, transform)
				.GetComponent<BurningDirectionalOverlay>();
			partialBurningOverlay.enabled = true;
			partialBurningOverlay.StopBurning();
		}

		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
		livingHealthBehaviour.OnClientFireStacksChange.AddListener(OnClientFireStacksChange);
		OnClientFireStacksChange(livingHealthBehaviour.FireStacks);

		//StaticSpriteHandler

		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
			// add listner in case clothing was changed
			c.OnClothingEquiped += OnClothingEquipped;
		}
	}

	public void SetupCharacterData(CharacterSettings Character)
	{
		ThisCharacter = Character;
		RaceTexture = Spawn.RaceData["human"];
		SetupBodySprites();
		SetupCustomisations();
		OnDirectionChange(directional.CurrentDirection);

	}

	public void SetupCustomisations()
	{
		if (ThisCharacter.UnderwearName != "None")
		{
			clothes["underwear"].spriteHandler.spriteData = new SpriteData();
			clothes["underwear"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				PlayerCustomisationDataSOs.Instance.Get(
					CustomisationType.Underwear,
					ThisCharacter.Gender,
					ThisCharacter.UnderwearName
				).Equipped));
			clothes["underwear"].spriteHandler.PushTexture();
		}

		if (ThisCharacter.SocksName != "None")
		{
			clothes["socks"].spriteHandler.spriteData = new SpriteData();
			clothes["socks"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				PlayerCustomisationDataSOs.Instance.Get(
					CustomisationType.Socks,
					ThisCharacter.Gender,
					ThisCharacter.SocksName
				).Equipped));
			clothes["socks"].spriteHandler.PushTexture();
		}


		if (ThisCharacter.FacialHairName != "None")
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.FacialHairColor, out var newColor);
			clothes["beard"].spriteHandler.spriteData = new SpriteData();
			clothes["beard"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				PlayerCustomisationDataSOs.Instance.Get(
					CustomisationType.FacialHair,
					ThisCharacter.Gender,
					ThisCharacter.FacialHairName
				).Equipped));

			clothes["beard"].spriteHandler.SetColor(newColor);
			clothes["beard"].spriteHandler.PushTexture();
		}

		if (ThisCharacter.HairStyleName != "None")
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.HairColor, out var newColor);
			clothes["Hair"].spriteHandler.spriteData = new SpriteData();
			clothes["Hair"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				PlayerCustomisationDataSOs.Instance.Get(
					CustomisationType.HairStyle,
					ThisCharacter.Gender,
					ThisCharacter.HairStyleName
				).Equipped));
			clothes["Hair"].spriteHandler.SetColor(newColor);
			clothes["Hair"].spriteHandler.PushTexture();
		}
	}

	public void SetupBodySprites()
	{
		//RaceVariantTextureData
		//Assuming male for now
		SexSetupBodySprites(RaceTexture.Base);
		if (ThisCharacter.Gender == Gender.Female)
		{
			SexSetupBodySprites(RaceTexture.Female);
		}
		else
		{
			SexSetupBodySprites(RaceTexture.Male);
		}
	}


	public void SexSetupBodySprites(RaceVariantTextureData Variant)
	{
		ColorUtility.TryParseHtmlString(ThisCharacter.SkinTone, out var newColor);

		if (Variant.Torso.Texture != null)
		{
			clothes["body_torso"].spriteHandler.spriteData = new SpriteData();
			clothes["body_torso"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Torso));
			clothes["body_torso"].spriteHandler.SetColor(newColor);
			clothes["body_torso"].spriteHandler.PushTexture();
		}


		if (Variant.LegRight.Texture != null)
		{
			clothes["body_rightleg"].spriteHandler.spriteData = new SpriteData();
			clothes["body_rightleg"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.LegRight));
			clothes["body_rightleg"].spriteHandler.SetColor(newColor);
			clothes["body_rightleg"].spriteHandler.PushTexture();
		}


		if (Variant.LegLeft.Texture != null)
		{
			clothes["body_leftleg"].spriteHandler.spriteData = new SpriteData();
			clothes["body_leftleg"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.LegLeft));
			clothes["body_leftleg"].spriteHandler.SetColor(newColor);
			clothes["body_leftleg"].spriteHandler.PushTexture();
		}

		if (Variant.ArmRight.Texture != null)
		{
			clothes["body_rightarm"].spriteHandler.spriteData = new SpriteData();
			clothes["body_rightarm"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.ArmRight));
			clothes["body_rightarm"].spriteHandler.SetColor(newColor);
			clothes["body_rightarm"].spriteHandler.PushTexture();
		}


		if (Variant.ArmLeft.Texture != null)
		{
			clothes["body_leftarm"].spriteHandler.spriteData = new SpriteData();
			clothes["body_leftarm"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.ArmLeft));
			clothes["body_leftarm"].spriteHandler.SetColor(newColor);
			clothes["body_leftarm"].spriteHandler.PushTexture();
		}


		if (Variant.Head.Texture != null)
		{
			clothes["body_head"].spriteHandler.spriteData = new SpriteData();
			clothes["body_head"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Head));
			clothes["body_head"].spriteHandler.SetColor(newColor);
			clothes["body_head"].spriteHandler.PushTexture();
		}


		if (Variant.HandRight.Texture != null)
		{
			clothes["body_right_hand"].spriteHandler.spriteData = new SpriteData();
			clothes["body_right_hand"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.HandRight));
			clothes["body_right_hand"].spriteHandler.SetColor(newColor);
			clothes["body_right_hand"].spriteHandler.PushTexture();
		}


		if (Variant.HandLeft.Texture != null)
		{
			clothes["body_left_hand"].spriteHandler.spriteData = new SpriteData();
			clothes["body_left_hand"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.HandLeft));
			clothes["body_left_hand"].spriteHandler.SetColor(newColor);
			clothes["body_left_hand"].spriteHandler.PushTexture();
		}

		ColorUtility.TryParseHtmlString(ThisCharacter.EyeColor, out newColor);
		if (Variant.Eyes.Texture != null)
		{
			clothes["eyes"].spriteHandler.spriteData = new SpriteData();
			clothes["eyes"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Eyes));
			clothes["eyes"].spriteHandler.SetColor(newColor);
			clothes["eyes"].spriteHandler.PushTexture();
		}
	}

	private void OnClientFireStacksChange(float newStacks)
	{
		if (newStacks <= 0)
		{
			engulfedBurningOverlay.StopBurning();
			partialBurningOverlay.StopBurning();
		}
		else
		{
			if (newStacks >= FIRE_STACK_ENGULF_THRESHOLD)
			{
				engulfedBurningOverlay.Burn(directional.CurrentDirection);
				partialBurningOverlay.StopBurning();
			}
			else
			{
				partialBurningOverlay.Burn(directional.CurrentDirection);
				engulfedBurningOverlay.StopBurning();
			}
		}
	}

	private void OnDirectionChange(Orientation direction)
	{
		//update the clothing sprites
		foreach (ClothingItem c in clothes.Values)
		{
			c.Direction = direction;
		}

		if (livingHealthBehaviour.FireStacks > 0)
		{
			if (livingHealthBehaviour.FireStacks >= FIRE_STACK_ENGULF_THRESHOLD)
			{
				engulfedBurningOverlay.Burn(direction);
			}
			else
			{
				partialBurningOverlay.Burn(direction);
			}
		}
	}

	public void NotifyPlayer(GameObject recipient, bool clothItems = false)
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
	/// Returns true iff this playersprites has a clothing item for the specified named slot
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
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
		{	//repeat 11 times, once for each bit		
			for (int i = 0;i < 11; i++)
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
