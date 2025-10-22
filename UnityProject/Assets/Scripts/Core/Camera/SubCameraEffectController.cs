using UnityEngine;

namespace CameraEffects
{
	public class SubCameraEffectControl : MonoBehaviour
	{
		private NightVisionCamera _nightVisionEffect;

		[Header("Settings")]
		[SerializeField]
		private GameObject minimalVisibilitySprite;


		private void OnEnable()
		{
			if (_nightVisionEffect == true) return;
			_nightVisionEffect = gameObject.AddComponent<NightVisionCamera>();
		}

		public void OnGhostSpawn()
		{
			ToggleNightVisionEffectState(false, Color.white);
		}

		public void ToggleNightVisionEffectState(bool state, Color nightVisionColour)
		{
			if (_nightVisionEffect == false) return;
			_nightVisionEffect.enabled = state;
			if(state) _nightVisionEffect.ToShaderColour = nightVisionColour;
		}

		public void NvgHasMaxedLensRadius(bool set)
		{
			if (_nightVisionEffect == false) return;
			_nightVisionEffect.HasMaxedLensRadius(set);
		}

		public void EnsureAllEffectsAreDisabled()
		{
			if (_nightVisionEffect == false) return;
			_nightVisionEffect.enabled = false;
		}
	}
}
