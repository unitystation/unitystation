using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class TransformerCalculations  {


	//This should be the split up into different functions depending on what stage you want
	//This will give you back if you give it the right data, how it modifies resistance, and then How it modifies current  
	public static Tuple<float, float> TransformerCalculate(TransformerModule TransformInformation,  float ResistanceToModify = 0, float Voltage = 0, float ResistanceModified = 0)
	{
		
	
		//Logger.Log(TransformInformation.TurnRatio + " < TurnRatio " + TransformInformation.VoltageLimiting + " < VoltageLimiting " + TransformInformation.VoltageLimitedTo + " < VoltageLimitedTo ");
		if (!(ResistanceToModify == 0))
		{
			//float R2 = ResistanceToModify;
			//float I2 = 1/ResistanceToModify;
			//float V2 = 1;

			//float Turn_ratio = TransformInformation.TurnRatio;

			//float V1 = (V2*Turn_ratio);
			//float I1 = (V2/V1)*I2;
			//float R1 = V1/I1;
			Tuple<float, float> returns = new Tuple<float, float>(
				(float)Math.Pow(TransformInformation.TurnRatio, 2.0) * (ResistanceToModify),
				0
			);
			return (returns);
		}
		if (!(Voltage == 0))
		{
			float offcut = 0;
			float V2 = Voltage / TransformInformation.TurnRatio;
			float R2 = V2 / ((Voltage / V2) * (Voltage / ResistanceModified));
			if (!(TransformInformation.VoltageLimiting == 0))
			{ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/
			  //float VVoltage = ElectricityFunctions.WorkOutVoltage(TransformInformation.ControllingNode.Node);
				//Logger.Log("V2 > " + V2 + " VoltageLimitedTo > " + TransformInformation.VoltageLimitedTo, Category.Electrical);
				if (V2 > TransformInformation.VoltageLimiting) { 
					offcut = ((V2) - TransformInformation.VoltageLimitedTo);
					V2 = V2 - offcut;
					if (V2 < 0)
					{
						V2 = 0;
					}
				}
			}
			float I2 = V2 / R2;
			Tuple<float, float> returns = new Tuple<float, float>(
				I2,
				offcut
			);
			return (returns);
		}
		Tuple<float, float> returnsE = new Tuple<float, float>(0.0f, 0);
		return (returnsE);
	}

	public static Tuple<float, float> ResistanceStageTransformerCalculate(TransformerModule TransformInformation, float ResistanceToModify = 0, bool FromHighSide = false)
	{
		float TurnRatio = TransformInformation.TurnRatio;
		if (FromHighSide) //Since is travelling different directions
		{
			TurnRatio = 1 / TransformInformation.TurnRatio;
		}
		//Logger.Log(TransformInformation.TurnRatio + " < TurnRatio " + TransformInformation.VoltageLimiting + " < VoltageLimiting " + TransformInformation.VoltageLimitedTo + " < VoltageLimitedTo ");

		//float R2 = ResistanceToModify;
		//float I2 = 1/ResistanceToModify;
		//float V2 = 1;

		//float Turn_ratio = TransformInformation.TurnRatio;

		//float V1 = (V2*Turn_ratio);
		//float I1 = (V2/V1)*I2;
		//float R1 = V1/I1;
		Tuple<float, float> returns = new Tuple<float, float>(
			(float)Math.Pow(TurnRatio, 2.0) * (ResistanceToModify),
			0);
		return (returns);

	}


	public static Tuple<float, float> ElectricalStageTransformerCalculate(TransformerModule TransformInformation, float Voltage = 0, float ResistanceModified = 0, bool FromHighSide = false)
	{
		float TurnRatio = TransformInformation.TurnRatio;
		if (!FromHighSide)
		{
			TurnRatio = 1 / TransformInformation.TurnRatio;
		}
		//Logger.Log(TransformInformation.TurnRatio + " < TurnRatio " + TransformInformation.VoltageLimiting + " < VoltageLimiting " + TransformInformation.VoltageLimitedTo + " < VoltageLimitedTo ");
		if (!(Voltage == 0))
		{
			float offcut = 0;
			float V2 = Voltage / TurnRatio;
			float R2 = V2 / ((Voltage / V2) * (Voltage / ResistanceModified));
			if (!(TransformInformation.VoltageLimiting == 0))
			{ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/
			  //float VVoltage = ElectricityFunctions.WorkOutVoltage(TransformInformation.ControllingNode.Node);
				if (V2 > TransformInformation.VoltageLimiting)
				{
					offcut = ((V2) - TransformInformation.VoltageLimitedTo);
					V2 = V2 - offcut;
					if (V2 < 0)
					{
						V2 = 0;
					}
				}
			}
			float I2 = V2 / R2;
			Tuple<float, float> returns = new Tuple<float, float>(
				I2,
				offcut
			);
			return (returns);
		}
		Tuple<float, float> returnsE = new Tuple<float, float>(0.0f, 0);
		return (returnsE);
	}
}
