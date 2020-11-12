using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BVOld_checker : MonoBehaviour
{
	void Start()
	{
		var name = gameObject.name;
		while (name.Contains("BVOld_"))
	    {
		    name = name.Replace("BVOld_", "");
	    }

	    gameObject.name = "BVOld_" + name;
	}

}
