using System.Collections;
using System.Collections.Generic;
using InputControl;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

public class ShutterController : ObjectTrigger
{
	private Matrix _matrix;
	private RegisterDoor _registerTile;
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
		_registerTile = gameObject.GetComponent<RegisterDoor>();
		_matrix = Matrix.GetMatrix(this);

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
		_registerTile.IsClosed = state;
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
        IEnumerable<HealthBehaviour> healthBehaviours = _matrix.Get<HealthBehaviour>(_registerTile.Position);
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