using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using Matrix;
using InputControl;

public class ShutterController : ObjectTrigger
{
	private Animator animator;
	private RegisterTile registerTile;

	public bool IsClosed { get; private set; }

	private int closedLayer;
	private int openLayer;

	//For network sync reliability
	private bool waitToCheckState = false;
	private bool tempStateCache;

	void Awake()
	{
		animator = gameObject.GetComponent<Animator>();
		registerTile = gameObject.GetComponent<RegisterTile>();

		closedLayer = LayerMask.NameToLayer("Door Closed");
		openLayer = LayerMask.NameToLayer("Door Open");
		SetLayer(closedLayer);
	}

	public override void Trigger(bool state)
	{
		tempStateCache = state;
		if (waitToCheckState)
			return;
		
		if (animator == null) {
			waitToCheckState = true;
			return;
		}

		SetState(state);
	}

	private void SetState(bool state){
		IsClosed = state;
		registerTile.UpdateTileType(state ? TileType.Door : TileType.None);
		SetLayer(state ? closedLayer : openLayer);
		animator.SetBool("close", state);
	}

	public void SetLayer(int layer)
	{
		gameObject.layer = layer;
		foreach (Transform child in transform) {
			child.gameObject.layer = layer;
		}
	}

	//Handle network spawn sync failure
	IEnumerator WaitToTryAgain(){
		yield return new WaitForSeconds(0.2f);
		if (animator == null) {
			animator = GetComponentInChildren<Animator>();
			if (animator != null) {
				SetState(tempStateCache);
			} else {
				Debug.LogWarning("ShutterController still failing Animator sync");
			}
		} else {
			SetState(tempStateCache);
		}
		waitToCheckState = false;
	}
}
