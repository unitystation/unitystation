using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lighter : NetworkBehaviour, ICheckedInteractable<HandActivate>,
	IServerDespawn
{
	private const int DEFAULT_SPRITE = 0;
	private const int LIT_SPRITE = 1;

	public SpriteHandler[] spriteHandlers;
	private Pickupable pickupable;

	public bool isFancy;
	private FireSource fireSource;

	[SyncVar]
	private bool isLit;

	private void Awake()
	{
		fireSource = GetComponent<FireSource>();
		pickupable = GetComponent<Pickupable>();
	}

	private void Update()
	{
		// update UI image on client
		// TODO: replace it with more general method
		pickupable?.RefreshUISlotImage();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		isLit = !isLit;

		// generate message for chat
		GenerateActivationMessage(interaction.PerformerPlayerScript, interaction.HandSlot);

		// toggle flame
		if (fireSource)
		{
			fireSource.IsBurning = isLit;
		}

		// set sprite
		foreach (var handler in spriteHandlers)
		{
			if (handler)
			{
				var newSpriteID = isLit ? LIT_SPRITE : DEFAULT_SPRITE;
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
				Chat.AddActionMsgToChat(player.gameObject,
					$"Without even breaking stride, {playerName} flips open and lights {lighterName} in one smooth movement.",
					$"Without even breaking stride, you flip open and light {lighterName} in one smooth movement.");
			}
			else
			{
				var protection = CheckGlovesProtection(player);
				var random = Random.value;

				if (protection || random <= 0.75f)
				{
					Chat.AddActionMsgToChat(player.gameObject,
						$"After a few attempts, {playerName} manages to light {lighterName}.",
						$"After a few attempts, you manage to light {lighterName}.");
				}
				else
				{
					var isLeftHand = slot.SlotIdentifier.NamedSlot == NamedSlot.leftHand;
					var bodyPart = isLeftHand ? BodyPartType.LeftArm : BodyPartType.RightArm;

					player.playerHealth?.
						ApplyDamageToBodypart(gameObject, 5f, AttackType.Energy, DamageType.Burn, bodyPart);

					var they = player.characterSettings.TheyPronoun();
					var their = player.characterSettings.TheirPronoun();

					Chat.AddActionMsgToChat(player.gameObject,
						$"After a few attempts, {playerName} manages to light {lighterName} - however, {they} burn {their} finger in the process.",
						$"You burn yourself while lighting the lighter!");
				}
			}
		}
		else
		{
			if (isFancy)
			{
				var theyre = player.characterSettings.TheyrePronoun();
				Chat.AddActionMsgToChat(player.gameObject,
					$"You hear a quiet click, as {playerName} shuts off {lighterName} without even looking at what {theyre} doing. Wow.",
					$"You quietly shut off {lighterName} without even looking at what you're doing. Wow.");
			}
			else
			{
				Chat.AddActionMsgToChat(player.gameObject,
					$"{playerName}  quietly shuts off {lighterName}.",
					$"You quietly shut off {lighterName}.");
			}
		}
	}

	private bool CheckGlovesProtection(PlayerScript player)
	{
		if (player && player.ItemStorage)
		{
			var playerEquipment = player.ItemStorage;
			var gloves = playerEquipment.GetNamedItemSlot(NamedSlot.hands);

			if (gloves != null && gloves.IsOccupied)
			{
				// TODO: need aditional check to heat resistence and gloves trait
				return true;
			}
		}

		return false;
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		isLit = false;
	}
}
