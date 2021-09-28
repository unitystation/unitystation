using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Objects.Atmospherics;

namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// Main class for the <see cref="AirController"/>'s GUI.
	/// </summary>
	public class GUI_Acu : NetTab
	{
		[SerializeField, BoxGroup("Paging")]
		private NetPageSwitcher pageSwitcher = default;
		[SerializeField, BoxGroup("Paging")]
		private GUI_AcuLockedPage lockedMessagePage = default;
		[SerializeField, BoxGroup("Paging")]
		private GUI_AcuNoPowerPage noPowerPage = default;

		[SerializeField, BoxGroup("Status")]
		private NetColorChanger statusIndicator = default;
		[SerializeField, BoxGroup("Status")]
		private NetLabel statusLabel = default;

		[SerializeField, BoxGroup("Element References")]
		private GameObject lockIcon = default;
		[SerializeField, BoxGroup("Element References")]
		private GameObject powerIcon = default;
		[SerializeField, BoxGroup("Element References")]
		private GameObject connectionIcon = default;

		[SerializeField]
		private NetLabel acuLabel = default;

		[SerializeField]
		private GUI_AcuValueModal editValueModal = default;

		[SerializeField, BoxGroup("Colors")]
		private Color colorOff = Color.grey;
		[SerializeField, BoxGroup("Colors")]
		private Color colorNominal = Color.green;
		[SerializeField, BoxGroup("Colors")]
		private Color colorCaution = Color.yellow;
		[SerializeField, BoxGroup("Colors")]
		private Color colorAlert = Color.red;

		public GUI_AcuValueModal EditValueModal => editValueModal;

		// Remember the requested page so that it can be loaded when the conditions are met (e.g. power returns).
		private GUI_AcuPage requestedPage;

		private NetSpriteImage lockIconSprite;
		private NetColorChanger lockIconColor;
		private NetColorChanger powerIconColor;
		private NetColorChanger connectionIconColor;

		private static Dictionary<AcuStatus, Color> statusColors;

		public AirController Acu { get; private set; }

		#region Initialisation

		private void Awake()
		{
			if (statusColors == null)
			{
				statusColors = new Dictionary<AcuStatus, Color>()
				{
					{ AcuStatus.Off, colorOff },
					{ AcuStatus.Nominal, colorNominal },
					{ AcuStatus.Caution, colorCaution },
					{ AcuStatus.Alert, colorAlert },
				};
			}

			lockIconSprite = lockIcon.GetComponent<NetSpriteImage>();
			lockIconColor = lockIcon.GetComponent<NetColorChanger>();
			powerIconColor = powerIcon.GetComponent<NetColorChanger>();
			connectionIconColor = connectionIcon.GetComponent<NetColorChanger>();
		}

		protected override void InitServer()
		{
			requestedPage = pageSwitcher.DefaultPage as GUI_AcuPage;
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			Acu = Provider.GetComponent<AirController>();

			var acuName = Acu.name.StartsWith("ACU - ") ? Acu.name.Substring("ACU - ".Length) : Acu.name;
			// "ACU - " as per NameValidator tool.
			acuLabel.SetValueServer(acuName);

			foreach (var netPage in pageSwitcher.Pages)
			{
				var page = netPage as GUI_AcuPage;
				page.Acu = Acu;
				page.AcuUi = this;
			}

			editValueModal.Acu = Acu;
			editValueModal.AcuUi = this;

			OnTabOpened.AddListener(TabOpened);
			OnTabClosed.AddListener(TabClosed);
			if (IsUnobserved == false)
			{
				// Call manually; OnTabOpened is invoked before the Provider is set,
				// so the initial invoke was missed.
				TabOpened();
			}
		}

		private void TabOpened(ConnectedPlayer newPeeper = default)
		{
			SetPage(ValidatePage(requestedPage));

			// Quicker ACU updates if we have peepers.
			UpdateManager.Add(PeriodicUpdate, 0.5f);
			Acu.OnStateChanged += OnAcuStateChanged;
			PeriodicUpdate();
		}

		private void TabClosed(ConnectedPlayer oldPeeper = default)
		{
			// Remove listeners when unobserved (old peeper has not yet been removed).
			if (Peepers.Count <= 1)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
				Acu.OnStateChanged -= OnAcuStateChanged;
			}
		}

		#endregion

		private void PeriodicUpdate()
		{
			Acu.RequestImmediateUpdate();
			UpdateElements();
			(pageSwitcher.CurrentPage as GUI_AcuPage).OnPeriodicUpdate();
		}

		private void OnAcuStateChanged()
		{
			UpdateElements();
			SetPage(ValidatePage(requestedPage));
		}

		private void UpdateElements()
		{
			statusIndicator.SetValueServer(statusColors[Acu.OverallStatus]);

			// Update display's system tray elements
			statusLabel.SetValueServer(Acu.IsPowered
					? ColorStringByStatus(Acu.OverallStatus.ToString(), Acu.OverallStatus)
					: string.Empty);
			lockIconSprite.SetSprite(Acu.IsLocked ? 0 : 1);
			lockIconColor.SetValueServer(Acu.IsLocked ? colorNominal : colorCaution);
			powerIconColor.SetValueServer(Acu.IsPowered ? colorNominal : colorAlert);
			Color sampleQualityColor = Acu.ConnectedDevices.Count > 0 ? colorCaution : colorAlert;
			sampleQualityColor = Acu.ConnectedDevices.Count > 2 ? colorNominal : sampleQualityColor;
			connectionIconColor.SetValueServer(sampleQualityColor);
		}

		private GUI_AcuPage ValidatePage(GUI_AcuPage requestedPage)
		{
			if (Acu.IsPowered == false) return noPowerPage;
			if (requestedPage.IsProtected && Acu.IsLocked) return lockedMessagePage;

			return requestedPage;
		}

		private void SetPage(GUI_AcuPage page)
		{
			var currentPage = pageSwitcher.CurrentPage as GUI_AcuPage;
			if (page != currentPage)
			{
				EditValueModal.Close();
				currentPage.OnPageDeactivated();
				pageSwitcher.SetActivePage(page);
				page.OnPageActivated();
			}

			page.OnPeriodicUpdate();
		}

		#region Buttons

		public void BtnRequestPage(int pageIndex)
		{
			PlayClick();
			requestedPage = pageSwitcher.Pages[pageIndex] as GUI_AcuPage;
			SetPage(ValidatePage(requestedPage));
		}

		#endregion

		#region Helpers

		/// <summary>Get the color code associated with the given <c>ACU</c> status.</summary>
		/// <returns>HTML color code as a hexadecimal string</returns>
		public static string GetHtmlColorByStatus(AcuStatus status)
		{
			if (statusColors.ContainsKey(status))
			{
				return ColorUtility.ToHtmlStringRGB(statusColors[status]);
			}

			// What, write some kind of color code here? Pfft!
			return ColorUtility.ToHtmlStringRGB(Color.white);
		}

		/// <summary>Color the given string with the associated color of the given <c>ACU</c> status.</summary>
		public static string ColorStringByStatus(string text, AcuStatus status)
		{
			return $"<color=#{GetHtmlColorByStatus(status)}>{text}</color>";
		}

		public void PlayClick()
		{
			PlaySound(CommonSounds.Instance.Click01);
		}

		public void PlayTap()
		{
			PlaySound(CommonSounds.Instance.Tap);
		}

		#endregion
	}
}
