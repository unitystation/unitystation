using System.Collections;
using System.Collections.Generic;
using Systems.Research.Objects;
using UnityEngine;

public class ResearchLaserProjector : MonoBehaviour
{
	public ResearchServer Server;

	public LaserProjection LaserProjectionprefab;

	public ItemPlinth Pedestal;

	public LaserProjection LivingLine;

	[NaughtyAttributes.Button()]
	public void TriggerLaser()
	{


		if (Server == null)
		{
			Logger.LogError("Server Not Set");
			return;
		}


		if (LivingLine != null)
		{
			Destroy(LivingLine.gameObject);
		}

		LivingLine = Instantiate(LaserProjectionprefab, this.transform);
		LivingLine.Initialise(gameObject, Pedestal, this);
	}

}
