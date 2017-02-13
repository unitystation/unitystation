using UnityEngine;
using System.Collections;
using PlayGroup;
using Matrix;

public class DoorController: Photon.PunBehaviour {
    public AudioSource openSFX;
    public AudioSource closeSFX;

    private Animator animator;
    private BoxCollider2D boxColl;
    private RegisterTile registerTile;

    private bool isOpened = false;

    public float maxTimeOpen = 5;
    private float timeOpen = 0;

    void Start() {
        animator = gameObject.GetComponent<Animator>();
        boxColl = gameObject.GetComponent<BoxCollider2D>();
        registerTile = gameObject.GetComponent<RegisterTile>();
    }

    void Update() {
        waitUntilClose();
    }

    public void BoxCollToggleOn() {
        registerTile.UpdateTileType(TileType.Door);
        boxColl.enabled = true;
    }

    public void BoxCollToggleOff() {
        registerTile.UpdateTileType(TileType.None);
        boxColl.enabled = false;
    }

    private void waitUntilClose() {
        if(isOpened) { //removed numOccupies condition for time being
            timeOpen += Time.deltaTime;

            if(timeOpen >= maxTimeOpen) {
				TryClose();
            }
        } else {
            timeOpen = 0;
        }
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

    void OnMouseDown() {
        if(PlayerManager.PlayerInReach(transform)) {
            if(isOpened) {
                photonView.RPC("Close", PhotonTargets.All);
            } else {
                photonView.RPC("Open", PhotonTargets.All);
            }
        }
    }

	public void TryOpen(){
		if (PhotonNetwork.connectedAndReady) {
			photonView.RPC("Open", PhotonTargets.All, null);
		} else {
			Open();
		}
	}

	public void TryClose(){
		if (PhotonNetwork.connectedAndReady) {
			photonView.RPC("Close", PhotonTargets.All, null);
		} else {
			Close();
		}
	}

    [PunRPC]
    public void Open() {
        isOpened = true;
        animator.SetBool("open", true);
    }

    [PunRPC]
    public void Close() {
        isOpened = false;
        animator.SetBool("open", false);
    }
}
