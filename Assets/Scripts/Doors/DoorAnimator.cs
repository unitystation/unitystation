using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using Sprites;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;
namespace Doors
{
	[ExecuteInEditMode]
	public class DoorAnimator : MonoBehaviour
	{
		public DoorController doorController;
		public SpriteRenderer overlay_Lights;
		public SpriteRenderer overlay_Glass;
		public SpriteRenderer doorbase;
		public Sprite[] doorBaseSprites;
		public Sprite[] overlaySprites;
		public Sprite[] overlayLights;
		//fix replace the hardcoded animation sizes;
		public int animSize;

#if UNITY_EDITOR
		/// <summary>
		/// Via the editor check for the required child gObjs
		/// </summary>
		public void FindMembers()
		{
			foreach (Transform child in transform) {
				switch (child.gameObject.name) {
					case "doorbase":
						doorbase = child.gameObject.GetComponent<SpriteRenderer>();
						break;
					case "overlay_Glass":
						overlay_Glass = child.gameObject.GetComponent<SpriteRenderer>();
						break;
					case "overlay_Lights":
						overlay_Lights = child.gameObject.GetComponent<SpriteRenderer>();
						break;
				}
			}
		}

		/// <summary>
		/// Once all of the default sprites are added to the child Sprite Renderers
		/// then return to the inspector to connect all of the sprite arrays. ('Auto Load' button)
		/// </summary>
		public void LoadSprites()
		{
			doorController = GetComponent<DoorController>();
			animSize = doorController.doorAnimationSize;
			if (doorController.doorAnimator == null) {
				doorController.doorAnimator = this;
			}
			//loading the spritesLists from the child sprites. they are giving reference to the sprite Atlas that is being fed into the lists.
			doorBaseSprites = GetListOfSpritesFromLoadedSprite(doorbase.sprite);
			overlaySprites = GetListOfSpritesFromLoadedSprite(overlay_Glass.sprite);
			overlayLights = GetListOfSpritesFromLoadedSprite(overlay_Lights.sprite);
		}
		//getting the sprites from the resources folder using the reference sprites.
		//only works in editor, so sprites are cached before play
		public Sprite[] GetListOfSpritesFromLoadedSprite(Sprite sprite)
		{
			string basepath = AssetDatabase.GetAssetPath(sprite).Replace("Assets/Resources/", "");
			return Resources.LoadAll<Sprite>(basepath.Replace(".png", ""));
		}
#endif
		//animations
		public void AccessDenied()
		{
			doorController.isPerformingAction = true;
			SoundManager.PlayAtPosition("AccessDenied", transform.position);
			if (doorController.oppeningDirection == DoorController.OppeningDirection.Vertical) {
				StartCoroutine(SpritesPlayer(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset + 2, 1));
			}
			StartCoroutine(SpritesPlayer(overlay_Lights, overlayLights, 12, 6, true, false, true));
		}

		public void OpenDoor()
		{
			doorController.isPerformingAction = true;
			doorController.PlayOpenSound();
			StartCoroutine(SpritesPlayer(doorbase, doorBaseSprites, doorController.DoorSpriteOffset, animSize, false, true, true));
			if (doorController.oppeningDirection == DoorController.OppeningDirection.Vertical) {
				StartCoroutine(SpritesPlayer(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset, 1));
			} else {
				StartCoroutine(SpritesPlayer(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset));
			}
			StartCoroutine(SpritesPlayer(overlay_Glass, overlaySprites, doorController.DoorCoverSpriteOffset));
			//mabe the boxColliderStuff should be on the DoorController. 
			doorController.BoxCollToggleOff();
		}

		public void CloseDoor()
		{
			doorController.isPerformingAction = true;
			doorController.PlayCloseSound();
			StartCoroutine(SpritesPlayer(doorbase, doorBaseSprites, doorController.DoorSpriteOffset + animSize, animSize, false, true, true));
			if (doorController.oppeningDirection == DoorController.OppeningDirection.Vertical) {
				StartCoroutine(SpritesPlayer(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset, 1, true));
			} else {
				StartCoroutine(SpritesPlayer(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset + animSize, animSize, true));
			}
			StartCoroutine(SpritesPlayer(overlay_Glass, overlaySprites, doorController.DoorCoverSpriteOffset + 6));
			doorController.BoxCollToggleOn();
		}

		/// <summary>
		/// plays a range of sprites from a Sprite[] list starting from the int offset and stopping in a limit giving int numberOfSpritesToPlay.
		/// offset is optional zero by deafult.
		/// int numberOfSpritesToPlay is 6 by deafult, but can be changed to any positive number different from zero. 
		/// bool nullfySprite will set the sprite to null in the end of the animation.
		/// updateFov is optinal and deafult = false.
		/// updateAction is a flag that is now coupled with the doorcontroller. 
		/// </summary>
		IEnumerator SpritesPlayer(SpriteRenderer renderer, Sprite[] list, int offset = 0, int numberOfSpritesToPlay = 6, bool nullfySprite = false, bool updateFOV = false, bool updateAction = false)
		{
			if ((offset > -1) && (numberOfSpritesToPlay > 0)) {
				int limit = offset + numberOfSpritesToPlay;
				for (int i = offset; i < limit; i++) {
					renderer.sprite = list[i];
					yield return new WaitForSeconds(0.1f);
				}
				yield return new WaitForSeconds(0.1f);
				if (nullfySprite) {
					renderer.sprite = null;
				}
				if (updateFOV == true) {
					if (doorbase.isVisible)
						EventManager.Broadcast(EVENT.UpdateFov);
				}
				if (updateAction == true) {
					doorController.isPerformingAction = false;
				}
			} else {
				Debug.Log("Offset and the range of sprites must be a positive or zero.");
				yield break;
			}
		}
	}
}
