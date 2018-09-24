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

	private ItemAttributes itemAtts;

	[SyncVar(hook = "UpdateState")]
	public bool isOn;

	private void Start()
	{
		itemAtts = GetComponent<ItemAttributes>();
		leftHandOriginal = itemAtts.inHandReferenceLeft;
		rightHandOriginal = itemAtts.inHandReferenceRight;

		leftHandFlame = leftHandOriginal + 4;
		rightHandFlame = rightHandOriginal + 4;
	}

	void UpdateState(bool _isOn)
	{
		isOn = _isOn;
	}
}