using System;

[Serializable]
public class DNAandBloodType
{
	public string DNAString;

	public BloodTypes BloodType;

	public BloodSplatType BloodColor;

	private static Random random = new Random();

	private float BloodTypeGenerator = random.Next(0,1000);

	public DNAandBloodType()
	{
		// Assigns DNA a GUID
		DNAString = Guid.NewGuid().ToString();
		// Assigns blood type by roll, note that there isnt an equal chance for each roll.
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
		else if (BloodTypeGenerator > 995 && BloodTypeGenerator <= 1000)
		{
			BloodType = BloodTypes.abNeg;
		}
	}
}

// Lists all the blood types.
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
