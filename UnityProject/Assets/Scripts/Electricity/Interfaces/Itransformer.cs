using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Itransformer {
	float TurnRatio  {get; set;}  //They desire TurnRatio There should be really linked to an input and output, Could be added in the future
	float VoltageLimiting {get; set;} //If it requires VoltageLimiting and  At what point the VoltageLimiting will kick in
	float VoltageLimitedTo  {get; set;}  //what it will be limited to
}
