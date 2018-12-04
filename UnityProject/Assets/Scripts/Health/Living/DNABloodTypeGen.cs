using Random = UnityEngine.Random;

[System.Serializable]
public class DNAandBloodType
{
	public string DNAString;
	public string BloodType;

	enum bloodtypes { oneg, opos, bneg, bpos, aneg, apos, abneg, abpos}
	
	private int BloodTypeGenerator;
	
	public void BloodTypeandDNA()
	{	
		DNAString = System.Guid.NewGuid().ToString();
		BloodTypeGenerator = Random.Range(1,1000);
		if (BloodTypeGenerator <= 364)
		{
			BloodType = "opos";
		}
		else if (BloodTypeGenerator > 364 && BloodTypeGenerator <= 407)
		{
			BloodType = "oneg";
		}
		else if (BloodTypeGenerator > 407 && BloodTypeGenerator <= 690)
		{
			BloodType = "apos";
		}
		else if (BloodTypeGenerator > 690 && BloodTypeGenerator <= 725)
		{
			BloodType = "aneg";
		}
		else if (BloodTypeGenerator > 725 && BloodTypeGenerator <= 931)
		{
			BloodType = "bpos";
		}
		else if (BloodTypeGenerator > 931 && BloodTypeGenerator <= 945)
		{
			BloodType = "bneg";
		}
		else if (BloodTypeGenerator > 945 && BloodTypeGenerator <= 995)
		{
			BloodType = "abpos";
		}
		else if (BloodTypeGenerator > 995 && BloodTypeGenerator <= 100)
		{
			BloodType = "abneg";
		}
	}
}