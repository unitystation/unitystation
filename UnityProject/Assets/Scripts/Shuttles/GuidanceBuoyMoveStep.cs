using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GuidanceBuoyMoveStep
{
	//On set
	public bool UseConnectorAsCentreOfShuttle;
	public OrientationEnum DesiredFaceDirection = OrientationEnum.Default;


	//On Reach
	public ShuttleConnector ConnectTo;
	public GuidanceBuoy NextInLine;
	public bool IsEnd = false;
}

