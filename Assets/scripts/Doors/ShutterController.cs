using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ShutterController : MonoBehaviour {
    private Animator animator;

    public bool IsClosed { get; private set; }

    void Start() {
        animator = gameObject.GetComponent<Animator>();
    }

    public void Open() {
        IsClosed = false;
        animator.SetBool("close", false);
    }

    public void Close() {
        IsClosed = true;
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
