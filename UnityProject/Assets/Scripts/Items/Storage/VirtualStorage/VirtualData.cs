using Systems.Research;
using Objects.Research;

namespace Items.Storage.VirtualStorage
{
	public class VirtualData
	{
		public int Size {get; set;}
		public bool Corrupted {get; set;}

		public void CorruptDataByChance()
		{
			if (DMMath.Prob(50)) Corrupted = true;
		}
	}

	public class TechwebFiles : VirtualData
	{
		public Techweb Techweb = new Techweb();

		public void UpdateTechwebSize()
		{
			Size = Techweb.researchedTech.Count;
		}
	}
}