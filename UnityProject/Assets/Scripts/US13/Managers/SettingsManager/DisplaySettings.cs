using System;
using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;
using US13.Managers.UpdateManager;
using US13.PlayerPrefs;
using US13.UI.Systems;
using Util;

namespace US13.Managers.SettingsManager
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
				return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.VSyncEnabled, DEFAULT_VSYNCENABLED ? 1 : 0) == 1;
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.VSyncEnabled))
				{
					if (value != VSyncEnabled)
					{
						dsEventArgs.VSyncChanged = true;
						UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.VSyncEnabled, value ? 1 : 0);
						UnityEngine.PlayerPrefs.Save();
						ApplyEnableVSync();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.VSyncEnabled, value ? 1 : 0);
					UnityEngine.PlayerPrefs.Save();
					ApplyEnableVSync();
				}
			}
		}
		private const bool DEFAULT_VSYNCENABLED = true;

		public int TargetFrameRate
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate, DEFAULT_TARGETFRAMERATE);
			}
			set
			{
				int clampedVal = Mathf.Clamp(value, MIN_TARGETFRAMERATE, MAX_TARGETFRAMERATE);

				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
				{
					if (clampedVal != TargetFrameRate)
					{
						dsEventArgs.TargetFrameRateChanged = true;
						UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, clampedVal);
						UnityEngine.PlayerPrefs.Save();
						ApplyTargetFrameRate();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, clampedVal);
					UnityEngine.PlayerPrefs.Save();
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
				return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.ScrollWheelZoom, DEFAULT_SCROLLWHEELZOOM ? 1 : 0) == 1;
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ScrollWheelZoom))
				{
					if (value != ScrollWheelZoom)
					{
						dsEventArgs.ScrollWheelZoomChanged = true;
						UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, value ? 1 : 0);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, value ? 1 : 0);
					UnityEngine.PlayerPrefs.Save();
				}

			}
		}
		private const bool DEFAULT_SCROLLWHEELZOOM = true;

		public int ZoomLevel
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.CamZoomKey, DEFAULT_ZOOMLEVEL);
			}
			set
			{
				int clampedVal = Mathf.Clamp(value, MIN_ZOOMLEVEL, MAX_ZOOMLEVEL);

				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.CamZoomKey))
				{
					if (clampedVal != ZoomLevel)
					{
						dsEventArgs.ZoomLevelChanged = true;
						UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.CamZoomKey, clampedVal);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.CamZoomKey, clampedVal);
					UnityEngine.PlayerPrefs.Save();
				}
			}
		}
		private const int DEFAULT_ZOOMLEVEL = 32;
		private const int MIN_ZOOMLEVEL = 32;
		private const int MAX_ZOOMLEVEL = 512;

		#region ChatBubbles

		public float ChatBubbleSize
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleSize, DEFAULT_CHATBUBBLESIZE);
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
				{
					if (value.Approx(ChatBubbleSize) == false)
					{
						dsEventArgs.ChatBubbleSizeChanged = true;
						UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, value);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, value);
					UnityEngine.PlayerPrefs.Save();
				}
			}
		}

		public const float DEFAULT_CHATBUBBLESIZE = 2f;

		public int ChatBubbleInstant
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleInstant, DEFAULT_CHATBUBBLEINSTANT);
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleInstant))
				{
					if (value != ChatBubbleInstant)
					{
						dsEventArgs.ChatBubbleInstantChanged = true;
						UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleInstant, value);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleInstant, value);
					UnityEngine.PlayerPrefs.Save();
				}
			}
		}

		// 0 == false, 1 == true
		public const int DEFAULT_CHATBUBBLEINSTANT = 0;

		public float ChatBubblePopInSpeed
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, DEFAULT_CHATBUBBLEPOPINSPEED);
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubblePopInSpeed))
				{
					if (value.Approx(ChatBubblePopInSpeed) == false)
					{
						dsEventArgs.ChatBubblePopInSpeedChanged = true;
						UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, value);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubblePopInSpeed, value);
					UnityEngine.PlayerPrefs.Save();
				}
			}
		}

		public const float DEFAULT_CHATBUBBLEPOPINSPEED = 0.05f;

		public float ChatBubbleAdditionalTime
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, DEFAULT_CHATBUBBLEADDITIONALTIME);
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleAdditionalTime))
				{
					if (value.Approx(ChatBubbleAdditionalTime) == false)
					{
						dsEventArgs.ChatBubbleAdditionalTimeChanged = true;
						UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, value);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime, value);
					UnityEngine.PlayerPrefs.Save();
				}
			}
		}

		public const float DEFAULT_CHATBUBBLEADDITIONALTIME = 2f;

		public int ChatBubbleClownColour
		{
			get
			{
				return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleClownColour, DEFAULT_CHATBUBBLECLOWNCOLOUR);
			}
			set
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleClownColour))
				{
					if (value != ChatBubbleClownColour)
					{
						dsEventArgs.ChatBubbleClownColourChanged = true;
						UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleClownColour, value);
						UnityEngine.PlayerPrefs.Save();
					}
				}
				else
				{
					UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleClownColour, value);
					UnityEngine.PlayerPrefs.Save();
				}
			}
		}

		public const int DEFAULT_CHATBUBBLECLOWNCOLOUR = 1;

		#endregion

		//TODO: Resolution options: issues #2047 #4107

		public static string UISCALE_KEY = "uiscale";
		public static Vector2 UISCALE_DEFAULT = new Vector2(2560, 1440);

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
			Vector2 savedScale = new Vector2(
				UnityEngine.PlayerPrefs.GetInt(UISCALE_KEY + "x", (int)UISCALE_DEFAULT.x),
				UnityEngine.PlayerPrefs.GetInt(UISCALE_KEY + "y", (int)UISCALE_DEFAULT.y)
			);
			UIManager.Instance.Scaler.referenceResolution = savedScale;
			SetupPrefs();
		}

		private void OnEnable()
		{
			UpdateManager.UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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
			if (!UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.VSyncEnabled))
			{
				SetPrefDefaults(PlayerPrefKeys.VSyncEnabled);
			}
			else
			{
				ApplyEnableVSync();
			}

			if (!UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
			{
				SetPrefDefaults(PlayerPrefKeys.TargetFrameRate);
			}
			else
			{
				ApplyTargetFrameRate();
			}

			if (!UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.CamZoomKey))
			{
				SetPrefDefaults(PlayerPrefKeys.CamZoomKey);
			}

			if (!UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ScrollWheelZoom))
			{
				SetPrefDefaults(PlayerPrefKeys.ScrollWheelZoom);
			}

			if (!UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
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
			int vSync = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.VSyncEnabled);
			QualitySettings.vSyncCount = vSync;
		}

		/// <summary>
		/// Apply the TargetFrameRate setting from PlayerPrefs
		/// </summary>
		private void ApplyTargetFrameRate()
		{
			int target = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate);
			Application.targetFrameRate = Mathf.Clamp(target, MIN_TARGETFRAMERATE, MAX_TARGETFRAMERATE);
		}
	}
}