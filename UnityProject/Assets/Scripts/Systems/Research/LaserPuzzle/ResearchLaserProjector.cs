using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchLaserProjector : MonoBehaviour
{

	public LaserProjection LaserProjectionprefab;

	public ItemPlinth Pedestal;

	[NaughtyAttributes.Button()]
	public void TriggerLaser()
	{
		var line = Instantiate(LaserProjectionprefab, this.transform);

		line.Initialise(gameObject, Pedestal, this);
	}

}
