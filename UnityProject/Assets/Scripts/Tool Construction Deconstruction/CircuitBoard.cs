using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitBoard : MonoBehaviour
{
	public GameObject ConstructionTarget;
	public int StartAtStage;

	void Start()
	{
		if (ConstructionTarget != null)
		{
			this.gameObject.name = ConstructionTarget.name + "Circuit board";
		}
		else {
			this.gameObject.name = "Blank circuit board";
		}
	}
}
