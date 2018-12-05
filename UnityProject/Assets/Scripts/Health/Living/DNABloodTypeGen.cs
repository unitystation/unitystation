using Random = UnityEngine.Random;

[System.Serializable]
public class DNAandBloodType
{
	public string DNAString;
	public BloodTypes BloodType;
	
	private int BloodTypeGenerator;
	
	public void BloodTypeandDNA()
	{	
		DNAString = System.Guid.NewGuid().ToString();
		BloodTypeGenerator = Random.Range(1,1000);
		if (BloodTypeGenerator <= 364)
		{
			BloodType = BloodTypes.oPos;
		}
		else if (BloodTypeGenerator > 364 && BloodTypeGenerator <= 407)
		{
			BloodType = BloodTypes.oNeg;
		}
		else if (BloodTypeGenerator > 407 && BloodTypeGenerator <= 690)
		{
			BloodType = BloodTypes.aPos;
		}
		else if (BloodTypeGenerator > 690 && BloodTypeGenerator <= 725)
		{
			BloodType = BloodTypes.aNeg;
		}
		else if (BloodTypeGenerator > 725 && BloodTypeGenerator <= 931)
		{
			BloodType = BloodTypes.bPos;
		}
		else if (BloodTypeGenerator > 931 && BloodTypeGenerator <= 945)
		{
			BloodType = BloodTypes.bNeg;
		}
		else if (BloodTypeGenerator > 945 && BloodTypeGenerator <= 995)
		{
			BloodType = BloodTypes.abPos;
		}
		else if (BloodTypeGenerator > 995 && BloodTypeGenerator <= 100)
		{
			BloodType = BloodTypes.abNeg;
		}
	}
}

public enum BloodTypes
{
	oNeg,
	oPos,
	bNeg,
	bPos,
	aNeg,
	aPos,
	abNeg,
	abPos
}