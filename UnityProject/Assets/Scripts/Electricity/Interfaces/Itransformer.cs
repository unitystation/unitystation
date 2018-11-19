using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Itransformer {
	float TurnRatio  {get; set;} 
	float VoltageLimiting {get; set;} 
	float VoltageLimitedTo  {get; set;} 
}
