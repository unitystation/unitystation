using UnityEngine;

namespace US13.Core.Camera
{
	public class SubCameraEffectControl : MonoBehaviour
	{
		private NightVisionCamera _nightVisionEffect;
		private NoirCamera _noirEffect;
		private NightEyesCamera _nightEyesEffect;

		[Header("Settings")]
		[SerializeField]
		private GameObject minimalVisibilitySprite;


		private void OnEnable()
		{
			if (_nightVisionEffect == false) _nightVisionEffect = gameObject.AddComponent<NightVisionCamera>();
			if(_noirEffect == false) _noirEffect = gameObject.AddComponent<NoirCamera>();
			if(_nightEyesEffect == false) _nightEyesEffect = gameObject.AddComponent<NightEyesCamera>();
		}

		public void OnGhostSpawn()
		{
			ToggleNightVisionEffectState(false, Color.white);
			ToggleNightEyesState(false);
			ToggleNoirEffectState(false);
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

		public void ToggleNightEyesState(bool state)
		{
			if (_nightEyesEffect == false) return;
			_nightEyesEffect.enabled = state;
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
			if(_nightEyesEffect) _nightEyesEffect.enabled = false;
		}
	}
}
