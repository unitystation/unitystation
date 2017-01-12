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

        registerTile.tileType = TileType.Space;
    }

    public void Open() {
        IsClosed = false;
        registerTile.tileType = TileType.Space;
        animator.SetBool("close", false);
    }

    public void Close() {
        IsClosed = true;
        registerTile.tileType = TileType.Door;
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
