using Random = UnityEngine.Random;

[System.Serializable]
public class DNAandBloodType
{
	public string DNAString;
	
	public BloodTypes BloodType;
	
	private float BloodTypeGenerator;
	
	public DNAandBloodType()
	{	
		// Assigns DNA a GUID
		DNAString = System.Guid.NewGuid().ToString();
		// Rolls from 1 to 1000 to determine blood chance.
		BloodTypeGenerator = Random.Range(1.0f,1000.0f);
		// Assigns blood type by roll, note that there isnt an equal chance for each roll.
		if (BloodTypeGenerator <= 364.0f)
		{
			BloodType = BloodTypes.oPos;
		}
		else if (BloodTypeGenerator > 364.0f && BloodTypeGenerator <= 407.0f)
		{
			BloodType = BloodTypes.oNeg;
		}
		else if (BloodTypeGenerator > 407.0f && BloodTypeGenerator <= 690.0f)
		{
			BloodType = BloodTypes.aPos;
		}
		else if (BloodTypeGenerator > 690.0f && BloodTypeGenerator <= 725.0f)
		{
			BloodType = BloodTypes.aNeg;
		}
		else if (BloodTypeGenerator > 725.0f && BloodTypeGenerator <= 931.0f)
		{
			BloodType = BloodTypes.bPos;
		}
		else if (BloodTypeGenerator > 931.0f && BloodTypeGenerator <= 945.0f)
		{
			BloodType = BloodTypes.bNeg;
		}
		else if (BloodTypeGenerator > 945.0f && BloodTypeGenerator <= 995.0f)
		{
			BloodType = BloodTypes.abPos;
		}
		else if (BloodTypeGenerator > 995.0f && BloodTypeGenerator <= 1000.0f)
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