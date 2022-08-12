using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Storage.VirtualStorage;
using Objects.Research;
using Systems.Cargo;

namespace Items.Science
{
	public class ArtifactDataDisk : HardDriveBase
	{
		public int SellPrice = 10000;

		private ItemAttributesV2 itemAttributes;

		public void Awake()
		{
			DataOnStorage.Add(new ArtifactDataFile());
			itemAttributes = GetComponent<ItemAttributesV2>();
			itemAttributes.onExport.AddListener(OnSell);
		}

		private void OnSell(string ExportName, string ExportMessage)
		{
			foreach (ArtifactDataFile file in DataOnStorage)
			{
				if (CargoManager.ResearchedArtifacts.Contains(file.correctData.ID) == false)
				{
					CargoManager.AddArtifactToList(file.correctData.ID);
				}
				else
				{
					itemAttributes.ServerSetArticleName("Artifact Data Disk: Already Researched");
					itemAttributes.SetExportCost(0);
				}
			}
		}

		public void CalculateExportCost()
		{
			int cost = 0;

			foreach(ArtifactDataFile file in DataOnStorage)
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

	public class ArtifactDataFile : VirtualData
	{
		//Data saved onto disk by console
		public ArtifactData inputData = new ArtifactData();
		//Correct Data, used to judge value for export
		public ArtifactData correctData = new ArtifactData();

		public void UpdateArtifactDataSize()
		{
			Size = 30;
		}
	}
}
