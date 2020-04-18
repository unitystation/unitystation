using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
/// <summary>
/// Base Component for an object that needs a list of object triggers
/// ListOfObjectsEditor allows to fill the list with objects from the scene
/// </summary>
public class SwitchBase : NetworkBehaviour
{
	public List<ObjectTrigger> listOfTriggers;
}
