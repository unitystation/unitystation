using System.Collections.Generic;
using Light2D;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Pickupable))]
public class EnergySword : NetworkBehaviour, ICheckedInteractable<HandActivate>,
	ICheckedInteractable<InventoryApply>
{
	private ItemAttributesV2 itemAttributes;
	public EswordSprites Sprites;
	public ItemLightControl playerLightControl;
	public LightSprite worldLight;
	public GameObject worldRenderer;

	[SyncVar(hook = nameof(SyncColor))]
	public int color;

	[Range(0, 100)]
	public float activatedHitDamage = 30;
	private float originalHitDamage;

	[Range(0, 100)]
	public float activatedThrowDamage = 20;
	private float originalThrowDamage;

	public List<string> activatedVerbs = new List<string>();
	private List<string> originalVerbs = new List<string>();

	public ItemSize activatedSize = ItemSize.Huge;
	private ItemSize originalSize;

	public string activatedHitSound = "blade1";
	private string originalHitSound;

	public float activatedLightIntensity = 1;

	[SyncVar(hook = nameof(UpdateState))]
	public bool activated;

	private Pickupable pickupable;

	public void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (itemAttributes != null) return;
		itemAttributes = GetComponent<ItemAttributesV2>();
		pickupable = GetComponent<Pickupable>();
		if (color == (int) SwordColor.Random)
		{
			color = Random.Range(1, 5);
		}

		worldRenderer.SetActive(false);
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncColor(color, color);
	}

	public override void OnStartServer()
	{
		EnsureInit();
		SyncColor(color, color);
	}

	private void SyncColor(int oldC, int c)
	{
		EnsureInit();
		color = c;

		var lightColor = Color.white;

		switch ((SwordColor)color)
		{
			case SwordColor.Red:
				lightColor = new Color32(250, 130, 130, 255); // LIGHT_COLOR_RED
				break;
			case SwordColor.Blue:
				lightColor = new Color32(64, 206, 255, 255); // LIGHT_COLOR_LIGHT_CYAN
				break;
			case SwordColor.Green:
				lightColor = new Color32(100, 200, 100, 255); // LIGHT_COLOR_GREEN
				break;
			case SwordColor.Purple:
				lightColor = new Color32(155, 81, 255, 255); // LIGHT_COLOR_LAVENDER
				break;
		}

		playerLightControl.Colour = lightColor;
		playerLightControl.PlayerLightData.Colour = lightColor;
		worldLight.Color = lightColor;
		pickupable.RefreshUISlotImage();
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return true;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}


		if (interaction.TargetObject != gameObject
			|| !Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver))
		{
			return false;
		}

		//only works if screwdriver is in hand
		if (!interaction.IsFromHandSlot) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ToggleState(interaction.Performer.WorldPosServer());
		PlayerAppearanceMessage.SendToAll(interaction.Performer, (int)interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (activated)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't adjust the sword while it's on!");
			return;
		}

		if (color == (int)SwordColor.Rainbow)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "It's already fabulous!");
			return;
		}

		if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver))
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You adjust the crystalline beam emitter...");
			var c = color + 1;
			if (c >= (int)SwordColor.Rainbow)
			{
				c = (int)SwordColor.Red;
			}

			SyncColor(color, c);
		}
		else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Multitool))
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "RNBW_ENGAGE");
			SyncColor(color, (int)SwordColor.Rainbow);
		}
	}

	public void ToggleState(Vector3 position)
	{
		UpdateState(activated, !activated);

		SoundManager.PlayNetworkedAtPos(activated ? "saberon" : "saberoff", position, 1f);
	}

	private void UpdateState(bool oldState, bool newState)
	{
		activated = newState;

		UpdateSprite();
		UpdateValues();
		UpdateLight();
		pickupable.RefreshUISlotImage();
	}

	private void UpdateSprite()
	{
		if (activated)
		{
			switch ((SwordColor)color)
			{
				case SwordColor.Blue:
					itemAttributes.SetSprites(Sprites.Blue);
					break;
				case SwordColor.Green:
					itemAttributes.SetSprites(Sprites.Green);
					break;
				case SwordColor.Purple:
					itemAttributes.SetSprites(Sprites.Purple);
					break;
				case SwordColor.Rainbow:
					itemAttributes.SetSprites(Sprites.Rainbow);
					break;
				case SwordColor.Red:
					itemAttributes.SetSprites(Sprites.Red);
					break;
			}
		}
		else
		{
			itemAttributes.SetSprites(Sprites.Off);
		}
	}

	private void UpdateValues()
	{
		if (originalVerbs.Count == 0)
		{ // Get the initial values before we replace
			originalHitDamage = itemAttributes.ServerHitDamage;
			originalThrowDamage = itemAttributes.ServerThrowDamage;
			originalVerbs = new List<string>(itemAttributes.ServerAttackVerbs);
			originalSize = itemAttributes.Size;
			originalHitSound = itemAttributes.ServerHitSound;
		}

		if (activated)
		{
			itemAttributes.ServerHitDamage = activatedHitDamage;
			itemAttributes.ServerThrowDamage = activatedThrowDamage;
			itemAttributes.ServerAttackVerbs = activatedVerbs;
			itemAttributes.ServerSetSize(activatedSize);
			itemAttributes.ServerHitSound = activatedHitSound;
		}
		else
		{
			itemAttributes.ServerHitDamage = originalHitDamage;
			itemAttributes.ServerThrowDamage = originalThrowDamage;
			itemAttributes.ServerAttackVerbs = originalVerbs;
			itemAttributes.ServerSetSize(originalSize);
			itemAttributes.ServerHitSound = originalHitSound;
		}
	}

	private void UpdateLight()
	{
		playerLightControl.Toggle(activated);
		playerLightControl.SetIntensity(activated ? activatedLightIntensity : 0);
		worldRenderer.SetActive(activated);
	}

	public enum SwordColor
	{
		Random = 0,
		Red = 1,
		Blue = 2,
		Green = 3,
		Purple = 4,
		Rainbow = 5
	}
}
[System.Serializable]
public class EswordSprites{
	public ItemsSprites Blue = new ItemsSprites();
	public ItemsSprites Green = new ItemsSprites();
	public ItemsSprites Purple = new ItemsSprites();
	public ItemsSprites Rainbow = new ItemsSprites();
	public ItemsSprites Red = new ItemsSprites();
	public ItemsSprites Off = new ItemsSprites();
}