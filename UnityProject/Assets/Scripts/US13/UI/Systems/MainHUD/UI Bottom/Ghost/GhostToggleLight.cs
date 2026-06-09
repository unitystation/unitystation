using UnityEngine;
using UnityEngine.UI;
using US13.Core.Camera;

namespace US13.UI.Systems.MainHUD.UI_Bottom.Ghost
{
	/// <summary>
	/// UI element to control ghost FOV and lighting vision
	/// </summary>
	[RequireComponent(typeof(Image))]
	public class GhostToggleLight : MonoBehaviour
	{
		public Sprite lightOnSprite;
		public Sprite lightOffSprite;

		[SerializeField]
		private Image image = null;

		private LightingSystem lighting;
		/// <summary>
		/// Safely resolve lighting system dependency
		/// </summary>
		private LightingSystem LightingSys
		{
			get
			{
				// Already have lighting link?
				if (lighting)
					return lighting;

				// Check camera main
				if (Camera.main == null)
				{
					//means that the client may of logged out
					//and the scene is in between loading
					return null;
				}

				// Get the lighting system
				lighting = Camera.main.GetComponent<LightingSystem>();
				return lighting;
			}
		}

		private void OnEnable()
		{
			if (!LightingSys)
				return;

			// subscribe to lighting system change (can be disabled from other systems, like admin spawn)
			LightingSys.OnLightingSystemEnabled += OnLightingSystemEnabled;
			// update sprite
			UpdateSprite();
		}

		private void OnDisable()
		{
			if (!LightingSys)
				return;

			// unsubscribe from lighting system
			LightingSys.OnLightingSystemEnabled -= OnLightingSystemEnabled;
			CameraEffectControlScript.Instance.Xray.RemovePosition(this.gameObject);
		}

		/// <summary>
		/// Toggle on/off lighting system
		/// </summary>
		public void OnLightTogglePressed()
		{
			if (LightingSys == null)
				return;

			// Change lighting system state to opposite
			// Image sprite will change by event
			var isLighitingEnabled = LightingSys.enabled;
			if (LightingSys.enabled && CameraEffectControlScript.Instance.Xray.HasPosition(this.gameObject) == false)
			{
				CameraEffectControlScript.Instance.Xray.RecordPosition(this.gameObject, true);
			}
			else
			{
				if (LightingSys.enabled)
				{
					CameraEffectControlScript.Instance.Xray.RemovePosition(this.gameObject);
					LightingSys.enabled = false;
				}
				else
				{
					CameraEffectControlScript.Instance.Xray.RemovePosition(this.gameObject);
					LightingSys.enabled = true;
				}

			}


		}

		private void OnLightingSystemEnabled(bool isEnabled)
		{
			UpdateSprite();
		}

		/// <summary>
		/// Validate that sprite shows correct state of lighting system
		/// </summary>
		private void UpdateSprite()
		{
			image.sprite = LightingSys.enabled ? lightOnSprite : lightOffSprite;
		}

	}
}
