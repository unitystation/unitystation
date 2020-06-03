using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactorControlConsole : MonoBehaviour
{
	public List<ReactorGraphiteChamber> ReactorChambers = new List<ReactorGraphiteChamber>();
	public void RequestRelativeChange(float Multiplier)
	{
		Logger.Log("Multiplier " + Multiplier);
		float ControlRodDepthPercentage = 0;
		foreach (var Chamber in ReactorChambers)
		{
			if (Chamber.ControlRodDepthPercentage == 0)
			{
				Chamber.ControlRodDepthPercentage = 0.001f;
			}
			Chamber.ControlRodDepthPercentage *= (1/Multiplier);
			ControlRodDepthPercentage = Chamber.ControlRodDepthPercentage;
//Change this to use SetControlRodDepth
			if (Chamber.ControlRodDepthPercentage > 1)
			{
				Chamber.ControlRodDepthPercentage = 1;
			}
		}
		Logger.Log("ControlRodDepthPercentage " + ControlRodDepthPercentage);
	}

	public void SuchControllRodDepth(float Specified)
	{
		if (Specified > 1)
		{
			Specified = 1;
		}
		else if (0 > Specified)
		{
			Specified = 0;
		}

		foreach (var Chamber in ReactorChambers)
		{
			Chamber.ControlRodDepthPercentage = (Specified);
		}
	}

}