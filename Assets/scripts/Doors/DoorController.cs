using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using PlayGroup;
using Matrix;
using Sprites;

public class DoorController: NetworkBehaviour {
    public AudioSource openSFX;
    public AudioSource closeSFX;
    private Animator animator;
    private RegisterTile registerTile;
    public bool usingAnimator = true;
    public bool isWindowedDoor = false;
    [HideInInspector]
    public bool isPerformingAction = false;
    public DoorType doorType;
    public float maxTimeOpen = 5;
    private HorizontalDoorAnimator horizontalAnim;

    private IEnumerator coWaitOpened;

    public bool IsOpened;

    void Start() {
        animator = gameObject.GetComponent<Animator>();
        registerTile = gameObject.GetComponent<RegisterTile>();
        if(!usingAnimator) {
            //TODO later change usingAnimator to horizontal checker (when vertical doors are done)
            horizontalAnim = gameObject.AddComponent<HorizontalDoorAnimator>();
        }
    }

    public void BoxCollToggleOn() {
        registerTile.UpdateTileType(TileType.Door);
        gameObject.layer = LayerMask.NameToLayer("Door Closed");
    }

    public void BoxCollToggleOff() {
        registerTile.UpdateTileType(TileType.None);
        gameObject.layer = LayerMask.NameToLayer("Door Open");
    }

    private IEnumerator WaitUntilClose() {
        // After the door opens, wait until it's supposed to close.
        yield return new WaitForSeconds(maxTimeOpen);
        if(isServer)
            CmdTryClose();
    }

    //3d sounds
    public void PlayOpenSound() {
        if(openSFX != null)
            openSFX.Play();
    }

    public void PlayCloseSound() {
        if(closeSFX != null)
            closeSFX.Play();
    }

    public void PlayCloseSFXshort() {
        if(closeSFX != null) {
            closeSFX.time = 0.6f;
            closeSFX.Play();
        }
    }

    [Command]
    public void CmdTryOpen() {
        if(!IsOpened && !isPerformingAction) {
            RpcOpen();

            ResetWaiting();
        }
    }

    [Command]
    public void CmdTryClose() {
		if (IsOpened && !isPerformingAction && !Matrix.Matrix.At(transform.position).IsPlayer() &&
		   !Matrix.Matrix.At(transform.position).IsObject()) {
			RpcClose();
		} else {
			ResetWaiting();
		}
    }

    private void ResetWaiting() {
        if(coWaitOpened != null) {
            StopCoroutine(coWaitOpened);
            coWaitOpened = null;
        }

        coWaitOpened = WaitUntilClose();
        StartCoroutine(coWaitOpened);
    }

    [ClientRpc]
    public void RpcOpen() {
        IsOpened = true;

        if(usingAnimator) {
            animator.SetBool("open", true);
        } else {
            if(!isPerformingAction) {
                horizontalAnim.OpenDoor();
            }
        }
    }

    [ClientRpc]
    public void RpcClose() {
        IsOpened = false;
        if(usingAnimator) {
            animator.SetBool("open", false);
        } else {
            if(!isPerformingAction) {
                horizontalAnim.CloseDoor();
            }
        }
    }
}
