using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DisplaySettings : MonoBehaviour
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
	}


	private DisplaySettingsChangedEventArgs dsEventArgs = new DisplaySettingsChangedEventArgs();

	/// <summary>
	/// We are in fullscreen mode, or currently changing to fullscreen
	/// </summary>
	public bool IsFullScreen { get; private set; }

	private const int DEFAULT_WINDOWWIDTH = 1280;
	private const int DEFAULT_WINDOWHEIGHT = 720;

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
				if (value != ChatBubbleSize)
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
	private const float DEFAULT_CHATBUBBLESIZE = 2f;

	//TODO: Resolution options: issues #2047 #4107

	public event EventHandler<DisplaySettingsChangedEventArgs> SettingsChanged;
	protected virtual void OnSettingsChanged(DisplaySettingsChangedEventArgs e)
	{
		EventHandler<DisplaySettingsChangedEventArgs> handler = SettingsChanged;
		handler?.Invoke(this, e);
	}

	private void Awake()
	{
		IsFullScreen = Screen.fullScreen;
		SetupPrefs();
	}

	private void Update()
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
			yield return null;
		}
		else
		{
			Screen.SetResolution(DEFAULT_WINDOWWIDTH, DEFAULT_WINDOWHEIGHT, false);
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