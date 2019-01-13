using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDeviceControl  {

	///<Summary>
	/// Checking if the electrical object has been removed from the circuit in a state check
	///<Summary>
	void PotentialDestroyed ();

	void TurnOffCleanup ();
}
