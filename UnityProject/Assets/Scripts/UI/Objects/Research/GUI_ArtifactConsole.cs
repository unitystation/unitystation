using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Objects;
using Items.Science;
using Objects.Research;
using Items.Storage.VirtualStorage;

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
			if (console.dataDisk == null || console.connectedArtifact == null) return;

			foreach(ArtifactDataFiles data in console.dataDisk.DataOnStorage)
			{
				data.inputData = inputData;
				data.correctData = console.connectedArtifact.artifactData;
			}
			console.dataDisk.CalculateExportCost();
		}


		public void EjectDisk()
		{
			console.GetComponent<ItemStorage>().ServerDropAll();
		}

	}
}
