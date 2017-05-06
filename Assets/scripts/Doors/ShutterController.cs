using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using Matrix;

public class ShutterController : MonoBehaviour {
    private Animator animator;
    private RegisterTile registerTile;

    public bool IsClosed { get; private set; }

    void Start() {
        animator = gameObject.GetComponent<Animator>();
        registerTile = gameObject.GetComponent<RegisterTile>();

        registerTile.UpdateTileType(TileType.None);
        gameObject.layer = LayerMask.NameToLayer("Door Open");
    }

    public void Open() {
        IsClosed = false;
        registerTile.UpdateTileType(TileType.None);
        gameObject.layer = LayerMask.NameToLayer("Door Open");
        animator.SetBool("close", false);
    }

    public void Close() {
        IsClosed = true;
        registerTile.UpdateTileType(TileType.Door);
        gameObject.layer = LayerMask.NameToLayer("Door Closed");
        animator.SetBool("close", true);
    }
}
