using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items;
using Systems.Explosions;

public class Battery : MonoBehaviour, IEmpAble, IExaminable
{
	public int Watts = 9000;
	public int MaxWatts = 9000;

	public int InternalResistance = 240;

	public bool isBroken = false;

	public void OnEmp(int EmpStrength)
	{
		Watts -= EmpStrength * 100;
		Mathf.Clamp(Watts, 0, MaxWatts);

		if(EmpStrength > 50 && DMMath.Prob(25))
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
