using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICable //The work around for the sprite script 
{
	WiringColor CableType { get; set; }
	int DirectionEnd { get; set; }
	int DirectionStart { get; set; }
	bool IsCable { get; set; }
}