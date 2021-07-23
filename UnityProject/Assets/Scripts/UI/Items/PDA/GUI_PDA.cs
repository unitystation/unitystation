using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;
using Items.PDA;

namespace UI.Items.PDA
{
	public class GUI_PDA : NetTab
	{
		[Tooltip("Assign the breadcrumb here")]
		[SerializeField]
		private NetLabel breadcrumb = null;

		[Tooltip("Put the main NetPage switcher here")]
		public NetPageSwitcher mainSwitcher = null;

		[Tooltip("Put the object here")]
		public Image Background = null;

		[Tooltip("Put the overlay object here")]
		public Image BackgroundOverlay = null;

		[Tooltip("Put the overlay images here")]
		[SerializeField]
		List<Sprite> overlays = default;

		[Header("Assign the PDA's main pages here")]
		public GUI_PDAMainMenu menuPage = null;
		public GUI_PDACrewManifest manifestPage = null;
		public GUI_PDANotes notesPage = null;
		public GUI_PDASettingMenu settingsPage = null;
		public GUI_PDAUplinkMenu uplinkPage = null;

		public PDALogic PDA { get; private set; }
		public NetPage MainPage => mainSwitcher.DefaultPage;

		#region Lifecycle

		[SuppressMessage("ReSharper", "UEA0006")] // Supresses the couroutine allocation warning, because it's annoying
		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			PDA = Provider.GetComponent<PDALogic>();
			Background.color = PDA.UIBG;
			BackgroundOverlay.sprite = overlays[PDA.OVERLAY];
			BackgroundOverlay.color = PDA.UIOVER;
			PDA.PDAGui = this;
			OpenPage(MainPage);
		}

		#endregion Lifecycle

		/// <summary>
		/// Opens the page on the given switcher, setting it as the current page.
		/// Runs the page's lifecycle methods if it implements IPageCleanupable or IPageReadyable.
		/// </summary>
		/// <param name="switcher">The switcher that the page is associated with</param>
		/// <param name="page">The page to set as the current page for the given switcher</param>
		public void OpenPageOnSwitcher(NetPageSwitcher switcher, NetPage page)
		{
			if (switcher.CurrentPage is IPageCleanupable cleanupable)
			{
				cleanupable.OnPageDeactivated();
			}

			if (page is IPageReadyable readyable)
			{
				readyable.OnPageActivated();
			}

			switcher.SetActivePage(page);
		}

		/// <summary>
		/// Tells the main page switcher to set the current page.
		/// </summary>
		/// <param name="page">The page to set as the current page</param>
		public void OpenPage(NetPage page)
		{
			OpenPageOnSwitcher(mainSwitcher, page);
		}

		/// <summary>
		/// Sets the text for the breadcrumb that exists the top of every page.
		/// </summary>
		/// <param name="directory"></param>
		public void SetBreadcrumb(string directory)
		{
			breadcrumb.SetValueServer(directory);
		}

		public void PlayRingtone()
		{
			PDA.PlayRingtone();
		}

		public void PlayDenyTone()
		{
			PDA.PlayDenyTone();
		}
	}
}
