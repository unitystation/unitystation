using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MaterialNetLabel : NetLabel
{
	//Will allow the material label to be updated after the NetTab is opened.
	public override void AfterInit()
	{
		//Logger.Log("MaterialNetLabel: Updating " + Value);
		//UpdatePeepers();
	}
}