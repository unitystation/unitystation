using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Chemistry.Components;
using Items;

[RequireComponent(typeof(Pickupable))]
public class Welder : NetworkBehaviour, IInteractable<HandActivate>, IServerSpawn
{
	//TODO: Update the sprites from the array below based on how much fuel is left
	//TODO: gas readout in stats

	[Header("Place sprites in order from full gas to no gas 5 all up!")]
	public Sprite[] welderSprites;

	public Sprite[] flameSprites;

	public SpriteRenderer welderRenderer;

	public SpriteRenderer flameRenderer;

	public Chemistry.Reagent fuel;

	/// <summary>
	/// Invoked server side when welder turns off for any reason.
	/// </summary>
	[NonSerialized]
	public UnityEvent OnWelderOffServer = new UnityEvent();

	//Inhands
	private int leftHandOriginal = 0;
	private int rightHandOriginal = 0;
	private int leftHandFlame;
	private int rightHandFlame;

	private bool isBurning = false;

	public float damageOn;
	private float damageOff;

	private string currentHand;

	private ItemAttributesV2 itemAtts;
	private RegisterTile registerTile;
	private Pickupable pickupable;

	[SyncVar(hook = nameof(SyncIsOn))]
	private bool isOn;

	/// <summary>
	/// Is welder on?
	/// </summary>
	public bool IsOn => isOn;

	private Coroutine coBurnFuel;

	private ReagentContainer reagentContainer;
	private FireSource fireSource;

	private float FuelAmount => reagentContainer[fuel];

	void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (pickupable != null) return;

		pickupable = GetComponent<Pickupable>();
		itemAtts = GetComponent<ItemAttributesV2>();
		registerTile = GetComponent<RegisterTile>();
		fireSource = GetComponent<FireSource>();

		reagentContainer = GetComponent<ReagentContainer>();
		if (reagentContainer != null)
		{
			reagentContainer.OnSpillAllContents.AddListener(ServerEmptyWelder);
		}

		damageOff = itemAtts.ServerHitDamage;

		//leftHandOriginal = itemAtts.inHandReferenceLeft;
		//rightHandOriginal = itemAtts.inHandReferenceRight;

		leftHandFlame = leftHandOriginal + 4;
		rightHandFlame = rightHandOriginal + 4;
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncIsOn(isOn, isOn);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncIsOn(isOn, false);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ServerToggleWelder(interaction.Performer);
	}

	[Server]
	public void ServerEmptyWelder()
	{
		SyncIsOn(isOn, false);
	}

	[Server]
	public void ServerToggleWelder(GameObject originator)
	{
		SyncIsOn(isOn, !isOn);
	}

	private void SyncIsOn(bool _wasOn, bool _isOn)
	{
		EnsureInit();
		if (isServer)
		{
			if (FuelAmount <= 0f)
			{
				_isOn = false;
			}
		}

		isOn = _isOn;

		if (isServer)
		{
			//update damage stats when on / off
			if (isOn)
			{
				itemAtts.ServerDamageType = DamageType.Burn;
				itemAtts.ServerHitDamage = damageOn;
			}
			else
			{
				itemAtts.ServerDamageType = DamageType.Brute;
				itemAtts.ServerHitDamage = damageOff;
				//stop all progress
				OnWelderOffServer.Invoke();
			}
		}

		//update appearance / animation when on / off
		if (isOn)
		{
			//itemAtts.inHandReferenceLeft = leftHandFlame;
			//itemAtts.inHandReferenceRight = rightHandFlame;
			isBurning = true;
			flameRenderer.sprite = flameSprites[0];
			if (coBurnFuel == null)
				coBurnFuel = StartCoroutine(BurnFuel());

		}
		else
		{
			//itemAtts.inHandReferenceLeft = leftHandOriginal;
			//itemAtts.inHandReferenceRight = rightHandOriginal;
			isBurning = false;
			if (coBurnFuel != null)
			{
				StopCoroutine(coBurnFuel);
				coBurnFuel = null;
			}
			flameRenderer.sprite = null;
		}

		pickupable?.RefreshUISlotImage();

		// toogle firesource to burn things around
		if (fireSource)
		{
			fireSource.IsBurning = isOn;
		}
	}

	IEnumerator BurnFuel()
	{
		int spriteIndex = 0;
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
				//With the variable below, it takes about 3:40 minutes (or 220 seconds) for a emergency welding tool (starts with 10 fuel) to run dry. In /tg/ it would have taken about 4:30 minutes (or 270 seconds). - PM
				//Original variable below was 0.041f (emergency welder ran out after about 25 seconds with it). - PM
				reagentContainer.TakeReagents(0.005f);

				//Ran out of fuel
				if (FuelAmount <= 0f)
				{
					SyncIsOn(isOn, false);
				}
			}

			yield return WaitFor.Seconds(.1f);
		}
	}
}