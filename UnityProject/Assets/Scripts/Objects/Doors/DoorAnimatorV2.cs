using System;
using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Core.Editor.Attributes;
using AddressableReferences;
using Audio.Managers;
using Messages.Server;
using Messages.Server.SoundMessages;


namespace Doors
{
	public class DoorAnimatorV2 : MonoBehaviour
	{
		#region Sprite layers
		[SerializeField, BoxGroup("Sprite Layers") ]
		[Tooltip("Game object which represents the base of this door")]
		private GameObject doorBase = null;
		public GameObject DoorBase => doorBase;

		[SerializeField, BoxGroup("Sprite Layers") ]
		[Tooltip("Game object which represents the light layer of this door")]
		private GameObject overlaySparks = null;
		public GameObject OverlaySparks => overlaySparks;

		[SerializeField, BoxGroup("Sprite Layers") ]
		[Tooltip("Game object which represents the light layer of this door")]
		private GameObject overlayLights = null;
		public GameObject OverlayLights => overlayLights;

		[SerializeField, BoxGroup("Sprite Layers") ]
		[Tooltip("Game object which represents the fill layer of this door")]
		private GameObject overlayFill = null;
		public GameObject OverlayFill => overlayFill;

		[SerializeField, BoxGroup("Sprite Layers") ]
		[Tooltip("Game object which represents the welded and effects layer for this door")]
		private GameObject overlayWeld = null;
		public GameObject OverlayWeld => overlayWeld;

		[SerializeField, BoxGroup("Sprite Layers") ]
		[Tooltip("Game object which represents the hacking panel layer for this door")]
		private GameObject overlayHacking = null;
		public GameObject OverlayHacking => overlayHacking;

		[SerializeField ]
		[Tooltip("Time this door's opening animation takes")]
		private float openingAnimationTime = 0.6f;

		[SerializeField ]
		[Tooltip("Time this door's closing animation takes")]
		private float closingAnimationTime = 0.6f;

		[SerializeField ]
		[Tooltip("Time this door's denied animation takes")]
		private float deniedAnimationTime = 0.6f;

		[SerializeField ]
		[Tooltip("Time this door's warning animation takes")]
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

		private int previousLightSprite = -1;


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
			previousLightSprite = (int) Lights.NoLight;
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
					previousLightSprite = (int) Lights.Closing;
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
			previousLightSprite = (int) Lights.NoLight;
			overlayFillHandler.ChangeSprite((int) DoorFrame.Closed, false);
			doorBaseHandler.ChangeSprite((int) DoorFrame.Closed, false);

			AnimationFinished?.Invoke();
		}

		public IEnumerator PlayDeniedAnimation()
		{
			if (previousLightSprite == -1)
			{
				previousLightSprite = overlayLightsHandler.CurrentSpriteIndex;
			}
			overlayLightsHandler.ChangeSprite((int)Lights.Denied);
			yield return WaitFor.Seconds(deniedAnimationTime);

			if (previousLightSprite == -1) previousLightSprite = 0;
			overlayLightsHandler.ChangeSprite(previousLightSprite);
			previousLightSprite = -1;
			AnimationFinished?.Invoke();
		}

		public IEnumerator PlayPressureWarningAnimation()
		{
			if (previousLightSprite == -1)
			{
				previousLightSprite = overlayLightsHandler.CurrentSpriteIndex;
			}
			overlayLightsHandler.ChangeSprite((int)Lights.PressureWarning);
			yield return WaitFor.Seconds(warningAnimationTime);

			if (previousLightSprite == -1) previousLightSprite = 0;
			overlayLightsHandler.ChangeSprite(previousLightSprite);
			previousLightSprite = -1;
			AnimationFinished?.Invoke();
		}

		private void ClientPlaySound(AddressableAudioSource sound)
		{
			if(CustomNetworkManager.IsHeadless) return;

			_ = SoundManager.PlayAtPosition(sound, gameObject.AssumedWorldPosServer());
		}

		public void ServerPlayDeniedSound()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(deniedSFX, gameObject.AssumedWorldPosServer());
		}
		public void ServerPlayPressureSound()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(warningSFX, gameObject.AssumedWorldPosServer());
		}

		public void TurnOffAllLights()
		{
			overlayLightsHandler.ChangeSprite((int) Lights.NoLight);
			previousLightSprite = (int) Lights.NoLight;
		}

		public void TurnOnBoltsLight()
		{
			overlayLightsHandler.ChangeSprite((int) Lights.BoltsLights);
			previousLightSprite = (int) Lights.BoltsLights;
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
