using System;
using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;

namespace Managers.SettingsManager
{
	public class DisplaySettings : SingletonManager<DisplaySettings>
	{
		public class DisplaySettingsChangedEventArgs : EventArgs
		{
			public bool Changed => changed;
			private bool changed = false;

			public bool FullScreenChanged
			{
				get => fullScreenChanged;
				set
				{
					fullScreenChanged = value;
					changed |= value;
				}
			}
			private bool fullScreenChanged = false;

			public bool VSyncChanged
			{
				get => vSyncChanged;
				set
				{
					vSyncChanged = value;
					changed |= value;
				}
			}
			private bool vSyncChanged = false;

			public bool TargetFrameRateChanged
			{
				get => targetFrameRateChanged;
				set
				{
					targetFrameRateChanged = value;
					changed |= value;
				}
			}
			private bool targetFrameRateChanged = false;

			public bool ScrollWheelZoomChanged
			{
				get => scrollWheelZoomChanged;
				set
				{
					scrollWheelZoomChanged = value;
					changed |= value;
				}
			}
			private bool scrollWheelZoomChanged = false;

			public bool ZoomLevelChanged
			{
				get => zoomLevelChanged;
				set
				{
					zoomLevelChanged = value;
					changed |= value;
				}
			}
			private bool zoomLevelChanged = false;

			public bool ChatBubbleSizeChanged
			{
				get => chatBubbleSizeChanged;
				set
				{
					chatBubbleSizeChanged = value;
					changed |= value;
				}
			}
			private bool chatBubbleSizeChanged = false;

			public bool ChatBubbleInstantChanged
			{
				get => chatBubbleInstantChanged;
				set
				{
					chatBubbleInstantChanged = value;
					changed |= value;
				}
			}
			private bool chatBubbleInstantChanged = false;

			public bool ChatBubblePopInSpeedChanged
			{
				get => chatBubblePopInSpeedChanged;
				set
				{
					chatBubblePopInSpeedChanged = value;
					changed |= value;
				}
			}
			private bool chatBubblePopInSpeedChanged = false;

			public bool ChatBubbleAdditionalTimeChanged
			{
				get => chatBubbleAdditionalTimeChanged;
				set
				{
					chatBubbleAdditionalTimeChanged = value;
					changed |= value;
				}
			}
			private bool chatBubbleAdditionalTimeChanged = false;

			public bool ChatBubbleClownColourChanged
			{
				get => chatBubbleClownColourChanged;
				set
				{
					chatBubbleClownColourChanged = value;
					changed |= value;
				}
			}
			private bool chatBubbleClownColourChanged = false;
		}


		private DisplaySettingsChangedEventArgs dsEventArgs = new DisplaySettingsChangedEventArgs();

		/// <summary>
		/// We are in fullscreen mode, or currently changing to fullscreen
		/// </summary>
		public bool IsFullScreen { get; private set; }

		/// <summary>
		/// how much of the screen space window will take by default
		/// </summary>
		[SerializeField] [Range(0.1f, 1.0f)] private float windowSize = 0.5f;

		public bool VSyncEnabled
		{
			get
			{
				return PlayerPrefs.GetInt(PlayerPrefKeys.VSyncEnabled, DEFAULT_VSYNCENABLED ? 1 : 0) == 1;
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.VSyncEnabled))
				{
					if (value != VSyncEnabled)
					{
						dsEventArgs.VSyncChanged = true;
						PlayerPrefs.SetInt(PlayerPrefKeys.VSyncEnabled, value ? 1 : 0);
						PlayerPrefs.Save();
						ApplyEnableVSync();
					}
				}
				else
				{
					PlayerPrefs.SetInt(PlayerPrefKeys.VSyncEnabled, value ? 1 : 0);
					PlayerPrefs.Save();
					ApplyEnableVSync();
				}
			}
		}
		private const bool DEFAULT_VSYNCENABLED = true;

		public int TargetFrameRate
		{
			get
			{
				return PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate, DEFAULT_TARGETFRAMERATE);
			}
			set
			{
				int clampedVal = Mathf.Clamp(value, MIN_TARGETFRAMERATE, MAX_TARGETFRAMERATE);

				if (PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
				{
					if (clampedVal != TargetFrameRate)
					{
						dsEventArgs.TargetFrameRateChanged = true;
						PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, clampedVal);
						PlayerPrefs.Save();
						ApplyTargetFrameRate();
					}
				}
				else
				{
					PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, clampedVal);
					PlayerPrefs.Save();
					ApplyTargetFrameRate();
				}
			}
		}
		private const int DEFAULT_TARGETFRAMERATE = 99;
		private const int MIN_TARGETFRAMERATE = 30;
		public int Min_TargetFrameRate => MIN_TARGETFRAMERATE;
		private const int MAX_TARGETFRAMERATE = 144;
		public int Max_TargetFrameRate => MAX_TARGETFRAMERATE;

		public bool ScrollWheelZoom
		{
			get
			{
				return PlayerPrefs.GetInt(PlayerPrefKeys.ScrollWheelZoom, DEFAULT_SCROLLWHEELZOOM ? 1 : 0) == 1;
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.ScrollWheelZoom))
				{
					if (value != ScrollWheelZoom)
					{
						dsEventArgs.ScrollWheelZoomChanged = true;
						PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, value ? 1 : 0);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, value ? 1 : 0);
					PlayerPrefs.Save();
				}

			}
		}
		private const bool DEFAULT_SCROLLWHEELZOOM = true;

		public int ZoomLevel
		{
			get
			{
				return PlayerPrefs.GetInt(PlayerPrefKeys.CamZoomKey, DEFAULT_ZOOMLEVEL);
			}
			set
			{
				int clampedVal = Mathf.Clamp(value, MIN_ZOOMLEVEL, MAX_ZOOMLEVEL);

				if (PlayerPrefs.HasKey(PlayerPrefKeys.CamZoomKey))
				{
					if (clampedVal != ZoomLevel)
					{
						dsEventArgs.ZoomLevelChanged = true;
						PlayerPrefs.SetInt(PlayerPrefKeys.CamZoomKey, clampedVal);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetInt(PlayerPrefKeys.CamZoomKey, clampedVal);
					PlayerPrefs.Save();
				}
			}
		}
		private const int DEFAULT_ZOOMLEVEL = 24;
		private const int MIN_ZOOMLEVEL = 8;
		private const int MAX_ZOOMLEVEL = 64;

		#region ChatBubbles

		public float ChatBubbleSize
		{
			get
			{
				return PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleSize, DEFAULT_CHATBUBBLESIZE);
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
				{
					if (value.Approx(ChatBubbleSize) == false)
					{
						dsEventArgs.ChatBubbleSizeChanged = true;
						PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, value);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, value);
					PlayerPrefs.Save();
				}
			}
		}

		public const float DEFAULT_CHATBUBBLESIZE = 2f;

		public int ChatBubbleInstant
		{
			get
			{
				return PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleInstant, DEFAULT_CHATBUBBLEINSTANT);
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleInstant))
				{
					if (value != ChatBubbleInstant)
					{
						dsEventArgs.ChatBubbleInstantChanged = true;
						PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleInstant, value);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleInstant, value);
					PlayerPrefs.Save();
				}
			}
		}

		// 0 == false, 1 == true
		public const int DEFAULT_CHATBUBBLEINSTANT = 0;

		public float ChatBubblePopInSpeed
		{
			get
			{
				return PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, DEFAULT_CHATBUBBLEPOPINSPEED);
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubblePopInSpeed))
				{
					if (value.Approx(ChatBubblePopInSpeed) == false)
					{
						dsEventArgs.ChatBubblePopInSpeedChanged = true;
						PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, value);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, value);
					PlayerPrefs.Save();
				}
			}
		}

		public const float DEFAULT_CHATBUBBLEPOPINSPEED = 0.05f;

		public float ChatBubbleAdditionalTime
		{
			get
			{
				return PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, DEFAULT_CHATBUBBLEADDITIONALTIME);
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleAdditionalTime))
				{
					if (value.Approx(ChatBubbleAdditionalTime) == false)
					{
						dsEventArgs.ChatBubbleAdditionalTimeChanged = true;
						PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, value);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, value);
					PlayerPrefs.Save();
				}
			}
		}

		public const float DEFAULT_CHATBUBBLEADDITIONALTIME = 2f;

		public int ChatBubbleClownColour
		{
			get
			{
				return PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleClownColour, DEFAULT_CHATBUBBLECLOWNCOLOUR);
			}
			set
			{
				if (PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleClownColour))
				{
					if (value != ChatBubbleClownColour)
					{
						dsEventArgs.ChatBubbleClownColourChanged = true;
						PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleClownColour, value);
						PlayerPrefs.Save();
					}
				}
				else
				{
					PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleClownColour, value);
					PlayerPrefs.Save();
				}
			}
		}

		public const int DEFAULT_CHATBUBBLECLOWNCOLOUR = 1;

		#endregion

		//TODO: Resolution options: issues #2047 #4107

		public static string UISCALE_KEY = "uiscale";
		public static float UISCALE_DEFAULT = 0.85f;

		public event EventHandler<DisplaySettingsChangedEventArgs> SettingsChanged;
		protected virtual void OnSettingsChanged(DisplaySettingsChangedEventArgs e)
		{
			EventHandler<DisplaySettingsChangedEventArgs> handler = SettingsChanged;
			handler?.Invoke(this, e);
		}

		public override void Awake()
		{
			base.Awake();
			IsFullScreen = Screen.fullScreen;
			UIManager.Instance.Scaler.scaleFactor = PlayerPrefs.GetFloat(UISCALE_KEY, UISCALE_DEFAULT);
			SetupPrefs();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				if (Input.GetKey(KeyCode.LeftAlt))
				{
					ToggleFullScreen();
				}
			}

			if (dsEventArgs.Changed)
			{
				OnSettingsChanged(dsEventArgs);
				dsEventArgs = new DisplaySettingsChangedEventArgs();
			}
		}

		/// <summary>
		/// Load and set up Display related PlayerPrefs,
		/// then apply the settings to the current session
		/// </summary>
		private void SetupPrefs()
		{
			if (!PlayerPrefs.HasKey(PlayerPrefKeys.VSyncEnabled))
			{
				SetPrefDefaults(PlayerPrefKeys.VSyncEnabled);
			}
			else
			{
				ApplyEnableVSync();
			}

			if (!PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
			{
				SetPrefDefaults(PlayerPrefKeys.TargetFrameRate);
			}
			else
			{
				ApplyTargetFrameRate();
			}

			if (!PlayerPrefs.HasKey(PlayerPrefKeys.CamZoomKey))
			{
				SetPrefDefaults(PlayerPrefKeys.CamZoomKey);
			}

			if (!PlayerPrefs.HasKey(PlayerPrefKeys.ScrollWheelZoom))
			{
				SetPrefDefaults(PlayerPrefKeys.ScrollWheelZoom);
			}

			if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
			{
				SetPrefDefaults(PlayerPrefKeys.ChatBubbleSize);
			}
		}

		/// <summary>
		/// Resets all Display playerPrefs to defaults, alternatively provide specific PlayerPrefKeys entries to only reset those.
		/// </summary>
		/// <param name="playerPrefKeysEntries">Set of PlayerPrefKeys strings associated with a setting, to reset</param>
		public void SetPrefDefaults(params string[] playerPrefKeysEntries)
		{
			List<string> prefEntries = new List<string>(playerPrefKeysEntries);

			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.VSyncEnabled))
			{
				VSyncEnabled = DEFAULT_VSYNCENABLED;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.TargetFrameRate))
			{
				TargetFrameRate = DEFAULT_TARGETFRAMERATE;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.ChatBubbleSize))
			{
				ChatBubbleSize = DEFAULT_CHATBUBBLESIZE;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.CamZoomKey))
			{
				ZoomLevel = DEFAULT_ZOOMLEVEL;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.ScrollWheelZoom))
			{
				ScrollWheelZoom = DEFAULT_SCROLLWHEELZOOM;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.ChatBubbleInstant))
			{
				ChatBubbleInstant = DEFAULT_CHATBUBBLEINSTANT;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.ChatBubblePopInSpeed))
			{
				ChatBubblePopInSpeed = DEFAULT_CHATBUBBLEPOPINSPEED;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.ChatBubbleAdditionalTime))
			{
				ChatBubbleAdditionalTime = DEFAULT_CHATBUBBLEADDITIONALTIME;
			}
			if (prefEntries.Count == 0 || prefEntries.Contains(PlayerPrefKeys.ChatBubbleClownColour))
			{
				ChatBubbleClownColour = DEFAULT_CHATBUBBLECLOWNCOLOUR;
			}
		}

		private void ToggleFullScreen()
		{
			SetFullscreen(!IsFullScreen);
		}

		/// <summary>
		/// Sets fullscreen on or off. Fullscreen uses native resolution, windowed uses a default resolution.
		/// </summary>
		public void SetFullscreen(bool fullScreenOn)
		{
			StartCoroutine(ChangeFullscreenCoroutine(fullScreenOn));
		}

		private bool changingFullscreen = false;
		private IEnumerator ChangeFullscreenCoroutine(bool fullScreenOn)
		{
			while (changingFullscreen)
			{
				yield return null;
			}
			if (fullScreenOn == IsFullScreen)
			{
				yield break; // exit
			}

			changingFullscreen = true;
			IsFullScreen = fullScreenOn;

			if (fullScreenOn)
			{
				Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
				Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
				yield return null;
			}
			else
			{
				var windowWidth = (int)(Screen.currentResolution.width * windowSize);
				var windowHeight = (int)(Screen.currentResolution.height * windowSize);

				//making pixel perfect camera happy by not using odd resolutions
				if (windowWidth % 2 != 0)
				{
					windowWidth--;
				}
				if (windowHeight % 2 != 0)
				{
					windowHeight--;
				}

				Screen.SetResolution(windowWidth, windowHeight, false);
				Screen.fullScreenMode = FullScreenMode.Windowed;
				yield return null;
			}
			dsEventArgs.FullScreenChanged = true;
			changingFullscreen = false;
		}

		/// <summary>
		/// Apply the VSync setting from PlayerPrefs
		/// </summary>
		private void ApplyEnableVSync()
		{
			int vSync = PlayerPrefs.GetInt(PlayerPrefKeys.VSyncEnabled);
			QualitySettings.vSyncCount = vSync;
		}

		/// <summary>
		/// Apply the TargetFrameRate setting from PlayerPrefs
		/// </summary>
		private void ApplyTargetFrameRate()
		{
			int target = PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate);
			Application.targetFrameRate = Mathf.Clamp(target, MIN_TARGETFRAMERATE, MAX_TARGETFRAMERATE);
		}
	}
}