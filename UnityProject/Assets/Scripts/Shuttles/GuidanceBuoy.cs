using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GuidanceBuoy : NetworkBehaviour
{
	public MoveStep Out;
	public MoveStep In;
	public RegisterTile RegisterTile;

	public void Awake()
	{
		RegisterTile = this.GetComponent<RegisterTile>();
	}
}


[System.Serializable]
public class MoveStep
{
	//On set
	public bool UseConnectorAsCentreOfShuttle;
	public OrientationEnum DesiredFaceDirection = OrientationEnum.Default;


	//On Reach
	public ShuttleConnector ConnectTo;
	public GuidanceBuoy NextInLine;
	public bool IsEnd = false;
}

