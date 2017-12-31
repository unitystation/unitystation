using System.Collections;
using System.Collections.Generic;
using InputControl;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts;
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
		SetLayer(closedLayer, closedSortingLayer);
	}

	public override void Trigger(bool state)
	{
		tempStateCache = state;
		if (waitToCheckState)
		{
			return;
		}

		if (animator == null)
		{
			waitToCheckState = true;
			return;
		}

		SetState(state);
	}

	private void SetState(bool state)
	{
		IsClosed = state;
		registerTile.IsClosed = state;
		if (state)
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
		foreach (Transform child in transform)
		{
			child.gameObject.layer = layer;
		}
	}

    [Server]
    private void DamageOnClose()
    {
        IEnumerable<HealthBehaviour> healthBehaviours = matrix.Get<HealthBehaviour>(registerTile.Position);
        foreach (HealthBehaviour healthBehaviour in healthBehaviours)
        {
            healthBehaviour.ApplyDamage(gameObject, 500, DamageType.BRUTE);
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
				Debug.LogWarning("ShutterController still failing Animator sync");
			}
		}
		else
		{
			SetState(tempStateCache);
		}
		waitToCheckState = false;
	}
}