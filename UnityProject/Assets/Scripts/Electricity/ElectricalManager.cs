using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalManager : MonoBehaviour {
	void Update () { //since you can't add MonoBehaviour Too static
		ElectricalSynchronisation.Update ();
	}
}
