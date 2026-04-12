using UnityEngine;

namespace US13.Core.Camera
{
	public class SubCameraEffectControl : MonoBehaviour
	{
		private NightVisionCamera _nightVisionEffect;
		private NoirCamera _noirEffect;

		[Header("Settings")]
		[SerializeField]
		private GameObject minimalVisibilitySprite;


		private void OnEnable()
		{
			if (_nightVisionEffect == false) _nightVisionEffect = gameObject.AddComponent<NightVisionCamera>();
			if(_noirEffect == false) _noirEffect = gameObject.AddComponent<NoirCamera>();
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

		public void ToggleNoirEffectState(bool state)
		{
			if (_noirEffect == false) return;
			_noirEffect.enabled = state;
		}

		public void NvgHasMaxedLensRadius(bool set)
		{
			if (_nightVisionEffect == false) return;
			_nightVisionEffect.HasMaxedLensRadius(set);
		}

		public void EnsureAllEffectsAreDisabled()
		{
			if (_nightVisionEffect) _nightVisionEffect.enabled = false;
			if(_noirEffect) _noirEffect.enabled = false;
		}
	}
}
