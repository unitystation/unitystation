using System.Collections;
using UnityEngine;
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

		protected override void InitServer()
		{
			CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateCreditsText);
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
			NestedSwitcher.SetActivePage(pageToOpen);
			var cargopage = pageToOpen.GetComponent<GUI_CargoPage>();
			cargopage.OpenTab();
			cargopage.UpdateTab();
			DirectoryText.SetValueServer(cargopage.DirectoryName);
		}

		private void UpdateCreditsText()
		{
			СreditsText.SetValueServer($"Budget: {CargoManager.Instance.Credits}");
			if (cargoConsole != null) { cargoConsole.PlayBudgetUpdateSound(); }
		}

		public void CallShuttle()
		{
			CargoManager.Instance.CallShuttle();
		}

		public void ResetId()
		{
			cargoConsole.ResetID();
		}
	}
}
