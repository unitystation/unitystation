using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Welder : NetworkBehaviour
{
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

	[SyncVar]
	public float clientFuelAmt;
	private bool isBurning = false;
	private float burnRate = 0.2f;

	public GameObject heldByPlayer;

	private ItemAttributes itemAtts;

	[SyncVar(hook = "UpdateState")]
	public bool isOn;

	public override void OnStartServer()
	{
		Init();
		base.OnStartServer();
		clientFuelAmt = serverFuelAmt;
	}

	public override void OnStartClient()
	{
		Init();
		base.OnStartClient();
		UpdateState(isOn);
	}

	void Init()
	{
		itemAtts = GetComponent<ItemAttributes>();
		leftHandOriginal = itemAtts.inHandReferenceLeft;
		rightHandOriginal = itemAtts.inHandReferenceRight;

		leftHandFlame = leftHandOriginal + 4;
		rightHandFlame = rightHandOriginal + 4;
	}

	public void UpdateState(bool _isOn)
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
			StartCoroutine(BurnFuel());

		}
		else if (!isOn && isBurning || clientFuelAmt <= 0f)
		{
			itemAtts.inHandReferenceLeft = leftHandOriginal;
			itemAtts.inHandReferenceRight = rightHandOriginal;
			isBurning = false;
			flameRenderer.sprite = null;
		}

		CheckHeldByPlayer();
	}

	void CheckHeldByPlayer()
	{
		//Local player is holding it
		if (heldByPlayer == PlayerManager.LocalPlayer && heldByPlayer != null)
		{
			if(UIManager.Hands.CurrentSlot.Item == gameObject){
				UIManager.Hands.CurrentSlot.SetSecondaryImage(flameRenderer.sprite);
			}
		}
	}

	IEnumerator BurnFuel()
	{
		int spriteIndex = 0;
		while (isBurning)
		{
			yield return YieldHelper.DeciSecond;

			flameRenderer.sprite = flameSprites[spriteIndex];
			spriteIndex++;
			if (spriteIndex == 2)
			{
				spriteIndex = 0;
			}

			if (isServer)
			{
				serverFuelAmt -= 0.041f;
				clientFuelAmt = serverFuelAmt;
				if (serverFuelAmt < 0f)
				{
					serverFuelAmt = 0f;
					clientFuelAmt = 0f;
					UpdateState(false);
				}
			}
		}
	}
}