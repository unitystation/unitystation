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
}
