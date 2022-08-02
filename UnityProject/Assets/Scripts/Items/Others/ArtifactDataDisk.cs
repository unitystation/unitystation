using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Storage.VirtualStorage;

namespace Items.Science
{
	public class ArtifactDataDisk : HardDriveBase
	{
		public int SellPrice = 10000;

		public void Awake()
		{
			DataOnStorage.Add(new ArtifactDataFiles());
		}

		public void CalculateExportCost()
		{
			int cost = 0;

			foreach(ArtifactDataFiles file in DataOnStorage)
			{
				cost += Mathf.Clamp(1000 - (2*Mathf.Abs(file.inputData.radiationlevel - file.correctData.radiationlevel)), 0, 1000);
				cost += Mathf.Clamp(1000 - (20*Mathf.Abs(file.inputData.bluespacesig - file.correctData.bluespacesig)), 0, 1000);
				cost += Mathf.Clamp(1000 - (5*Mathf.Abs(file.inputData.bananiumsig- file.correctData.bananiumsig)), 0, 1000);

				if (file.inputData.DamageEffectValue == file.correctData.DamageEffectValue) cost += 2000;
				if (file.inputData.InteractEffectValue == file.correctData.InteractEffectValue) cost += 2000;
				if (file.inputData.AreaEffectValue == file.correctData.AreaEffectValue) cost += 2000;
				
				if (file.inputData.Type == file.correctData.Type) cost += 1000;
			}

			cost *= (SellPrice / 10000);

			GetComponent<ItemAttributesV2>().SetExportCost(cost);
		}
	}
}
