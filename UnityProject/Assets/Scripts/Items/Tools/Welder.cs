using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using UnityEngine.Networking;

public class Welder : NetworkBehaviour
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

	public GameObject heldByPlayer;
	private string currentHand;

	private ItemAttributes itemAtts;
	private MetaDataLayer metaDataLayer;
	private ReactionManager reactionManager;

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

	void Awake()
	{
		itemAtts = GetComponent<ItemAttributes>();
		metaDataLayer = GetComponentInParent<MetaDataLayer>();
		reactionManager = GetComponentInParent<ReactionManager>();

		leftHandOriginal = itemAtts.inHandReferenceLeft;
		rightHandOriginal = itemAtts.inHandReferenceRight;

		leftHandFlame = leftHandOriginal + 4;
		rightHandFlame = rightHandOriginal + 4;
	}

	[Server]
	public void ToggleWelder(GameObject originator)
	{
		heldByPlayer = originator;
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
			itemAtts.inHandReferenceLeft = leftHandFlame;
			itemAtts.inHandReferenceRight = rightHandFlame;
			isBurning = true;
			flameRenderer.sprite = flameSprites[0];
			if (coBurnFuel == null)
				coBurnFuel = StartCoroutine(BurnFuel());

		}

		if (!isOn || clientFuelAmt <= 0f)
		{
			itemAtts.inHandReferenceLeft = leftHandOriginal;
			itemAtts.inHandReferenceRight = rightHandOriginal;
			isBurning = false;
			if (coBurnFuel != null)
			{
				StopCoroutine(coBurnFuel);
				coBurnFuel = null;
			}
			flameRenderer.sprite = null;
		}

		CheckHeldByPlayer();
	}

	//A broadcast message from InventoryManager.cs on the server:
	public void OnRemoveFromInventory()
	{
		heldByPlayer = null;
	}

	void CheckHeldByPlayer()
	{
		//Local player is holding it, this is so we can update the UISlot of that player holding it
		if (heldByPlayer == PlayerManager.LocalPlayer && heldByPlayer != null)
		{
			if (UIManager.Hands.CurrentSlot.Item == gameObject)
			{
				UIManager.Hands.CurrentSlot.SetSecondaryImage(flameRenderer.sprite);
			}
		}

		//Server also needs to know which player is holding the item so that it can sync
		//the inhand image when the player turns it on and off:
		if (isServer && heldByPlayer != null)
		{
			heldByPlayer.GetComponent<Equipment>().SetHandItemSprite(itemAtts, UIManager.Hands.CurrentSlot.eventName);
		}
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

				Vector3Int position = transform.localPosition.RoundToInt();
				if (heldByPlayer != null)
				{
					position = heldByPlayer.transform.localPosition.RoundToInt();
				}

				reactionManager.ExposeHotspot(position, 700, 0.005f);
			}

			yield return YieldHelper.DeciSecond;
		}
	}
}