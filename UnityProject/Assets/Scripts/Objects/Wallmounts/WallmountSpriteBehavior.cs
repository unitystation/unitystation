using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects.Wallmounts
{
	/// <summary>
	/// Behavior for wallmount sprites.
	///
	/// Looks at the way a wallmount is facing by using the parent gameobject's Directional.CurrentDirection.
	///
	/// Wallmount sprites are occluded like all the other objects, however they should only be visible on the
	/// side of the wall that they are on. The occlusion logic keeps the nearest wall visible so we must
	/// check which side of the wall the mount is on so we can hide it if it's on the wrong side.
	/// This behavior makes the wallmount invisible if it is not facing towards the player.
	/// </summary>
	[DisallowMultipleComponent]
	public class WallmountSpriteBehavior : MonoBehaviour
	{
		// This sprite's renderer
		private SpriteRenderer spriteRenderer;
		//parent wallmount behavior
		private WallmountBehavior wallmountBehavior;

		private LightingSystem lightingSystem;
		private LightingSystem LightingSystem {
			get {
				if (this.lightingSystem == null && Camera.main.TryGetComponent(out LightingSystem lightingSystem))
				{
					this.lightingSystem = lightingSystem;
				}

				return this.lightingSystem;
			}
		}

		public void Awake()
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			wallmountBehavior = GetComponentInParent<WallmountBehavior>();
		}

		// Handles rendering logic, only runs when this sprite is on camera
		public void OnWillRenderObject()
		{
			// don't run check until player is created
			if (PlayerManager.LocalPlayer == null || wallmountBehavior == null)
			{
				return;
			}

			bool visible;
			// If the lighting system is disabled, we're probably a ghost or have the dev spawner open.
			if (!LightingSystem.enabled)
			{
				visible = true;
			}
			else
			{
				// recalculate if it is facing the player
				visible = wallmountBehavior.IsFacingPosition(Camera2DFollow.followControl.target.position);
			}
			SetAlpha(visible ? 1 : 0);
		}

		protected virtual void SetAlpha(int alpha)
		{
			spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
		}
	}
}
