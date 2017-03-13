using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sprites;

public class HorizontalDoorAnimator : MonoBehaviour
{
	private DoorController doorController;
	private SpriteRenderer overlay_Lights;
	private SpriteRenderer overlay_Glass;
	private SpriteRenderer doorbase;
	private Sprite[] sprites;
	private Sprite[] overlaySprites;

	void Start()
	{
		doorController = GetComponent<DoorController>();
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
		sprites = SpriteManager.DoorSprites[doorController.doorType.ToString()];
		overlaySprites = SpriteManager.DoorSprites["overlays"];
	}

	public void OpenDoor()
	{
		doorController.isPerformingAction = true;
		StartCoroutine(_OpenDoor());
	}

	IEnumerator _OpenDoor()
	{
		doorbase.sprite = sprites[0];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[39];
		} else {
			overlay_Glass.sprite = sprites[15];
		}
		overlay_Lights.sprite = null;
		doorController.PlayOpenSound();
		yield return new WaitForSeconds(0.03f);
		overlay_Lights.sprite = overlaySprites[0];
		yield return new WaitForSeconds(0.06f);
		overlay_Lights.sprite = null;
		yield return new WaitForSeconds(0.09f);
		overlay_Lights.sprite = overlaySprites[0];
		yield return new WaitForSeconds(0.12f);
		doorbase.sprite = sprites[3];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[41];
		} else {
			overlay_Glass.sprite = sprites[17];
		}
		overlay_Lights.sprite = overlaySprites[1];
		yield return new WaitForSeconds(0.15f);
		doorbase.sprite = sprites[4];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[42];
		} else {
			overlay_Glass.sprite = sprites[21];
		}
		overlay_Lights.sprite = overlaySprites[2];
		doorController.BoxCollToggleOff();
		yield return new WaitForSeconds(0.2f);
		doorbase.sprite = sprites[5];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[43];
		} else {
			overlay_Glass.sprite = sprites[20];
		}
		overlay_Lights.sprite = overlaySprites[3];
		yield return new WaitForSeconds(0.2f);
		doorbase.sprite = sprites[6];
		overlay_Lights.sprite = overlaySprites[4];
		yield return new WaitForSeconds(0.2f);
		doorbase.sprite = sprites[7];
		overlay_Lights.sprite = null;
		yield return new WaitForSeconds(0.2f);
		doorbase.sprite = sprites[8];
		yield return new WaitForEndOfFrame();
		doorController.isPerformingAction = false;
	}

	public void CloseDoor()
	{
		doorController.isPerformingAction = true;
		StartCoroutine(_CloseDoor());
	}

	IEnumerator _CloseDoor()
	{
		doorbase.sprite = sprites[8];
		overlay_Lights.sprite = overlaySprites[5];
		yield return new WaitForSeconds(0.03f);
		doorbase.sprite = sprites[9];
		overlay_Lights.sprite = overlaySprites[4];
		doorController.PlayCloseSFXshort();
		yield return new WaitForSeconds(0.04f);
		doorController.BoxCollToggleOn();
		yield return new WaitForSeconds(0.06f);
		doorbase.sprite = sprites[10];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[43];
		} else {
			overlay_Glass.sprite = sprites[20];
		}
		overlay_Lights.sprite = overlaySprites[3];
		yield return new WaitForSeconds(0.09f);
		doorbase.sprite = sprites[11];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[42];
		} else {
			overlay_Glass.sprite = sprites[21];
		}
		overlay_Lights.sprite = overlaySprites[2];
		yield return new WaitForSeconds(0.12f);
		doorbase.sprite = sprites[12];
		if (!doorController.isWindowedDoor) {
			overlay_Glass.sprite = sprites[22];
		}
		overlay_Lights.sprite = overlaySprites[1];
		yield return new WaitForSeconds(0.15f);
		doorbase.sprite = sprites[13];
		if (doorController.isWindowedDoor) {
			overlay_Glass.sprite = overlaySprites[39];
		} else {
			overlay_Glass.sprite = sprites[15];
		}
		overlay_Lights.sprite = overlaySprites[0];
		yield return new WaitForSeconds(0.18f);
		overlay_Lights.sprite = null;
		yield return new WaitForSeconds(0.20f);
		doorbase.sprite = sprites[13];
		yield return new WaitForEndOfFrame();
		doorController.isPerformingAction = false;
	}

}
