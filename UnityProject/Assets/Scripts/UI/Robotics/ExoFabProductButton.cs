using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExoFabProductButton : NetButton
{
	[HideInInspector]
	public MachineProduct machineProduct;

	[HideInInspector]
	public string categoryName;

	public override void ExecuteServer()
	{
		ServerMethod.Invoke();
	}
}