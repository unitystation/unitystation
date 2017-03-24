using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using PlayGroup;
using Matrix;
using Sprites;

public class DoorController: NetworkBehaviour
{
	public AudioSource openSFX;
	public AudioSource closeSFX;
	private Animator animator;
	private BoxCollider2D boxColl;
	private RegisterTile registerTile;
	public bool usingAnimator = true;
	public bool isWindowedDoor = false;
	[HideInInspector]
	public bool isPerformingAction = false;
	public DoorType doorType;
	public float maxTimeOpen = 5;
	private float timeOpen = 0;
	private HorizontalDoorAnimator horizontalAnim;

    public bool IsOpened { get; private set; }

	void Start()
	{
		animator = gameObject.GetComponent<Animator>();
		boxColl = gameObject.GetComponent<BoxCollider2D>();
		registerTile = gameObject.GetComponent<RegisterTile>();
		if (!usingAnimator) {
			//TODO later change usingAnimator to horizontal checker (when vertical doors are done)
			horizontalAnim = gameObject.AddComponent<HorizontalDoorAnimator>();
		}
	}
		
	public void BoxCollToggleOn()
	{
		registerTile.UpdateTileType(TileType.Door);
		boxColl.enabled = true;
	}

	public void BoxCollToggleOff()
	{
		registerTile.UpdateTileType(TileType.None);
		boxColl.enabled = false;
	}

	private IEnumerator _WaitUntilClose()
	{
		// After the door opens, wait until it's supposed to close.
		yield return new WaitForSeconds(maxTimeOpen);
		if(isServer)
		CmdTryClose();
	}

	//3d sounds
	public void PlayOpenSound()
	{
		if (openSFX != null)
			openSFX.Play();
	}

	public void PlayCloseSound()
	{
		if (closeSFX != null)
			closeSFX.Play();
	}

	public void PlayCloseSFXshort()
	{
		if (closeSFX != null) {
			closeSFX.time = 0.6f;
			closeSFX.Play();
		}
	}

	[Command]
	public void CmdTryOpen()
	{
		RpcOpen();
		StartCoroutine(_WaitUntilClose());
	}

	[Command]
	public void CmdTryClose()
	{
		RpcClose();
	}

	[ClientRpc]
	public void RpcOpen()
	{
        IsOpened = true;
	
		if (usingAnimator) {
			animator.SetBool("open", true);
		} else {
			if (!isPerformingAction) {
				horizontalAnim.OpenDoor();
			}
		}
	}

	[ClientRpc]
	public void RpcClose()
	{
        IsOpened = false;
		if (usingAnimator) {
			animator.SetBool("open", false);
		} else {
			if (!isPerformingAction) {
				horizontalAnim.CloseDoor();
			}
		}
	}
}
