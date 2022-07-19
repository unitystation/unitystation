using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Storage.VirtualStorage;

namespace Items.Science
{
	public class ArtifactDataDisk : HardDriveBase
	{
		public void Awake()
		{
			DataOnStorage.Add(new ArtifactDataFiles());
		}

		public void CalculateExportCost()
		{
			int cost = 0;

			foreach(ArtifactDataFiles file in DataOnStorage)
			{
				cost += Mathf.Clamp(50 - Mathf.Abs(file.inputData.radiationlevel - file.correctData.radiationlevel), 0, 50);
				cost += Mathf.Clamp(50 - 2*Mathf.Abs(file.inputData.bluespacesig - file.correctData.bluespacesig), 0, 50);
				cost += Mathf.Clamp(50 - Mathf.Abs(file.inputData.bananiumsig- file.correctData.bananiumsig), 0, 50);
			}
		}
	}
}
