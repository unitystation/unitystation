using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Initialisation;
using US13.Managers.SettingsManager;
using US13.PlayerPrefs;
using US13.UI.Systems;
using US13.UI.Systems.IngameMenu;

namespace US13.UI.Core.OptionsMenu
{
	/// <summary>
	/// Controller for the display options view
	/// <summary>
	public class DisplayOptions : MonoBehaviour //TOOD
	/*/
	 so,
bool Toggle
int input
Slide
x and y box
drop-dowr
button
	 /*/
	{
		private Color VALIDCOLOR = Color.white;
		private Color INVALIDCOLOR = Color.red;

		[SerializeField] private Toggle fullscreenToggle = null;

		[SerializeField] private Toggle vSyncToggle = null;
		[SerializeField] private GameObject vSyncWarning = null;

		[SerializeField] private InputField frameRateTarget = null;

		[SerializeField] private Slider camZoomSlider = null;
		[SerializeField] private TMP_InputField uiScaleX = null;
		[SerializeField] private TMP_InputField uiScaleY = null;

		[SerializeField] private Toggle scrollWheelZoomToggle = null;

		[SerializeField] private Toggle DropShadow = null;

		[SerializeField] private Toggle ShuttleRadaRotation = null;

		public bool ItIsRefreshing = false;

		void OnEnable()
		{
			DisplaySettings.Instance.SettingsChanged += DisplaySettings_SettingsChanged;
			RefreshForm();
		}

		private void OnDisable()
		{
			DisplaySettings.Instance.SettingsChanged -= DisplaySettings_SettingsChanged;
		}

		private void DisplaySettings_SettingsChanged(object sender, DisplaySettings.DisplaySettingsChangedEventArgs e)
		{
			RefreshForm();
		}

		/// <summary>
		/// Update the form to match currently used values
		/// </summary>
		void RefreshForm()
		{
			ItIsRefreshing = true;
			fullscreenToggle.isOn = DisplaySettings.Instance.IsFullScreen;

			bool vSync = DisplaySettings.Instance.VSyncEnabled;
			vSyncToggle.isOn = vSync;
			vSyncWarning.SetActive(vSync);

			frameRateTarget.text = DisplaySettings.Instance.TargetFrameRate.ToString();
			frameRateTarget.textComponent.color = VALIDCOLOR;

			camZoomSlider.value = DisplaySettings.Instance.ZoomLevel / 8f;

			scrollWheelZoomToggle.isOn = DisplaySettings.Instance.ScrollWheelZoom;
			uiScaleX.text = UnityEngine.PlayerPrefs.GetInt(DisplaySettings.UISCALE_KEY + "x", (int)DisplaySettings.UISCALE_DEFAULT.x).ToString();
			uiScaleY.text = UnityEngine.PlayerPrefs.GetInt(DisplaySettings.UISCALE_KEY + "y", (int)DisplaySettings.UISCALE_DEFAULT.y).ToString();
			//ParseAndSetReferenceResolutionForUiScale();

			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ItemDropShadow) == false)
			{
				UnityEngine.PlayerPrefs.SetString(PlayerPrefKeys.ItemDropShadow,  true.ToString());
			}

			DropShadow.isOn = bool.Parse(UnityEngine.PlayerPrefs.GetString(PlayerPrefKeys.ItemDropShadow));


			// if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ShuttleRadarRotation) == false)
			// {
			// 	UnityEngine.PlayerPrefs.SetString(PlayerPrefKeys.ShuttleRadarRotation,  true.ToString());
			// }

			//ShuttleRadaRotation.isOn = bool.Parse(UnityEngine.PlayerPrefs.GetString(PlayerPrefKeys.ShuttleRadarRotation));

			ItIsRefreshing = false;
		}


		public void SetShuttleRadaRotation(bool State)
		{
			if (ItIsRefreshing) return;
			ShuttleCameraRenderer.RotateCamera = State;
		}

		public void SetItemDropShadow(bool State)
		{
			if (ItIsRefreshing) return;
			LoadManager.Instance.SetMaterialStatus(State);
		}

		/// <summary>
		/// Reset DisplayOptions settings to their default values and apply them
		/// </summary>
		public void ResetDefaults()
		{
			ModalPanelManager.Instance.Confirm(
				"Are you sure?",
				() =>
				{
					DisplaySettings.Instance.SetPrefDefaults();
					RefreshForm();
				},
				"Reset"
			);
		}

		/// <summary>
		/// Toggles fullscreen. Fullscreen uses native resolution, windowed uses a default resolution.
		/// </summary>
		public void OnFullscreenToggle()
		{
			// Ensure this togglebox isn't triggered by attempts to fullscreen with Alt-Enter
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				if (Input.GetKey(KeyCode.LeftAlt))
				{
					return;
				}
			}

			DisplaySettings.Instance.SetFullscreen(fullscreenToggle.isOn);
		}

		/// <summary>
		/// Sets a new VSync setting and shows the TargetFrameRate warning if enabled
		/// </summary>
		public void OnVSyncToggle()
		{
			DisplaySettings.Instance.VSyncEnabled = vSyncToggle.isOn;
			vSyncWarning.SetActive(vSyncToggle.isOn);
		}

		/// <summary>
		/// Validates FrameRateTarget input, indicated with colored text. Use the new value if valid.
		/// </summary>
		public static string OnFrameRateTargetEdit(int val)
		{
			if (val >= DisplaySettings.Instance.Min_TargetFrameRate &&
			    val <= DisplaySettings.Instance.Max_TargetFrameRate)
			{
				//frameRateTarget.textComponent.color = VALIDCOLOR;
				DisplaySettings.Instance.TargetFrameRate = val;
				return "";
			}
			else
			{
				return "Invalid number";
				// //frameRateTarget.textComponent.color = INVALIDCOLOR;
				// return;
			}
		}

		public void OnZoomLevelChange()
		{
		}



		public static void ParseAndSetReferenceResolutionForUiScale( int x,  int y)
		{
			UIManager.Instance.Scaler.referenceResolution = new Vector2(x, y);
		}

		public void OnScrollWheelToggle()
		{
			DisplaySettings.Instance.ScrollWheelZoom = scrollWheelZoomToggle.isOn;
		}
	}
}