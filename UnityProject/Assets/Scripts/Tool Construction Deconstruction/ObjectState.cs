using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// The state that a Object can be in E.G Frozen or in construction, Meant so Scripts can react appropriately
/// </summary>
public enum ObjectState 
{
	Normal,
	InConstruction, 
	Frozen,
	Timestop,
	//etc ..
}
