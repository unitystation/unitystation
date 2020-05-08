using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactorChamberRod : MonoBehaviour
{
	public RodType rodType;
}

public enum RodType
{
	Fuel,
	Control
}