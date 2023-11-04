using System.Collections;
using Objects.Medical;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Medical.MedicalCon
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