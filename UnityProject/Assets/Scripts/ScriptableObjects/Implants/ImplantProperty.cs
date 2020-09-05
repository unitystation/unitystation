using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ImplantProperty : ScriptableObject
{
	public abstract void ImplantUpdate(ImplantBase implant, LivingHealthMasterBase healthMaster);
}
