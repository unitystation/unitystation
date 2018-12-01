using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class TransformerCalculations  {
	
	public static Tuple<float,float> TransformerCalculate( Itransformer TransformInformation, float ResistanceToModify = 0, float Voltage = 0, float ResistanceModified = 0, float ActualCurrent = 0 ){
		if (!(ResistanceToModify == 0)) {
			float R2 = ResistanceToModify;
			float I2 = 1/ResistanceToModify;
			float V2 = 1;

			float Turn_ratio = TransformInformation.TurnRatio;

			float V1 = (V2*Turn_ratio);
			float I1 = (V2/V1)*I2;
			float R1 = V1/I1;
			Tuple<float,float> returns = new Tuple<float, float>(
				R1, 
				0
			);
			return(returns);
		}
		if (!(Voltage == 0)) {
			float offcut = 0;
			float V1 = Voltage;
			float R1 = ResistanceModified;
			float I1 = V1/R1;
			float Turn_ratio = TransformInformation.TurnRatio;
			float V2 = V1/Turn_ratio;
			float IntervalI2 = (V1 / V2) * I1;
			float R2 = V2 / IntervalI2;
			if (!(TransformInformation.VoltageLimiting == 0)){ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/
				float ActualVoltage = ActualCurrent * ResistanceModified;

				float SUBV1 = ActualVoltage;
				float SUBR1 = ResistanceModified;
				float SUBI1 = ActualCurrent;

				float SUBV2 = SUBV1/Turn_ratio;
				float SUBI2 = (SUBV1 / SUBV2) * SUBI1;
				float SUBR2 = SUBV2 / SUBI2;
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
