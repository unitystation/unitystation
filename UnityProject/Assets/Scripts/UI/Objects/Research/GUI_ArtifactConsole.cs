using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Objects;
using Items.Science;

namespace UI.Objects.Research
{
	public class GUI_ArtifactConsole : NetTab
	{
		[SerializeField]
		private InputFieldFocus radInput;

		private ArtifactData inputData;

		bool isUpdating;

		private ArtifactConsole console;

		protected override void InitServer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StartCoroutine(WaitForProvider());
			}
		}

		private IEnumerator WaitForProvider()
		{

			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			console = Provider.GetComponent<ArtifactConsole>();

			Logger.Log(nameof(WaitForProvider), Category.Research);
		}

		public void UpdateGUI()
		{
			
		}

		public void WriteData()
		{

		}

		public void EjectDisk()
		{

		}

	}
}
