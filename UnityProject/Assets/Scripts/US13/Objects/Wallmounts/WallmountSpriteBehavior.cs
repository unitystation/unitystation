using UnityEngine;
using US13.Core.Camera;
using US13.Core.Lighting_System;
using US13.Player;

namespace US13.Objects.Wallmounts
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

		// Every mask camera invokes OnWillRenderObject. Share the camera lookup across all wallmounts.
		private static int lightingLookupFrame = -1;
		private static LightingSystem lightingSystem;
		private int lastVisibilityFrame = -1;

		private static LightingSystem GetLightingSystem()
		{
			if (lightingLookupFrame == Time.frameCount) return lightingSystem;
			lightingLookupFrame = Time.frameCount;
			lightingSystem = null;
			var mainCamera = Camera.main;
			if (mainCamera != null) mainCamera.TryGetComponent(out lightingSystem);
			return lightingSystem;
		}

		protected virtual void Awake()
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			wallmountBehavior = GetComponentInParent<WallmountBehavior>();
		}

		protected virtual void OnEnable()
		{
			lastVisibilityFrame = -1;
		}

		// Handles rendering logic, only runs when this sprite is on camera
		public void OnWillRenderObject()
		{
			if (lastVisibilityFrame == Time.frameCount) return;
			lastVisibilityFrame = Time.frameCount;

			// don't run check until player is created
			if (PlayerManager.LocalPlayerObject == null || wallmountBehavior == null)
			{
				return;
			}

			var currentLightingSystem = GetLightingSystem();
			// Cameras can be absent or replaced while scenes and the local player are being initialized.
			if (currentLightingSystem == null) return;

			bool visible;
			// If the lighting system is disabled, we're probably a ghost or have the dev spawner open.
			if (!currentLightingSystem.enabled)
			{
				visible = true;
			}
			else
			{
				// recalculate if it is facing the player
				var followControl = Camera2DFollow.followControl;
				if (followControl == null || followControl.target == null) return;
				visible = wallmountBehavior.IsFacingPosition(followControl.target.position);
			}
			SetAlpha(visible ?1 : 0f);
		}

		protected virtual void SetAlpha(float alpha)
		{
			if (spriteRenderer == null) return;
			var color = spriteRenderer.color;
			if (color.a == alpha) return;
			color.a = alpha;
			spriteRenderer.color = color;
		}
	}
}
