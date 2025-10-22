using System.Collections;
using Core.Camera;
using Core.Utils;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace CameraEffects
{
	public class CameraEffectControlScript : MonoBehaviour
	{

		[Header("Effect scripts")]
		public DrunkCamera drunkCamera;
		public GreyscaleCamera greyscaleCamera;
		public GlitchEffect glitchEffect;
		public NightVisionCamera nightVisionCamera;

		public BlurryVision blurryVisionEffect;
		public ColourblindEmulation colourblindEmulationEffect;
		public HighCamera HighCamera;

		[field: SerializeField] public FlashbangCamera FlashbangCamera { get; private set; }

		[Header("Settings")]
		[SerializeField]
		private GameObject minimalVisibilitySprite;
		public Vector3 MinimalVisibilityScale { private set; get; } = new(3.5f, 3.5f, 8);


		[SerializeField] private int maxDrunkTime = 120000;
		[SerializeField] private int maxFlashTime = 25;

		private const float TIMER_INTERVAL = 1f;
		private float drunkCameraTime = 0;

		[FormerlySerializedAs("LightingSystem")] public LightingSystem lightingSystem;


		private MultiInterestBool _blindness = new MultiInterestBool(false,
			MultiInterestBool.RegisterBehaviour.RegisterFalse,
			MultiInterestBool.BoolBehaviour.ReturnOnFalse);


		public MultiInterestBool Blindness => _blindness;


		[FormerlySerializedAs("BlindFOVDistance")] public float blindFOVDistance = 0.65f;
		[FormerlySerializedAs("FullVisionFOVDistance")] public float fullVisionFOVDistance = 15;

		private Coroutine _lastFlashbangCoroutine = null;


		private SubCameraEffectControl _backgroundEffects;
		private SubCameraEffectControl _lightMaskEffects;

		public void Awake()
		{
			lightingSystem = this.GetComponent<LightingSystem>();
			lightingSystem.OnLightingSystemEnabled += InitialiseSubCameraEffects;

			_blindness.OnBoolChange.AddListener(BlindnessValue);

			if (minimalVisibilitySprite != null)
			{
				MinimalVisibilityScale = minimalVisibilitySprite.transform.localScale;
				return;
			}
			Loggy.Warning("[CameraEffectControlScript] - visibilitySprite is null! please set it from the inspector.");
		}

		public void InitialiseSubCameraEffects(bool enabled)
		{
			if (enabled == false) return;

			//Setup mask cameras for effects
			if (_backgroundEffects == false)
			{
				var backgroundRenderer = GetComponentInChildren<BackgroundRenderer>();
				if (backgroundRenderer != null)
					_backgroundEffects = backgroundRenderer.gameObject.AddComponent<SubCameraEffectControl>();
			}

			if (_lightMaskEffects == false)
			{
				var lightMaskRenderer = GetComponentInChildren<LightMaskRenderer>();
				if (lightMaskRenderer != null)
					_lightMaskEffects = lightMaskRenderer.gameObject.AddComponent<SubCameraEffectControl>();
			}

			EnsureAllEffectsAreDisabled();
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.GhostSpawned, OnGhostSpawn);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DoEffectTimeCheck);
			EventManager.RemoveHandler(Event.GhostSpawned, OnGhostSpawn);
		}

		private void OnGhostSpawn()
		{
			drunkCameraTime = 0;
			ToggleNightVisionEffectState(false, Color.white);
			ToggleGlitchEffectState(false);

			_backgroundEffects?.OnGhostSpawn();
			_lightMaskEffects?.OnGhostSpawn();
		}


		//setts the FOV to emulate blindness on the player
		public void BlindnessValue(bool isBlind)
		{
			if (isBlind)
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().lightingSystem.fovDistance = blindFOVDistance;
			}
			else
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().lightingSystem.fovDistance = fullVisionFOVDistance;
			}
		}

		[Button("[DEBUG] - Flash me!")]
		public void DebugFlashMeDaddy()
		{
			FlashEyes(5f);
		}

		public void FlashEyes(float flashTime)
		{
			if (_lastFlashbangCoroutine != null) StopCoroutine(_lastFlashbangCoroutine);
			_lastFlashbangCoroutine = StartCoroutine(FlashEyesCoroutine(flashTime));
		}

		private IEnumerator FlashEyesCoroutine(float flashTime)
		{
			FlashbangCamera.enabled = true;
			FlashbangCamera.Power = 4f;
			FlashbangCamera.SetFlashbangSoundStrength(FlashbangCamera.LOWPASS);
			yield return WaitFor.Seconds(flashTime);
			LeanTween.value(gameObject, f => FlashbangCamera.Power = f, FlashbangCamera.Power, 0, 1.9f).setEaseInOutQuad();
			LeanTween.value(gameObject, f => FlashbangCamera.SetFlashbangSoundStrength(f),
				FlashbangCamera.GetFlashbangSoundStrength(), FlashbangCamera.NO_LOWPASS, 1.9f).setEaseInOutQuad();
			yield return WaitFor.Seconds(1.91f);
			FlashbangCamera.enabled = false;
			_lastFlashbangCoroutine = null;
		}

		public void Stop()
		{
			if (_lastFlashbangCoroutine == null) return;
			StopCoroutine(_lastFlashbangCoroutine);
			_lastFlashbangCoroutine = null;
			FlashbangCamera.enabled = false;
			_lastFlashbangCoroutine = null;
		}

		public void ToggleGlitchEffectState(bool state)
		{
			glitchEffect.enabled = state;
		}

		public void ToggleNightVisionEffectState(bool state, Color nightVisionColour)
		{
			nightVisionCamera.enabled = state;
			if(state) nightVisionCamera.ToShaderColour = nightVisionColour;
			_backgroundEffects?.ToggleNightVisionEffectState(state, nightVisionColour);
		}

		public void NvgHasMaxedLensRadius(bool set)
		{
			nightVisionCamera.HasMaxedLensRadius(set);
			_backgroundEffects?.NvgHasMaxedLensRadius(set);
		}

		private void DoEffectTimeCheck()
		{
			if (drunkCameraTime > 0)
			{
				drunkCamera.enabled = true;
				drunkCameraTime --;
			}
			else
			{
				drunkCamera.enabled = false;

			}
		}

		/// <summary>
		/// Updates the size of the dim light around the player that allows him to see themselves in the dark.
		/// </summary>
		public void AdjustPlayerVisibility(Vector3 newSize, float time)
		{
			LeanTween.scale(minimalVisibilitySprite, newSize, time);
		}

		public void EnsureAllEffectsAreDisabled()
		{
			_backgroundEffects?.EnsureAllEffectsAreDisabled();
			_lightMaskEffects?.EnsureAllEffectsAreDisabled();

			//TODO: Find out a solution in the shaders why the screen inverts if both drunk and greyscale are both on
			drunkCamera.enabled = false;
			glitchEffect.enabled = false;
			nightVisionCamera.enabled = false;
			greyscaleCamera.enabled = false;
			FlashbangCamera.enabled = false;
			colourblindEmulationEffect.SetColourMode(ColourBlindMode.None);
			blurryVisionEffect.SetBlurStrength(0);
		}
	}
}
