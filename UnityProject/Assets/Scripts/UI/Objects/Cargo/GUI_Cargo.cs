using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;
using Objects.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_Cargo : NetTab
	{
		public NetLabel СreditsText;
		public NetLabel DirectoryText;
		public NetPageSwitcher NestedSwitcher;

		public CargoConsole cargoConsole;

		public GUI_CargoPageCart pageCart;
		public GUI_CargoPageSupplies pageSupplies;
		public GUI_CargoOfflinePage OfflinePage;

		protected override void InitServer()
		{
			CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateCreditsText);
			CargoManager.Instance.OnConnectionChangeToCentComm.AddListener(SwitchToOfflinePage);
			foreach (var page in NestedSwitcher.Pages)
			{
				page.GetComponent<GUI_CargoPage>().cargoGUI = this;
			}
			UpdateCreditsText();
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
			DirectoryText.SetValueServer(cargopage.DirectoryName);
		}

		private void UpdateCreditsText()
		{
			if(CargoManager.Instance.CargoOffline)
			{
				СreditsText.SetValueServer("OFFLINE");
				return;
			}
			СreditsText.SetValueServer($"Budget: {CargoManager.Instance.Credits}");
			if (cargoConsole != null) { cargoConsole.PlayBudgetUpdateSound(); }
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
			if(CargoManager.Instance.CargoOffline == false) return;
			OpenTab(OfflinePage);
			ResetId();
		}
	}
}
