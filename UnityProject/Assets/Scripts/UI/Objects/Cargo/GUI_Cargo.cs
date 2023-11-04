using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;
using Objects.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_Cargo : NetTab
	{
		public NetText_label СreditsText;
		public NetText_label StatusText;
		public NetPageSwitcher NestedSwitcher;

		[SerializeField]
		private NetText_label raiseButtonText;

		public CargoConsole cargoConsole;

		public GUI_CargoPageCart pageCart;
		public GUI_CargoPageSupplies pageSupplies;
		public GUI_CargoOfflinePage OfflinePage;
		public GUI_CargoPageStatus statusPage;

		[SerializeField]
		private CargoCategory[] categories;

		protected override void InitServer()
		{
			CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateCreditsText);
			CargoManager.Instance.OnConnectionChangeToCentComm.AddListener(SwitchToOfflinePage);
			CargoManager.Instance.OnShuttleUpdate.AddListener(UpdateStatusText);

			foreach (var page in NestedSwitcher.Pages)
			{
				page.GetComponent<GUI_CargoPage>().cargoGUI = this;
			}

			UpdateCreditsText();
			UpdateStatusText();
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			cargoConsole = Provider.GetComponent<CargoConsole>();
			cargoConsole.cargoGUI = this;
			pageCart.SetUpTab();
		}

		public void OpenTab(NetPage pageToOpen)
		{
			NestedSwitcher.SetActivePage(CargoManager.Instance.CargoOffline ? OfflinePage : pageToOpen);
			//(Max) : NetUI shinangins where pages would randomly be null and kick players on headless servers.
			//This is a workaround to stop people from getting kicked. In-game reason would be this : Solar winds obstruct communications between CC and the station.
			if (pageToOpen == null) pageToOpen = OfflinePage;
			var cargopage = pageToOpen.GetComponent<GUI_CargoPage>();
			cargopage.OpenTab();
			cargopage.UpdateTab();
		}

		public void OpenCategory(int category)
		{
			pageSupplies.cargoCategory = categories[category];
			OpenTab(pageSupplies);
		}

		private void UpdateCreditsText()
		{
			if(CargoManager.Instance.CargoOffline)
			{
				СreditsText.SetValue("OFFLINE");
				return;
			}
			СreditsText.SetValue($"Credits:\n{CargoManager.Instance.Credits}");
			if (cargoConsole != null) { cargoConsole.PlayBudgetUpdateSound(); }
		}

		private void UpdateStatusText()
		{
			string[] statusText = new string[] { "On-Route Station", "Docked at Station", "On-Route Centcomm", "Docked at Centcomm" };
			
			if (CargoManager.Instance.CargoOffline)
			{
				StatusText.SetValue("OFFLINE");
				return;
			}

			switch(CargoManager.Instance.ShuttleStatus)
			{
				case ShuttleStatus.DockedStation:
					raiseButtonText.SetValue("Send");
					break;
				case ShuttleStatus.DockedCentcom:
					raiseButtonText.SetValue("Call");
					break;
			}

			statusPage.UpdateTab();
			StatusText.SetValue($"Status:\n{statusText[(int)CargoManager.Instance.ShuttleStatus]}");
		}

		public void CallShuttle()
		{
			if(CargoManager.Instance.CargoOffline) return;
			CargoManager.Instance.CallShuttle();
		}

		public void ResetId()
		{
			cargoConsole.ResetID();
		}

		private void SwitchToOfflinePage()
		{
			//If the event has been invoked and cargo is online, ignore.
			if (CargoManager.Instance.CargoOffline == false)
			{
				pageCart.SetUpTab();
				return;
			}
			OpenTab(OfflinePage);
			ResetId();
		}
	}
}
