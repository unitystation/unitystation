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