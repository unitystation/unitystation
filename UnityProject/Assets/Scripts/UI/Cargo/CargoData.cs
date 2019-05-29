using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoData : ScriptableObject
{
	//Stores all possible supplies broken into categories
	public List<CargoOrderCategory> Supplies = new List<CargoOrderCategory>();

	//TO-DO - comeup with clever idea for bounties
	public int GetBounty(ObjectBehaviour item)
	{
		return 50;
	}
}
