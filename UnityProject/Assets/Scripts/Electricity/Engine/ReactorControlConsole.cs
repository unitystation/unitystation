using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactorControlConsole : MonoBehaviour, ISetMultitoolSlave
{
	public ReactorGraphiteChamber ReactorChambers = null;
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

		if (ReactorChambers != null)
		{
			ReactorChambers.SetControlRodDepth(Specified);
		}
	}

	//######################################## Multitool interaction ##################################
	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.ReactorChamber;
	public MultitoolConnectionType ConType  => conType;

	public void SetMaster(ISetMultitoolMaster Imaster)
	{
		var Chamber  = (Imaster as Component)?.gameObject.GetComponent<ReactorGraphiteChamber>();
		if (Chamber != null)
		{
			ReactorChambers = Chamber;
		}
	}

}