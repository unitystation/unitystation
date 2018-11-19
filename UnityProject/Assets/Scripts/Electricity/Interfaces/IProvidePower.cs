using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface  IProvidePower  {
	HashSet<IElectricityIO> connectedDevices {get; set;}
	List<IElectricityIO> DirectionWorkOnNextList  {get; set;}
}
