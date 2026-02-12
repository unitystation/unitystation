using System.Collections;
using UnityEngine;
using US13.Objects.Medical;
using US13.UI.Core;
using US13.UI.Core.Net;

namespace US13.UI.Objects.Medical.MedicalCon
{
	public class GUI_MedicalConsole : NetTab
	{

		private MedicalTerminal medicalConsole;

		[SerializeField] private EmptyItemList entriesList;

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			medicalConsole = Provider.GetComponent<MedicalTerminal>();
			medicalConsole.OnScan.AddListener(UpdateList);
		}

		private void UpdateList()
		{
			entriesList.Clear();
			foreach (var info in medicalConsole.CrewInfo)
			{
				var element = entriesList.AddItem() as GUI_MedicalConsoleEntry;
				if (element == null) continue;
				element.SetValues(info);
			}
		}
	}
}