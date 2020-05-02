using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif


	[ExecuteInEditMode]
	public class AirLockAnimator : DoorAnimator
	{
		//fix replace the hardcoded animation sizes;
		public int animSize;

		public SpriteRenderer doorbase;
		public Sprite[] doorBaseSprites;
		public SpriteRenderer overlay_Glass;
		public SpriteRenderer overlay_Lights;

		public Sprite[] overlayLights;

		public Sprite[] overlaySprites;

		//animations
		public override void AccessDenied(bool skipAnimation)
		{
			if (skipAnimation)
			{
				//do nothing
				return;
			}
			doorController.isPerformingAction = true;
			SoundManager.PlayAtPosition("AccessDenied", transform.position, gameObject);

			// check if door uses a simple denied animation (flashes 1 frame on and off)
			if (doorController.useSimpleDeniedAnimation)
			{
				StartCoroutine(PlaySimpleDeniedAnim());
				// StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorDeniedSpriteOffset, animSize, true, false, true));
			}
			else
			{
				if (doorController.openingDirection == DoorController.OpeningDirection.Vertical)
				{
					StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset + 2, 1));
				}
				else
				{
					StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorDeniedSpriteOffset, animSize, true, false, true));
				}
			}
		}

		public override void PressureWarn(bool skipAnimation)
		{
			if (skipAnimation)
			{
				//do nothing
				return;
			}

			doorController.isPerformingAction = true;
			SoundManager.PlayAtPosition("TripleBeep", transform.position, gameObject, polyphonic: true, isGlobal: true);
			StartCoroutine(PlayPressureWarnAnim());
		}

		public override void OpenDoor(bool skipAnimation)
		{
			doorController.isPerformingAction = true;
			if (!skipAnimation)
			{
				doorController.PlayOpenSound();
			}

			// Opening animation.
			StartCoroutine(PlayAnim(doorbase, doorBaseSprites, doorController.DoorSpriteOffset, animSize, false, true, true, skipAnimation));

			// Light animation.
			// If the airlock has overlay_Lights sprites, play the overlay_Lights sprite animation.
			if (overlayLights.Length != 0)
			{
				// check if door uses a simple light animation (turn on 1 frame, turn it off at the end)
				if (doorController.useSimpleLightAnimation)
				{
					if (!skipAnimation)
					{
						StartCoroutine(PlaySimpleLightAnim());
					}
				}
				else
				{
					if (doorController.openingDirection == DoorController.OpeningDirection.Vertical)
					{
						StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset, 1, skipToEnd: skipAnimation));
					}
					else
					{
						StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset, animSize, true, skipToEnd: skipAnimation));
					}
				}
			}

			// Glass/cover animation.
			// If the airlock has overlay_Glass sprites, play the overlay_Glass sprite animation.
			if (overlaySprites.Length != 0)
			{
				StartCoroutine(PlayAnim(overlay_Glass, overlaySprites, doorController.DoorCoverSpriteOffset, skipToEnd: skipAnimation));
			}
			
			//mabe the boxColliderStuff should be on the DoorController.
			StartCoroutine(MakePassable(skipAnimation));
		}

		private IEnumerator MakePassable(bool instant) {
			if (instant)
			{
				yield return WaitFor.EndOfFrame;
			}
			else
			{
				yield return WaitFor.Seconds(0.15f);
			}
			doorController.BoxCollToggleOff();
		}

		public override void CloseDoor(bool skipAnimation)
		{
			doorController.isPerformingAction = true;
			if (!skipAnimation)
			{
				doorController.PlayCloseSound();
			}

			// Closing animation.
			StartCoroutine(PlayAnim(doorbase, doorBaseSprites, doorController.DoorSpriteOffset + animSize, animSize, false, true, true, skipAnimation));

			// Light animation.
			// If the airlock has overlay_Lights sprites, play the overlay_Lights sprite animation.
			if (overlayLights.Length != 0)
			{
				// check if door uses a simple light animation (turn on 1 frame, turn it off at the end)
				if (doorController.useSimpleLightAnimation)
				{
					if (!skipAnimation)
					{
						StartCoroutine(PlaySimpleLightAnim());
					}
				}
				else
				{
					if (doorController.openingDirection == DoorController.OpeningDirection.Vertical)
					{
						StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset, 1, true, skipToEnd: skipAnimation));
					}
					else
					{
						StartCoroutine(PlayAnim(overlay_Lights, overlayLights, doorController.DoorLightSpriteOffset + animSize, animSize, true, skipToEnd: skipAnimation));
					}
				}
			}

			// Glass/cover animation.
			// If the airlock has overlay_Glass sprites, play the overlay_Glass sprite animation.
			if (overlaySprites.Length != 0)
			{
				StartCoroutine(PlayAnim(overlay_Glass, overlaySprites, doorController.DoorCoverSpriteOffset + 6, skipToEnd: skipAnimation));
			}
			
			StartCoroutine(MakeSolid(skipAnimation));
		}

		private IEnumerator MakeSolid(bool instant) {
			if (instant)
			{
				yield return WaitFor.EndOfFrame;
			}
			else
			{
				yield return WaitFor.Seconds( 0.15f );
			}
			doorController.BoxCollToggleOn();
		}

		/// <summary>
		///     plays a range of sprites from a Sprite[] list starting from the int offset and stopping in a limit giving int
		///     numberOfSpritesToPlay.
		///     offset is optional zero by deafult.
		///     int numberOfSpritesToPlay is 6 by deafult, but can be changed to any positive number different from zero.
		///     bool nullfySprite will set the sprite to null in the end of the animation.
		///     updateFov is optinal and deafult = false.
		///     updateAction is a flag that is now coupled with the doorcontroller.
		/// </summary>
		private IEnumerator PlayAnim(SpriteRenderer renderer, Sprite[] list, int offset = 0, int numberOfSpritesToPlay = 6, bool nullfySprite = false,
			bool updateFOV = false, bool updateAction = false, bool skipToEnd = false)
		{
			if (offset > -1 && numberOfSpritesToPlay > 0)
			{
				int limit = offset + numberOfSpritesToPlay;
				if (skipToEnd)
				{
					renderer.sprite = list[limit - 1];
				}
				else
				{
					for (int i = offset; i < limit; i++)
					{
						renderer.sprite = list[i];
						yield return WaitFor.Seconds(0.1f);
					}
				}
				yield return WaitFor.Seconds(0.1f);
				if (nullfySprite)
				{
					renderer.sprite = null;
				}
				if (updateFOV)
				{
					if (doorbase.isVisible)
					{
						EventManager.Broadcast(EVENT.UpdateFov);
					}
				}
				if (updateAction)
				{
					doorController.OnAnimationFinished();
				}
			}
			else
			{
				Logger.Log("Offset and the range of sprites must be a positive or zero.", Category.Doors);
			}
		}

		/// <summary>
		///     plays 1 frame for the light animation instead of 6
		///		uses 1 frame from the overlayLights sprite sheet which is specified with the DoorLightSpriteOffset, then nullifies it at the end
		/// </summary>
		private IEnumerator PlaySimpleLightAnim()
		{
			overlay_Lights.sprite = overlayLights[doorController.DoorLightSpriteOffset];
			yield return WaitFor.Seconds(0.6f);
			overlay_Lights.sprite = null;
		}

		/// <summary>
		///     plays 1 frame for the denied animation instead of 6
		///		flashes a frame from the overlayLights sprite sheet which is specified with the DoorDeniedSpriteOffset, then nullifies it at the end
		/// </summary>
		private IEnumerator PlaySimpleDeniedAnim()
		{
			bool light = false;
			for (int i = 0; i < animSize; i++)
			{

				if (!light)
				{
					overlay_Lights.sprite = overlayLights[doorController.DoorDeniedSpriteOffset];
				}
				else
				{
					overlay_Lights.sprite = null;

				}
				light = !light;
				yield return WaitFor.Seconds(0.05f);
			}
			overlay_Lights.sprite = null;
			doorController.isPerformingAction = false;
		}

		/// <summary>
        /// Flashes the door's emergency access (yellow) lights several times,
        /// or the door bolts, depending on the pressure difference over the door.
        /// Sprite offset varies depending on door type - set in each door prefab.
        /// </summary>
        /// <returns></returns>
		private IEnumerator PlayPressureWarnAnim()
		{
			int flashCount = 3;

			// Choose emergency lights sprite, overwrite with door bolt lights if
			// pressureLevel is Warning and not Caution.
			int spriteOffset = doorController.DoorPressureSpriteOffset;
			if (doorController.pressureLevel == DoorController.PressureLevel.Warning)
			{
				spriteOffset = doorController.DoorDeniedSpriteOffset;
			}
			var sprite = overlayLights[spriteOffset];

			for (int i = 0; i < flashCount; i++)
			{
				overlay_Lights.sprite = sprite;
				yield return WaitFor.Seconds(0.1f);
				overlay_Lights.sprite = null;
				yield return WaitFor.Seconds(0.1f);
			}

			doorController.isPerformingAction = false;
		}

#if UNITY_EDITOR
		/// <summary>
		///     Via the editor check for the required child gObjs
		/// </summary>
		public void FindMembers()
		{
			foreach (Transform child in transform)
			{
				switch (child.gameObject.name)
				{
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
		///     Once all of the default sprites are added to the child Sprite Renderers
		///     then return to the inspector to connect all of the sprite arrays. ('Auto Load' button)
		/// </summary>
		public void LoadSprites()
		{
			doorController = GetComponent<DoorController>();
			animSize = doorController.doorAnimationSize;
			if (doorController.doorAnimator == null)
			{
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
			// If the sprite renderer has no sprite applied, return null.
			// Otherwise every sprite in "Assets/Resources/" will be returned.
			if (AssetDatabase.GetAssetPath(sprite).Equals(""))
			{
				return null;
			}

			string basepath = AssetDatabase.GetAssetPath(sprite).Replace("Assets/Resources/", "");
			return Resources.LoadAll<Sprite>(basepath.Replace(".png", ""));
		}
#endif
	}
