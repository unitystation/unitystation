using Core;
using Systems.Research;

namespace Items.Storage.VirtualStorage
{
	public class VirtualData
	{
		public int Size {get; set;}
		public bool Corrupted {get; set;}

		public void CorruptDataByChance()
		{
			if (Random13.Prob()) Corrupted = true; // 50/50 chance.
		}
	}

	public class TechwebFiles : VirtualData
	{
		public Techweb Techweb = new Techweb();

		public void UpdateTechwebSize()
		{
			Size = Techweb.ResearchedTech.Count;
		}
	}
}