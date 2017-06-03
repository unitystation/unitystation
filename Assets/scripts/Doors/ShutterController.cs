using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using Matrix;
using InputControl;

public class ShutterController : ObjectTrigger {
    private Animator animator;
    private RegisterTile registerTile;

    public bool IsClosed { get; private set; }

    void Awake() {
        animator = gameObject.GetComponent<Animator>();
        registerTile = gameObject.GetComponent<RegisterTile>();

		SetLayer(LayerMask.NameToLayer("Door Closed"));
    }

	public override void Trigger(bool state) {
		//open
		if (!state) {
			IsClosed = state;
			registerTile.UpdateTileType(TileType.None);
			SetLayer(LayerMask.NameToLayer("Door Open"));
			animator.SetBool("close", false);
		}
		//close
		else {
			IsClosed = state;
			registerTile.UpdateTileType(TileType.Door);
			SetLayer(LayerMask.NameToLayer("Door Closed"));
			animator.SetBool("close", true);
		}
	}

	public void SetLayer(int layer) {
		gameObject.layer = layer;
		foreach (Transform child in transform)
		{
			child.gameObject.layer = layer;
		}
	}
}
