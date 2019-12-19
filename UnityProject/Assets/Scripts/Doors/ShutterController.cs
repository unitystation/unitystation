using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ShutterController : ObjectTrigger
{
	private RegisterDoor registerTile;
	private Matrix matrix => registerTile.Matrix;
	private Animator animator;

	private int closedLayer;
	private int closedSortingLayer;
	private int openLayer;
	private int openSortingLayer;

	[SyncVar(hook = "SetState")] private bool closedState;

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

	public override void OnStartServer()
	{
		base.OnStartServer();
		closedState = registerTile.IsClosed;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SetState(closedState);
	}

	public override void Trigger(bool isClosed)
	{
		closedState = isClosed;
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
		foreach ( LivingHealthBehaviour healthBehaviour in matrix.Get<LivingHealthBehaviour>(registerTile.LocalPositionServer, true) )
		{
			healthBehaviour.ApplyDamage(gameObject, 500, AttackType.Melee, DamageType.Brute);
		}
	}
}