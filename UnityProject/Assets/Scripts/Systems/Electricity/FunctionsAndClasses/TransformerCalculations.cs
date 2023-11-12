using System;
using System.Collections;
using System.Collections.Generic;

namespace Systems.Electricity.NodeModules
{
	public static class TransformerCalculations
	{
		//This should be the split up into different functions depending on what stage you want
		//This will give you back if you give it the right data, how it modifies resistance, and then How it modifies current
		public static Tuple<float, float> TransformerCalculate(TransformerModule TransformInformation, float ResistanceToModify = 0, float Voltage = 0, float ResistanceModified = 0)
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

		public static ResistanceWrap ResistanceStageTransformerCalculate(TransformerModule TransformInformation,
				ResistanceWrap ResistanceToModify,
				bool FromHighSide = false)
		{

			//Logger.Log("TransformInformation!!!!!!!!!" + TransformInformation.gameObject);
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
			//Logger.Log(((Math.Pow(TurnRatio, 2.0))) + " (Math.Pow(TurnRatio, 2.0))");
			//Logger.Log(ResistanceToModify.ToString() + " HHHHHH");
			var VIRResistances = ElectricalPool.GetResistanceWrap();
			VIRResistances.SetUp(ResistanceToModify);
			VIRResistances.Multiply((float)Math.Pow(TurnRatio, 2.0));
			//Logger.Log(VIRResistances.ToString() + " HHHHHH");
			return (VIRResistances);

		}


		public static VIRCurrent ElectricalStageTransformerCalculate(TransformerModule TransformInformation,
				VIRCurrent Current,
				float ResistanceModified,
				float inVoltage,
				bool FromHighSide = false)
		{
			double TurnRatio = TransformInformation.TurnRatio;
			if (!FromHighSide)
			{
				TurnRatio = 1 / TransformInformation.TurnRatio;
			}

			double Voltage = (Current.Current() * ResistanceModified);
			//Logger.Log("Current.Current() > " + Current.Current() + " ResistanceModified > " + ResistanceModified);

			//Logger.Log(TransformInformation.TurnRatio + " < TurnRatio " + TransformInformation.VoltageLimiting + " < VoltageLimiting " + TransformInformation.VoltageLimitedTo + " < VoltageLimitedTo ");
			if (Voltage != 0)
			{
				double offcut = 0;

				//double V2 = Voltage / TurnRatio;
				//double R2 = V2 / ((Voltage / V2) * (Voltage / ResistanceModified));
				//double R2 = (ResistanceModified / Math.Pow(TurnRatio, 2.0));

				double V2 = Voltage / TurnRatio;
				double R2 = V2 / ((Voltage / V2) * (Voltage / ResistanceModified));
				//Logger.Log(R2 + " < R2 " + V2 + " < V2 " + ResistanceModified + " < ResistanceModified" + TurnRatio + " < TurnRatio " + Voltage + " < Voltage ");
				if (TransformInformation.VoltageLimiting != 0)
				{ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/
				  //float VVoltage = ElectricityFunctions.WorkOutVoltage(TransformInformation.ControllingNode.Node);
					if (V2 + inVoltage > TransformInformation.VoltageLimiting)
					{
						offcut = ((V2 + inVoltage) - TransformInformation.VoltageLimitedTo);
						V2 = V2 - offcut;
						if (V2 < 0)
						{
							V2 = 0;
						}
					}
				}

				//inVoltage
				TurnRatio = TurnRatio * (V2 / (Voltage / TurnRatio));

				//Logger.Log("V2 " + V2.ToString());
				//Logger.Log("I2 " + I2.ToString());
				//Logger.Log("Current.Current() " + Current.Current().ToString());
				var ReturnCurrent = Current.SplitCurrent((float)TurnRatio);
				//Logger.Log("ReturnCurrent " + ReturnCurrent.Current().ToString());
				return (ReturnCurrent);
			}
			return (Current);
		}
	}
}
