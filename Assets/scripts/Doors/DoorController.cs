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
    private bool openTrigger = false;
    private GameObject playerOpeningIt;
    private IEnumerator coWaitOpened;
    
    private int closedLayer;
    private int openLayer;
    private int closedSortingLayer;
    private int openSortingLayer;

    public bool IsOpened;
    public bool isWindowed = false;

    void Start() {
        if (!isWindowedDoor)
        {
            closedLayer = LayerMask.NameToLayer("Door Closed");
        }
        else
        {
            closedLayer = LayerMask.NameToLayer("Windows");
        }
        closedSortingLayer = SortingLayer.NameToID("Doors Open");
        openLayer = LayerMask.NameToLayer("Door Open");
        openSortingLayer = SortingLayer.NameToID("Doors Closed");
        
        animator = gameObject.GetComponent<Animator>();
        registerTile = gameObject.GetComponent<RegisterTile>();
        if(!usingAnimator) {
            //TODO later change usingAnimator to horizontal checker (when vertical doors are done)
            horizontalAnim = gameObject.AddComponent<HorizontalDoorAnimator>();
        }
    }

    public void BoxCollToggleOn() {
        registerTile.UpdateTileType(TileType.Door);
        gameObject.layer = closedLayer;
        GetComponentInChildren<SpriteRenderer>().sortingLayerID = closedSortingLayer;
    }

    public void BoxCollToggleOff() {
        registerTile.UpdateTileType(TileType.None);
        gameObject.layer = openLayer;
        GetComponentInChildren<SpriteRenderer>().sortingLayerID = openSortingLayer;
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
    public void CmdTryOpen(GameObject playerObj) {
        if(!IsOpened && !isPerformingAction) {
            RpcOpen(playerObj);

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

    void Update(){
        if (openTrigger)
        {
            float distToTriggerPlayer = Vector3.Distance(playerOpeningIt.transform.position, transform.position);
            if (distToTriggerPlayer < 1.5f)
            {
                openTrigger = false;
                OpenAction();
            }
        }
    }

    [ClientRpc]
    public void RpcOpen(GameObject _playerOpeningIt) {
        if (_playerOpeningIt == null)
            return;
        
        openTrigger = true;
        playerOpeningIt = _playerOpeningIt;
    }

    private void OpenAction(){
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
        playerOpeningIt = null;
        if(usingAnimator) {
            animator.SetBool("open", false);
        } else {
            if(!isPerformingAction) {
                horizontalAnim.CloseDoor();
            }
        }
    }
}
