using System.Collections.Generic;
using UnityEngine;

namespace Items.Storage.VirtualStorage
{
	public class HardDriveBase : MonoBehaviour
	{
		[SerializeField] private int maxCapacity = 1000;
		private int currentCapacity = 0;
		public int CurrentCapacity => currentCapacity;
		public List<VirtualData> DataOnStorage = new List<VirtualData>();

		public void UpdateCapacitySize()
		{
			currentCapacity = 0;
			foreach (var file in DataOnStorage)
			{
				currentCapacity += file.Size;
			}
		}

		public int GetTotalCorruptedFiles()
		{
			var count = 0;
			foreach (var file in DataOnStorage)
			{
				if(file.Corrupted == false) continue;
				count++;
			}

			return count;
		}

		public bool AddDataToStorage(VirtualData data)
		{
			if (currentCapacity >= maxCapacity) return false;
			DataOnStorage.Add(data);
			UpdateCapacitySize();
			return true;
		}

		public void CorruptRandomData(bool shuffleData = false)
		{
			if(shuffleData) DataOnStorage.Shuffle();
			foreach (var data in DataOnStorage)
			{
				data.CorruptDataByChance();
			}
		}
	}

	public class VirtualData
	{
		public int Size {get; set;}
		public bool Corrupted {get; set;}

		public void CorruptDataByChance()
		{
			if (DMMath.Prob(50)) Corrupted = true;
		}
	}
}