using System.Collections;

namespace UI.Objects.Medical.MedicalConsole
{
	public class GUI_MedicalConsole : NetTab
	{

		private global::Objects.Medical.MedicalConsole medicalConsole;

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

			medicalConsole = Provider.GetComponent<global::Objects.Medical.MedicalConsole>();
		}
	}
}