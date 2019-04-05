﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class TransformerCalculations  {
	//This should be the split up into different functions depending on what stage you want
	//This will give you back if you give it the right data, how it modifies resistance, and then How it modifies current  
	public static Tuple<float,float> TransformerCalculate( PowerSupplyControlInheritance TransformInformation, float ResistanceToModify = 0, float Voltage = 0, float ResistanceModified = 0, float ActualCurrent = 0 ){
		if (!(ResistanceToModify == 0)) {
			//float R2 = ResistanceToModify;
			//float I2 = 1/ResistanceToModify;
			//float V2 = 1;

			//float Turn_ratio = TransformInformation.TurnRatio;

			//float V1 = (V2*Turn_ratio);
			//float I1 = (V2/V1)*I2;
			//float R1 = V1/I1;
			Tuple<float,float> returns = new Tuple<float, float>(
				(float)Math.Pow(TransformInformation.TurnRatio, 2.0) * (ResistanceToModify),
				0
			);
			return(returns);
		}
		if (!(Voltage == 0)) {
			float offcut = 0;

			float V2 = Voltage/TransformInformation.TurnRatio;
			float R2 = V2 / ((Voltage / V2) * (Voltage / ResistanceModified));
			if (!(TransformInformation.VoltageLimiting == 0)){ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/

				float SUBV2 = (ActualCurrent * ResistanceModified)/TransformInformation.TurnRatio;

				if ((V2 + SUBV2) > TransformInformation.VoltageLimiting) { 
					offcut = ((V2 + SUBV2) - TransformInformation.VoltageLimitedTo)/ R2;
					V2 = TransformInformation.VoltageLimitedTo - SUBV2;
					if (V2 < 0) {
						V2 = 0;
					}
				}
			}
			float I2 = V2/R2;
			Tuple<float,float> returns = new Tuple<float, float>(
				I2, 
				offcut
			);
			return(returns);
		}
		Tuple<float,float> returnsE = new Tuple<float, float>(0.0f, 0);
		return(returnsE);
	}

}
