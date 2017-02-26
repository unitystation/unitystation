using UnityEngine;
using System.Collections;
using PlayGroup;
using Matrix;
using Sprites;

public class DoorController: Photon.PunBehaviour {
    public AudioSource openSFX;
    public AudioSource closeSFX;

    private Animator animator;
    private BoxCollider2D boxColl;
    private RegisterTile registerTile;

	public bool usingAnimator = true;
	private bool isPerformingAction = false;

	public DoorType doorType;

    private bool isOpened = false;

    public float maxTimeOpen = 5;
    private float timeOpen = 0;

	private SpriteRenderer overlay_Lights;
	private SpriteRenderer overlay_Glass;
	private SpriteRenderer doorbase;

	private Sprite[] sprites; 
	private Sprite[] overlaySprites;

    void Start() {
		
        animator = gameObject.GetComponent<Animator>();
        boxColl = gameObject.GetComponent<BoxCollider2D>();
        registerTile = gameObject.GetComponent<RegisterTile>();
		if (!usingAnimator) {
			foreach (Transform child in transform) {
				switch (child.gameObject.name) {
					case "overlay_Lights":
						overlay_Lights = child.gameObject.GetComponent<SpriteRenderer>();
						break;
					case "overlay_Glass":
						overlay_Glass = child.gameObject.GetComponent<SpriteRenderer>();
						break;
					case "doorbase":
						doorbase = child.gameObject.GetComponent<SpriteRenderer>();
						break;
				}
			}
		}

		sprites = SpriteManager.DoorSprites[doorType.ToString()];
		overlaySprites = SpriteManager.DoorSprites["overlays"];
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
		if (usingAnimator) {
			animator.SetBool("open", true);
		} else {
			if (!isPerformingAction) {
				isPerformingAction = true;
				StartCoroutine(OpenDoor());
			}
		}
    }

    [PunRPC]
    public void Close() {
        isOpened = false;
		if (usingAnimator) {
			animator.SetBool("open", false);
		} else {
			if (!isPerformingAction) {
				isPerformingAction = true;
				StartCoroutine(CloseDoor());
			}
		}
    }

	IEnumerator OpenDoor(){
		doorbase.sprite = sprites[0];
		overlay_Glass.sprite = sprites[15];
		overlay_Lights.sprite = null;
		PlayOpenSound();
		yield return new WaitForSeconds(0.03f);
		overlay_Lights.sprite = overlaySprites[0];
		yield return new WaitForSeconds(0.06f);
		overlay_Lights.sprite = null;
		yield return new WaitForSeconds(0.09f);
		overlay_Lights.sprite = overlaySprites[0];

		yield return new WaitForSeconds(0.12f);
		doorbase.sprite = sprites[3];
		overlay_Glass.sprite = sprites[17];
		overlay_Lights.sprite = overlaySprites[1];

		yield return new WaitForSeconds(0.15f);
		doorbase.sprite = sprites[4];
		overlay_Glass.sprite = sprites[21];
		overlay_Lights.sprite = overlaySprites[2];

		BoxCollToggleOff();

		yield return new WaitForSeconds(0.18f);
		doorbase.sprite = sprites[5];
		overlay_Glass.sprite = sprites[20];
		overlay_Lights.sprite = overlaySprites[3];

		yield return new WaitForSeconds(0.21f);
		doorbase.sprite = sprites[6];
		overlay_Glass.sprite = sprites[20];
		overlay_Lights.sprite = overlaySprites[4];

		yield return new WaitForSeconds(0.24f);
		doorbase.sprite = sprites[7];


		yield return new WaitForSeconds(0.27f);
		doorbase.sprite = sprites[8];
		yield return new WaitForEndOfFrame();
		isPerformingAction = false;
	}

	IEnumerator CloseDoor(){
		doorbase.sprite = sprites[8];
		overlay_Glass.sprite = overlaySprites[40];
		overlay_Lights.sprite = overlaySprites[5];
		yield return new WaitForSeconds(0.03f);
		doorbase.sprite = sprites[9];

		overlay_Glass.sprite = overlaySprites[46];
		
		overlay_Lights.sprite = overlaySprites[4];
		PlayCloseSFXshort();

		yield return new WaitForSeconds(0.04f);

		BoxCollToggleOn();

		yield return new WaitForSeconds(0.06f);
		doorbase.sprite = sprites[10];
		overlay_Glass.sprite = sprites[20];
		overlay_Lights.sprite = overlaySprites[3];

		yield return new WaitForSeconds(0.09f);
		doorbase.sprite = sprites[11];
		overlay_Glass.sprite = sprites[21];
		overlay_Lights.sprite = overlaySprites[2];

		yield return new WaitForSeconds(0.12f);
		doorbase.sprite = sprites[12];
		overlay_Glass.sprite = sprites[22];
		overlay_Lights.sprite = overlaySprites[1];

		yield return new WaitForSeconds(0.15f);
		doorbase.sprite = sprites[13];
		overlay_Glass.sprite = sprites[15];
		overlay_Lights.sprite = overlaySprites[0];

		yield return new WaitForSeconds(0.18f);
		overlay_Lights.sprite = null;
		yield return new WaitForSeconds(0.20f);
		doorbase.sprite = sprites[13];
		overlay_Glass.sprite = sprites[15];

		yield return new WaitForEndOfFrame();
		isPerformingAction = false;
	}
}
