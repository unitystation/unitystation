using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Pickupable))]
public class Welder : NetworkBehaviour, IInteractable<HandActivate>
{
	//TODO: Update the sprites from the array below based on how much fuel is left
	//TODO: gas readout in stats

	[Header("Place sprites in order from full gas to no gas 5 all up!")]
	public Sprite[] welderSprites;

	public Sprite[] flameSprites;

	public SpriteRenderer welderRenderer;

	public SpriteRenderer flameRenderer;

	//Inhands
	private int leftHandOriginal;
	private int rightHandOriginal;
	private int leftHandFlame;
	private int rightHandFlame;

	//Fuel
	private float serverFuelAmt = 100; //About 4mins of burn time

	[SyncVar] public float clientFuelAmt;
	private bool isBurning = false;
	private float burnRate = 0.2f;

	public float damageOn;
	private float damageOff;

	private string currentHand;

	private IItemAttributes itemAtts;
	private RegisterTile registerTile;
	private Pickupable pickupable;

	[SyncVar(hook = nameof(UpdateState))] public bool isOn;

	private Coroutine coBurnFuel;

	public override void OnStartServer()
	{
		base.OnStartServer();
		clientFuelAmt = serverFuelAmt;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	[Server]
	public void Refuel()
	{
		serverFuelAmt = 100f;
		clientFuelAmt = 100f;
	}

	private void Start()
	{
		pickupable = GetComponent<Pickupable>();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ToggleWelder(interaction.Performer);
	}

	void Awake()
	{
		itemAtts = GetComponent<IItemAttributes>();
		registerTile = GetComponent<RegisterTile>();

		damageOff = itemAtts.ServerHitDamage;

		//leftHandOriginal = itemAtts.inHandReferenceLeft;
		//rightHandOriginal = itemAtts.inHandReferenceRight;

		leftHandFlame = leftHandOriginal + 4;
		rightHandFlame = rightHandOriginal + 4;
	}

	[Server]
	public void ToggleWelder(GameObject originator)
	{
		UpdateState(!isOn);
	}

	void UpdateState(bool _isOn)
	{
		if (isServer)
		{
			if (serverFuelAmt <= 0f)
			{
				isOn = false;
			}
		}

		isOn = _isOn;
		ToggleWelder();
	}

	void ToggleWelder()
	{
		if (isOn && !isBurning && clientFuelAmt > 0f)
		{
			//itemAtts.inHandReferenceLeft = leftHandFlame;
			//itemAtts.inHandReferenceRight = rightHandFlame;
			isBurning = true;
			itemAtts.ServerDamageType = DamageType.Burn;
			itemAtts.ServerHitDamage = damageOn;
			flameRenderer.sprite = flameSprites[0];
			if (coBurnFuel == null)
				coBurnFuel = StartCoroutine(BurnFuel());

		}

		if (!isOn || clientFuelAmt <= 0f)
		{
			//itemAtts.inHandReferenceLeft = leftHandOriginal;
			//itemAtts.inHandReferenceRight = rightHandOriginal;
			isBurning = false;
			if (coBurnFuel != null)
			{
				StopCoroutine(coBurnFuel);
				coBurnFuel = null;
			}
			itemAtts.ServerDamageType = DamageType.Brute;
			itemAtts.ServerHitDamage = damageOff;
			flameRenderer.sprite = null;
		}

		CheckHeldByPlayer();
	}

	void CheckHeldByPlayer()
	{
		if (UIManager.Instance != null && UIManager.Hands != null && UIManager.Hands.CurrentSlot != null && UIManager.Hands.CurrentSlot.ItemObject == gameObject)
		{
			//TODO: Need a more systematic way to update inventory sprites.
			Inventory.UpdateSecondaryUISlotImage(gameObject, flameRenderer.sprite);
		}

		//Server also needs to know which player is holding the item so that it can sync
		//the inhand image when the player turns it on and off:
		//if (isServer && heldByPlayer != null)
		//{
		//	var clientPNA = heldByPlayer.GetComponent<PlayerNetworkActions>();
		//	heldByPlayer.GetComponent<Equipment>().SetHandItemSprite(itemAtts, clientPNA.activeHand);
		//}
	}

	IEnumerator BurnFuel()
	{
		int spriteIndex = 0;
		int serverFuelCheck = (int)serverFuelAmt;
		while (isBurning)
		{
			//Flame animation:
			flameRenderer.sprite = flameSprites[spriteIndex];
			spriteIndex++;
			if (spriteIndex == 2)
			{
				spriteIndex = 0;
			}

			//Server fuel burning:
			if (isServer)
			{
				serverFuelAmt -= 0.041f;

				//This is so that the syncvar isn't being updated every DeciSecond:
				if ((int)serverFuelAmt != serverFuelCheck)
				{
					serverFuelCheck = (int)serverFuelAmt;
					clientFuelAmt = serverFuelAmt;
				}

				//Ran out of fuel
				if (serverFuelAmt < 0f)
				{
					serverFuelAmt = 0f;
					clientFuelAmt = 0f;
					UpdateState(false);
				}

				Vector2Int position = gameObject.TileWorldPosition();

				registerTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(position, 700, 0.005f);
			}

			yield return WaitFor.Seconds(.1f);
		}
	}
}