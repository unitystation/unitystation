﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShutterController : ObjectTrigger
{
	private RegisterDoor registerTile;
	private Matrix matrix => registerTile.Matrix;
	private Animator animator;

	private int closedLayer;
	private int closedSortingLayer;
	private int openLayer;
	private int openSortingLayer;

	private bool tempStateCache;

	//For network sync reliability
	private bool waitToCheckState;

	public bool IsClosed { get; private set; }

	private void Awake()
	{
		animator = gameObject.GetComponent<Animator>();
		registerTile = gameObject.GetComponent<RegisterDoor>();

		closedLayer = LayerMask.NameToLayer("Door Closed");
		closedSortingLayer = SortingLayer.NameToID("Doors Open");
		openLayer = LayerMask.NameToLayer("Door Open");
		openSortingLayer = SortingLayer.NameToID("Doors Closed");
		SetLayer(openLayer,openSortingLayer);
	}

	public void Start(){
		gameObject.SendMessage("TurnOffDoorFov", null, SendMessageOptions.DontRequireReceiver);
	}

	public override void Trigger(bool iState)
	{
		tempStateCache = iState;
		if (waitToCheckState)
		{
			return;
		}

		if (animator == null)
		{
			waitToCheckState = true;
			return;
		}

		SetState(iState);
	}

	private void SetState(bool state)
	{
		IsClosed = state;
		registerTile.IsClosed = state;
		if (state)
		{
			gameObject.SendMessage("TurnOnDoorFov", null, SendMessageOptions.DontRequireReceiver);
			SetLayer(closedLayer, closedSortingLayer);
		//	gameObject.SendMessage("TurnOnDoorFov");
			if (isServer)
			{
				DamageOnClose();
			}
		}
		else
		{
			SetLayer(openLayer, openSortingLayer);
			gameObject.SendMessage("TurnOffDoorFov", null, SendMessageOptions.DontRequireReceiver);
			//gameObject.SendMessage("TurnOffDoorFov");
		}

		animator.SetBool("close", state);
	}

	public void SetLayer(int layer, int sortingLayer)
	{
		//		GetComponentInChildren<SpriteRenderer>().sortingLayerID = sortingLayer;
		gameObject.layer = layer;
		foreach (Transform child in transform)
		{
			child.gameObject.layer = layer;
		}
	}

    [Server]
    private void DamageOnClose() {
	    var healthBehaviours = matrix.Get<HealthBehaviour>(registerTile.Position);
	    for ( var i = 0; i < healthBehaviours.Count; i++ ) {
		    HealthBehaviour healthBehaviour = healthBehaviours[i];
		    healthBehaviour.ApplyDamage( gameObject, 500, DamageType.BRUTE );
	    }
    }

	//Handle network spawn sync failure
	private IEnumerator WaitToTryAgain()
	{
		yield return new WaitForSeconds(0.2f);
		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>();
			if (animator != null)
			{
				SetState(tempStateCache);
			}
			else
			{
				Logger.LogWarning("ShutterController still failing Animator sync", Category.Shutters);
			}
		}
		else
		{
			SetState(tempStateCache);
		}
		waitToCheckState = false;
	}
}