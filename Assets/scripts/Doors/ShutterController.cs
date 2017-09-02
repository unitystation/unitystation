using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using Matrix;
using InputControl;
using UnityEngine.Networking;

public class ShutterController : ObjectTrigger
{
	private Animator animator;
	private RegisterTile registerTile;

	public bool IsClosed { get; private set; }

	private int closedLayer;
	private int openLayer;
	private int closedSortingLayer;
	private int openSortingLayer;

	//For network sync reliability
	private bool waitToCheckState = false;
	private bool tempStateCache;

	void Awake()
	{
		animator = gameObject.GetComponent<Animator>();
		registerTile = gameObject.GetComponent<RegisterTile>();

		closedLayer = LayerMask.NameToLayer("Door Closed");
		closedSortingLayer = SortingLayer.NameToID("Doors Open");
		openLayer = LayerMask.NameToLayer("Door Open");
		openSortingLayer = SortingLayer.NameToID("Doors Closed");
		SetLayer(closedLayer, closedSortingLayer);
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
		if ( state )
		{
			SetLayer(closedLayer, closedSortingLayer);
			if (isServer)
			{
				DamageOnClose();
			}
		}
		else
		{
			SetLayer(openLayer, openSortingLayer);
		}
		
		animator.SetBool("close", state);
	}

	public void SetLayer(int layer, int sortingLayer)
	{
//		GetComponentInChildren<SpriteRenderer>().sortingLayerID = sortingLayer;
		gameObject.layer = layer;
		foreach (Transform child in transform) {
			child.gameObject.layer = layer;
		}
	}
	[Server]
	private void DamageOnClose()
	{
		var currentTile = Matrix.Matrix.At(transform.position);
//		var currentTile = Matrix.Matrix.At(transform.position);
		if ( currentTile.IsObject() || currentTile.IsPlayer() )
		{
			var healthBehaviours = currentTile.GetDamageables();
			for ( var i = 0; i < healthBehaviours.Count; i++ )
			{
				healthBehaviours[i].ApplyDamage(gameObject.name, 500, DamageType.BRUTE);
			}
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
