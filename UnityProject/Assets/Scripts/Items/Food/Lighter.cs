using System;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Base class for lighters (cheap lighters, zippo)
/// </summary>
public class Lighter : NetworkBehaviour, ICheckedInteractable<HandActivate>,
	IServerDespawn
{
	private const int DEFAULT_SPRITE = 0;
	private const int LIT_SPRITE = 1;

	public SpriteHandler[] spriteHandlers = new SpriteHandler[] { };
	private Pickupable pickupable;
	private FireSource fireSource;
	private ItemLightControl lightControl;


	[Tooltip("Fancy lighters (like zippo) have different text and never burn users fingers")]
	public bool isFancy = false;

	[SyncVar]
	private bool isLit = false;

	private void Awake()
	{
		fireSource   = GetComponent<FireSource>();
		pickupable   = GetComponent<Pickupable>();
		lightControl = GetComponent<ItemLightControl>();
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		// toggle lighters flame
		isLit = !isLit;

		// generate message for chat
		// cheap lighter may burn user fingers in process
		GenerateActivationMessage(interaction.PerformerPlayerScript, interaction.HandSlot);

		// update sprites and lighter logic
		ServerUpdateLit();
	}

	private void ServerUpdateLit()
	{
		// toggle flame (will fire things around)
		if (fireSource)   fireSource.IsBurning = isLit;
		if (lightControl) lightControl.Toggle(isLit);

		// set each render to new state
		foreach (var handler in spriteHandlers)
		{
			if (handler)
			{
				int newSpriteID = isLit ? LIT_SPRITE : DEFAULT_SPRITE;
				handler.ChangeSprite(newSpriteID);
			}
		}
	}

	private void GenerateActivationMessage(PlayerScript player, ItemSlot slot)
	{
		if (player == null || slot == null)
		{
			return;
		}

		var playerName = player.gameObject.ExpensiveName();
		var lighterName = gameObject.ExpensiveName();

		// generate message for chat
		if (isLit)
		{
			if (isFancy)
			{
				// edgy smoker
				Chat.AddActionMsgToChat(player.gameObject,
					$"Without even breaking stride, you flip open and light {lighterName} in one smooth movement.",
					$"Without even breaking stride, {playerName} flips open and lights {lighterName} in one smooth movement.");
			}
			else
			{
				var protection = CheckGlovesProtection(player);
				var random = Random.value;

				if (protection || random <= 0.75f)
				{
					Chat.AddActionMsgToChat(player.gameObject,
						$"After a few attempts, you manage to light {lighterName}.",
						$"After a few attempts, {playerName} manages to light {lighterName}.");
				}
				else
				{
					// burn user hand
					var isLeftHand = slot.SlotIdentifier.NamedSlot == NamedSlot.leftHand;
					var bodyPart = isLeftHand ? BodyPartType.LeftArm : BodyPartType.RightArm;

					// AttackType.Fire will set character on fire
					player.playerHealth?.
						ApplyDamageToBodyPart(gameObject, 5f, AttackType.Energy, DamageType.Burn, bodyPart);

					var they = player.characterSettings.TheyPronoun(player);
					var their = player.characterSettings.TheirPronoun(player);

					Chat.AddActionMsgToChat(player.gameObject,
						$"You burn yourself while lighting the lighter!",
						$"After a few attempts, {playerName} manages to light {lighterName} - however, {they} burn {their} finger in the process.");
				}
			}
		}
		else
		{
			if (isFancy)
			{
				var theyre = player.characterSettings.TheyrePronoun(player);
				Chat.AddActionMsgToChat(player.gameObject,
					$"You quietly shut off {lighterName} without even looking at what you're doing. Wow.",
					$"You hear a quiet click, as {playerName} shuts off {lighterName} without even looking at what {theyre} doing. Wow.");
			}
			else
			{
				Chat.AddActionMsgToChat(player.gameObject,
					$"You quietly shut off {lighterName}.",
					$"{playerName}  quietly shuts off {lighterName}.");
			}
		}
	}

	private bool CheckGlovesProtection(PlayerScript player)
	{
		if (player && player.DynamicItemStorage)
		{
			var playerEquipment = player.DynamicItemStorage;
			foreach (var itemSlot in playerEquipment.GetNamedItemSlots(NamedSlot.hands))
			{
				if (itemSlot != null && itemSlot.IsOccupied)
				{
					// TODO: need aditional check to heat resistence and gloves trait
					return true;
				}
			}

		}

		return false;
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		isLit = false;
		ServerUpdateLit();
	}
}
