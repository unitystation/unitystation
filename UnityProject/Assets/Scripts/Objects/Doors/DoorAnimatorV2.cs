using System;
using System.Collections;
using AddressableReferences;
using NaughtyAttributes;
using UnityEngine;
using Messages.Server;

namespace Doors
{
	public class DoorAnimatorV2 : MonoBehaviour
	{
		#region Sprite layers
		[BoxGroup("Sprite Layers"),
		 Tooltip("Game object which represents the base of this door"),
		 SerializeField]
		private GameObject doorBase = null;

		[BoxGroup("Sprite Layers"),
		 Tooltip("Game object which represents the light layer of this door"),
		 SerializeField]
		private GameObject overlaySparks = null;

		[BoxGroup("Sprite Layers"),
		 Tooltip("Game object which represents the light layer of this door"),
		 SerializeField]
		private GameObject overlayLights = null;

		[BoxGroup("Sprite Layers"),
		 Tooltip("Game object which represents the fill layer of this door"),
		 SerializeField]
		private GameObject overlayFill = null;

		[BoxGroup("Sprite Layers"),
		 Tooltip("Game object which represents the welded and effects layer for this door"),
		 SerializeField]
		private GameObject overlayWeld = null;

		[BoxGroup("Sprite Layers"),
		 Tooltip("Game object which represents the hacking panel layer for this door"),
		 SerializeField]
		private GameObject overlayHacking = null;

		[SerializeField, Tooltip("Time this door's opening animation takes")]
		private float openingAnimationTime = 0.6f;

		[SerializeField, Tooltip("Time this door's closing animation takes")]
		private float closingAnimationTime = 0.6f;

		[SerializeField, Tooltip("Time this door's denied animation takes")]
		private float deniedAnimationTime = 0.6f;

		[SerializeField, Tooltip("Time this door's warning animation takes")]
		private float warningAnimationTime = 0.6f;
		#endregion

		[SerializeField, Tooltip("Sound that plays when opening this door")]
		private AddressableAudioSource openingSFX;
		[SerializeField, Tooltip("Sound that plays when closing this door")]
		private AddressableAudioSource closingSFX;
		[SerializeField, Tooltip("Sound that plays when access is denied by this door")]
		private AddressableAudioSource deniedSFX;
		[SerializeField, Tooltip("Sound that plays when pressure warning is played by this door")]
		private AddressableAudioSource warningSFX;

		public event Action AnimationFinished;

		private SpriteHandler doorBaseHandler;
		private SpriteHandler overlaySparksHandler;
		private SpriteHandler overlayLightsHandler;
		private SpriteHandler overlayFillHandler;
		private SpriteHandler overlayWeldHandler;
		private SpriteHandler overlayHackingHandler;

		private void Awake()
		{
			doorBaseHandler = doorBase.GetComponent<SpriteHandler>();
			overlaySparksHandler = overlaySparks.GetComponent<SpriteHandler>();
			overlayLightsHandler = overlayLights.GetComponent<SpriteHandler>();
			overlayFillHandler = overlayFill.GetComponent<SpriteHandler>();
			overlayWeldHandler = overlayWeld.GetComponent<SpriteHandler>();
			overlayHackingHandler = overlayHacking.GetComponent<SpriteHandler>();
		}

		//Called on client and server
		// panelExposed and lights not hooked up into the net message yet
		public void PlayAnimation(DoorUpdateType type, bool skipAnimation, bool panelExposed = false, bool lights = true)
		{
			if (type == DoorUpdateType.Open)
			{
				StartCoroutine(PlayOpeningAnimation(skipAnimation, panelExposed));
			}
			else if (type == DoorUpdateType.Close)
			{
				StartCoroutine(PlayClosingAnimation(skipAnimation, panelExposed));
			}
			else if (type == DoorUpdateType.AccessDenied)
			{
				StartCoroutine(PlayDeniedAnimation());
			}

			else if (type == DoorUpdateType.PressureWarn)
			{
				StartCoroutine(PlayPressureWarningAnimation());
			}
		}

		public IEnumerator PlayOpeningAnimation(bool skipAnimation = false, bool panelExposed = false, bool lights = true)
		{
			if (skipAnimation == false)
			{
				if (panelExposed)
				{
					overlayHackingHandler.ChangeSprite((int)Panel.Opening, false);
				}

				if (lights)
				{
					overlayLightsHandler.ChangeSprite((int) Lights.Opening, false);
				}
				overlayFillHandler.ChangeSprite((int) DoorFrame.Opening, false);
				doorBaseHandler.ChangeSprite((int) DoorFrame.Opening, false);
				ClientPlaySound(openingSFX);
				yield return WaitFor.Seconds(openingAnimationTime);
			}

			// Change to open sprite after done opening
			if (panelExposed)
			{
				overlayHackingHandler.ChangeSprite((int)Panel.Open, false);
			}
			else
			{
				overlayHackingHandler.ChangeSprite((int) Panel.NoPanel, false);
			}

			overlayLightsHandler.ChangeSprite((int) Lights.NoLight, false);
			overlayFillHandler.ChangeSprite((int) DoorFrame.Open, false);
			doorBaseHandler.ChangeSprite((int) DoorFrame.Open, false);

			AnimationFinished?.Invoke();
		}

		public IEnumerator PlayClosingAnimation(bool skipAnimation = false, bool panelExposed = false, bool lights = true)
		{
			if (skipAnimation == false)
			{
				if (panelExposed)
				{
					overlayHackingHandler.ChangeSprite((int)Panel.Closing, false);
				}

				if (lights)
				{
					overlayLightsHandler.ChangeSprite((int) Lights.Closing, false);
				}

				overlayFillHandler.ChangeSprite((int) DoorFrame.Closing, false);
				doorBaseHandler.ChangeSprite((int) DoorFrame.Closing, false);
				ClientPlaySound(closingSFX);
				yield return WaitFor.Seconds(closingAnimationTime);
			}

			//Change to closed sprite after it is done closing
			if (panelExposed)
			{
				overlayHackingHandler.ChangeSprite((int) Panel.Closed, false);
			}
			else
			{
				overlayHackingHandler.ChangeSprite((int) Panel.NoPanel, false);
			}

			overlayLightsHandler.ChangeSprite((int) Lights.NoLight, false);
			overlayFillHandler.ChangeSprite((int) DoorFrame.Closed, false);
			doorBaseHandler.ChangeSprite((int) DoorFrame.Closed, false);

			AnimationFinished?.Invoke();
		}

		public IEnumerator PlayDeniedAnimation()
		{
			int previousLightSprite = overlayLightsHandler.CurrentSpriteIndex;
			overlayLightsHandler.ChangeSprite((int)Lights.Denied);
			ClientPlaySound(deniedSFX);
			yield return WaitFor.Seconds(deniedAnimationTime);

			if (previousLightSprite == -1) previousLightSprite = 0;
			overlayLightsHandler.ChangeSprite(previousLightSprite);

			AnimationFinished?.Invoke();
		}

		public IEnumerator PlayPressureWarningAnimation()
		{
			int previousLightSprite = overlayLightsHandler.CurrentSpriteIndex;
			overlayLightsHandler.ChangeSprite((int)Lights.PressureWarning);
			ClientPlaySound(warningSFX);
			yield return WaitFor.Seconds(warningAnimationTime);

			if (previousLightSprite == -1) previousLightSprite = 0;
			overlayLightsHandler.ChangeSprite(previousLightSprite);
			AnimationFinished?.Invoke();
		}

		private void ClientPlaySound(AddressableAudioSource sound)
		{
			if(CustomNetworkManager.IsHeadless) return;

			_ = SoundManager.PlayAtPosition(sound, gameObject.AssumedWorldPosServer());
		}

		public void TurnOffAllLights()
		{
			overlayLightsHandler.ChangeSprite((int) Lights.NoLight);
		}

		public void TurnOnBoltsLight()
		{
			overlayLightsHandler.ChangeSprite((int) Lights.BoltsLights);
		}

		public void AddWeldOverlay()
		{
			overlayWeldHandler.ChangeSprite((int) Weld.Weld);
		}

		public void RemoveWeldOverlay()
		{
			overlayWeldHandler.ChangeSprite((int) Weld.NoWeld);
		}

		public void AddPanelOverlay()
		{
			overlayHackingHandler.ChangeSprite((int) Panel.Closed);
		}

		public void RemovePanelOverlay()
		{
			overlayHackingHandler.ChangeSprite((int) Panel.NoPanel);
		}

		/// <summary>
		/// Used to call coroutines from outside monobehaviors
		/// </summary>
		/// <param name="anim"></param>
		public void RequestAnimation(IEnumerator anim)
		{
			StartCoroutine(anim);
		}
	}

	public enum Weld
	{
		NoWeld,
		Weld
	}

	public enum DoorFrame
	{
		Closed,
		Opening,
		Open,
		Closing
	}

	public enum Lights
	{
		NoLight,
		Denied,
		Opening,
		Closing,
		Emergency,
		BoltsLights,
		PressureWarning
	}

	public enum Sparks
	{
		NoSparks,
		SparkLights,
		BrokenSparks,
		DamagedSparks,
		OpenSparks
	}

	public enum Panel
	{
		NoPanel,
		Closed,
		Opening,
		Open,
		Closing
	}
}
