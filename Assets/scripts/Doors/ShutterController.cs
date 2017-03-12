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
    }

    public void Open() {
        IsClosed = false;
        registerTile.UpdateTileType(TileType.None);
        animator.SetBool("close", false);
    }

    public void Close() {
        IsClosed = true;
        registerTile.UpdateTileType(TileType.Door);
        animator.SetBool("close", true);
    }

    public void SyncState(bool isClosed) {
        IsClosed = isClosed;

        animator.SetBool("close", IsClosed);

        if(IsClosed) {
            animator.Play("04_shuttersClose", -1, 1);
        } else {
            animator.Play("04_shuttersOpen", -1, 1);
        }

    }
}
