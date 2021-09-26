using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics.Acu
{
	/// <summary>
	/// Allows the peeper to manage devices connected to the <see cref="AirController"/>.
	/// </summary>
	public class GUI_AcuDevicesPage : GUI_AcuPage
	{
		[SerializeField]
		private NetLabel tabLabel = default;

		[SerializeField]
		private NetPageSwitcher pageSwitcher = default;

		[SerializeField]
		private EmptyItemList ventsList = default;
		[SerializeField]
		private EmptyItemList scrubbersList = default;

		public override bool IsProtected => true;

		private static readonly string ventsTabStr =
				"----------\n" +
				"| Vents  | Scrubbers\n" +
				"----------------------------------------------------";
		private static readonly string scrubbersTabStr =
				"         -------------\n" +
				"  Vents  | Scrubbers |\n" +
				"----------------------------------------------------";

		private int requestedPage = 0;

		public override void OnPageActivated()
		{
			SetDevicePage(requestedPage);
		}

		public override void OnPageDeactivated()
		{
			ClearLists();
		}

		public override void OnPeriodicUpdate()
		{
			// Periodic as the ACU's list of connected devices can change at any time.
			UpdateLists();
		}

		#region Buttons

		public void BtnShowDevices(int page)
		{
			AcuUi.PlayTap();
			SetDevicePage(page);
		}

		#endregion

		private void SetDevicePage(int page)
		{
			if (page != requestedPage)
			{
				requestedPage = page;
				ClearLists();
				pageSwitcher.SetActivePage(requestedPage);
			}
			
			UpdateLists();
		}

		private void UpdateLists()
		{
			switch (requestedPage)
			{
				case 0:
					tabLabel.SetValueServer(ventsTabStr);
					PopulateVents();
					break;
				case 1:
					tabLabel.SetValueServer(scrubbersTabStr);
					PopulateScrubbers();
					break;
			}
		}

		private void ClearLists()
		{
			ventsList.Clear();
			scrubbersList.Clear();
		}

		// TODO: Consider generic method or, perhaps, restructure AirVents/Scrubbers
		private void PopulateVents()
		{
			List<AirVent> vents = Acu.ConnectedDevices.OfType<AirVent>().ToList();
			if (vents.Count != ventsList.Entries.Length)
			{
				ventsList.SetItems(vents.Count);
			}

			for (int i = 0; i < ventsList.Entries.Length; i++)
			{
				DynamicEntry dynamicEntry = ventsList.Entries[i];
				var entry = dynamicEntry.GetComponent<GUI_AcuVentEntry>();
				entry.SetValues(AcuUi, vents[i]);
			}
		}

		private void PopulateScrubbers()
		{
			List<Scrubber> scrubbers = Acu.ConnectedDevices.OfType<Scrubber>().ToList();
			if (scrubbers.Count != scrubbersList.Entries.Length)
			{
				scrubbersList.SetItems(scrubbers.Count);
			}

			for (int i = 0; i < scrubbersList.Entries.Length; i++)
			{
				DynamicEntry dynamicEntry = scrubbersList.Entries[i];
				var entry = dynamicEntry.GetComponent<GUI_AcuScrubberEntry>();
				entry.SetValues(AcuUi, scrubbers[i]);
			}
		}
	}
}
