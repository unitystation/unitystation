using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items;
using Systems.Explosions;

public class Battery : MonoBehaviour, IEMPAble, IExaminable
{
	public int Watts = 9000;
	public int MaxWatts = 9000;

	public int InternalResistance = 240;

	public bool isBroken = false;

	public void OnEMP(int EMPStrength)
    {
		Watts -= EMPStrength * 100;

		if (Watts < 0)
        {
			Watts = 0;
        }

		if(EMPStrength > 50 && UnityEngine.Random.Range(0,5) == 0)
        {
			isBroken = true;
        }
    }

	public string Examine(Vector3 worldPos = default)
	{
		string status = "";
        if (isBroken)
        {
			status = $"<color=red>It appears to be broken.";
        }
		return $"{gameObject.GetComponent<ItemAttributesV2>().InitialDescription}. Charge indicator shows a {Watts/MaxWatts*100} percent charge." +
			status;
	}
}
