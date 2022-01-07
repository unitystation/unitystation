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

	public void OnEMP(int EMPStrength)
    {
		Watts -= EMPStrength * 100;
		if (Watts < 0)
        {
			Watts = 0;
        }
    }

	public string Examine(Vector3 worldPos = default)
	{
		return $"{gameObject.GetComponent<ItemAttributesV2>().InitialDescription}. Charge indicator shows a {Watts/MaxWatts*100} percent charge.";
	}
}
