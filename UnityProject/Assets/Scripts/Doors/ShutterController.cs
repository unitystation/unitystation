using System.Collections;
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
		// Initialize animator and Register Door components
		animator = gameObject.GetComponent<Animator>();
		registerTile = gameObject.GetComponent<RegisterDoor>();

		closedLayer = LayerMask.NameToLayer("Door Closed");
		closedSortingLayer = SortingLayer.NameToID("Doors Open");
		openLayer = LayerMask.NameToLayer("Door Open");
		openSortingLayer = SortingLayer.NameToID("Doors Closed");
	}

	public void Start()
	{
		// Close or open the door depending on Register Door Script's "Is Closed" flag at object creation
		Trigger(registerTile.IsClosed);

		// If the animator has not yet initialized for the object, try again
		if (waitToCheckState)
		{
			WaitToTryAgain();
		}
	}

	public override void Trigger(bool isClosed)
	{
		if (waitToCheckState)
		{
			return;
		}

		if (animator == null)
		{
			// Store isClosed as a property for non-initialized animator
			tempStateCache = isClosed;

			waitToCheckState = true;
			return;
		}

		SetState(isClosed);
	}

	/// <summary>
	/// Sets the state of Shutter object to open or closed depending on isClosed parameter.
	/// This will start the relevant animation loop as well as change the Shutter's layers.
	/// </summary>
	/// <param name="isClosed">The state to set the door (Open, Closed)</param>
	private void SetState(bool isClosed)
	{
		this.IsClosed = isClosed;
		registerTile.IsClosed = isClosed;

		// Start animation change process
		animator.SetBool("close", isClosed);

		if (isClosed)
		{
			gameObject.SendMessage("TurnOnDoorFov", null, SendMessageOptions.DontRequireReceiver);
			SetLayer(closedLayer, closedSortingLayer);

			// Damage living objects underneath shutter as it closes
			if (isServer)
			{
				DamageOnClose();
			}
		}
		else
		{
			SetLayer(openLayer, openSortingLayer);
			gameObject.SendMessage("TurnOffDoorFov", null, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void SetLayer(int layer, int sortingLayer)
	{
		gameObject.layer = layer;
		foreach (Transform child in transform)
		{
			child.gameObject.layer = layer;
		}
	}

	[Server]
	private void DamageOnClose()
	{
		var healthBehaviours = matrix.Get<LivingHealthBehaviour>(registerTile.Position);
		for (var i = 0; i < healthBehaviours.Count; i++)
		{
			LivingHealthBehaviour healthBehaviour = healthBehaviours[i];
			healthBehaviour.ApplyDamage(gameObject, 500, DamageType.Brute);
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