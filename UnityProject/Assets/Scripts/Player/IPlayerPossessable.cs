using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public interface IPlayerPossessable
{

	LivingHealthMasterBase LHMB { get; set; }

	virtual LivingHealthMasterBase LivingHealthMasterBase()
	{
		return LHMB;
	}




}
