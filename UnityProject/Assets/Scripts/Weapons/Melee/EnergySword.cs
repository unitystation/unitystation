using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Pickupable))]
public class EnergySword : NetworkBehaviour, ICheckedInteractable<HandActivate>,
	ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
{
	[SerializeField]
	[SyncVar]
	private SwordColor color = default;

	[SerializeField]
	private ItemSize activatedSize = ItemSize.Huge;
	private ItemSize offSize;

	[SerializeField]
	private string activatedHitSound = "blade1";
	private string offHitSound;

	[SerializeField]
	[Range(0, 100)]
	private float activatedHitDamage = 30;
	private float offHitDamage;

	[SerializeField]
	[Range(0, 100)]
	private float activatedThrowDamage = 20;
	private float offThrowDamage;

	[SerializeField]
	[Tooltip("The verbs to use when the energy sword being used to attack something while activated.")]
	private List<string> activatedVerbs = new List<string>();
	private List<string> offAttackVerbs;

	[SerializeField]
	private EswordSprites Sprites;

	private ItemAttributesV2 itemAttributes;
	private ItemLightControl lightControl;
	private SpriteHandler spriteHandler;

	[SyncVar(hook = nameof(SyncState))]
	private bool isActivated;

	#region Lifecycle

	private void Awake()
	{
		itemAttributes = GetComponent<ItemAttributesV2>();
		lightControl = GetComponent<ItemLightControl>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		if (color == SwordColor.Random)
		{
			// Get random color
			color = (SwordColor)Enum.GetValues(typeof(SwordColor)).GetValue(Random.Range(1, 5));
		}
	}

	private void Start()
	{
		offSize = itemAttributes.Size;
		offHitSound = itemAttributes.ServerHitSound;
		offHitDamage = itemAttributes.ServerHitDamage;
		offThrowDamage = itemAttributes.ServerThrowDamage;
		offAttackVerbs = new List<string>(itemAttributes.ServerAttackVerbs);
	}

	#endregion Lifecycle

	private void SyncState(bool oldState, bool newState)
	{
		isActivated = newState;

		if (isActivated)
		{
			itemAttributes.SetSprites(GetItemSprites(color));
		}
		else
		{
			itemAttributes.SetSprites(Sprites.Off);
		}
	}

	#region Interaction-ToggleState

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ServerToggleState(interaction);
	}

	#endregion Interaction-ToggleState

	private void ServerToggleState(HandActivate interaction)
	{
		isActivated = !isActivated; // This runs SyncState, which sets itemAttributes on clients
		var lightColor = GetLightSourceColor(color);
		lightControl.SetColor(lightColor);
		lightControl.Toggle(isActivated);

		if (isActivated)
		{
			SetActivatedAttributes();
			spriteHandler.ChangeSprite((int)color);
			itemAttributes.SetSprites(GetItemSprites(color));
		}
		else
		{
			SetDeactivatedAttributes();
			spriteHandler.ChangeSprite(0);
			itemAttributes.SetSprites(Sprites.Off);
		}

		SoundManager.PlayNetworkedAtPos(
				isActivated ? "saberon" : "saberoff", gameObject.AssumedWorldPosServer());
		StartCoroutine(DelayCharacterSprite(interaction));
	}

	// Cheap hack until networked character sprites.
	private IEnumerator DelayCharacterSprite(HandActivate interaction)
	{
		yield return WaitFor.Seconds(1);
		PlayerAppearanceMessage.SendToAll(interaction.Performer, (int)interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
	}

	#region Interaction-AdjustColor

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		//only works if screwdriver is in hand
		if (!interaction.IsFromHandSlot) return false;

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Multitool);
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		AdjustColor(interaction.UsedObject, interaction.Performer);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Multitool);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		AdjustColor(interaction.UsedObject, interaction.Performer);
	}

	private void AdjustColor(GameObject usedObject, GameObject performer)
	{
		if (isActivated)
		{
			Chat.AddExamineMsgFromServer(performer, "You can't adjust the sword while it's <i>on</i>!");
			return;
		}

		if (color == SwordColor.Rainbow)
		{
			Chat.AddExamineMsgFromServer(performer, "It's <i>already</i> fabulous!");
			return;
		}

		if (Validations.HasItemTrait(usedObject, CommonTraits.Instance.Screwdriver))
		{
			color += 1;
			if (color > SwordColor.Purple)
			{
				color = SwordColor.Red;
			}

			Chat.AddExamineMsgFromServer(performer, "You adjust the crystalline beam emitter.");
		}
		else if (Validations.HasItemTrait(usedObject, CommonTraits.Instance.Multitool))
		{
			color = SwordColor.Rainbow;
			Chat.AddExamineMsgFromServer(performer,
					"You tinker with the sword's firmware using the multitool.\nIt reports; <b>RNBW_ENGAGE</b>.");
		}
	}

	#endregion Interaction-AdjustColor

	private void SetDeactivatedAttributes()
	{
		itemAttributes.ServerSetSize(offSize);
		itemAttributes.ServerHitSound = offHitSound;
		itemAttributes.ServerHitDamage = offHitDamage;
		itemAttributes.ServerThrowDamage = offThrowDamage;
		itemAttributes.ServerAttackVerbs = offAttackVerbs;
	}

	private void SetActivatedAttributes()
	{
		itemAttributes.ServerSetSize(activatedSize);
		itemAttributes.ServerHitSound = activatedHitSound;
		itemAttributes.ServerHitDamage = activatedHitDamage;
		itemAttributes.ServerThrowDamage = activatedThrowDamage;
		itemAttributes.ServerAttackVerbs = activatedVerbs;
	}

	private ItemsSprites GetItemSprites(SwordColor swordColor)
	{
		switch (swordColor)
		{
			case SwordColor.Red:
				return Sprites.Red;
			case SwordColor.Blue:
				return Sprites.Blue;
			case SwordColor.Green:
				return Sprites.Green;
			case SwordColor.Purple:
				return Sprites.Purple;
			case SwordColor.Rainbow:
				return Sprites.Rainbow;
		}

		return Sprites.Off;
	}

	private Color GetLightSourceColor(SwordColor swordColor)
	{
		switch (swordColor)
		{
			case SwordColor.Red:
				return new Color32(250, 130, 130, 255); // LIGHT_COLOR_RED
			case SwordColor.Blue:
				return new Color32(64, 206, 255, 255); // LIGHT_COLOR_LIGHT_CYAN
			case SwordColor.Green:
				return new Color32(100, 200, 100, 255); // LIGHT_COLOR_GREEN
			case SwordColor.Purple:
				return new Color32(155, 81, 255, 255); // LIGHT_COLOR_LAVENDER
		}

		return default;
	}

	private enum SwordColor
	{
		Random = 0,
		Red = 1,
		Blue = 2,
		Green = 3,
		Purple = 4,
		Rainbow = 5
	}
}

[Serializable]
public class EswordSprites{
	public ItemsSprites Blue = new ItemsSprites();
	public ItemsSprites Green = new ItemsSprites();
	public ItemsSprites Purple = new ItemsSprites();
	public ItemsSprites Rainbow = new ItemsSprites();
	public ItemsSprites Red = new ItemsSprites();
	public ItemsSprites Off = new ItemsSprites();
}
