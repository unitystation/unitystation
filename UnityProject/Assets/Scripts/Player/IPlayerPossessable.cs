using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public interface IPlayerPossessable
{

	public LivingHealthMasterBase LHMB { get; set; }

	public IPlayerPossessable Possessing { get; set; }

	public GameObject PossessingObject { get; set; }


	virtual LivingHealthMasterBase LivingHealthMasterBase()
	{
		return LHMB;
	}


	public bool IsRelatedToObject(GameObject Object)
	{
		if (PossessingObject == Object)
		{
			return true;
		}

		if (Possessing != null && Possessing.IsRelatedToObject(Object))
		{
			return true;
		}

		return false;
	}


}
