using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APCInteract : NetworkTabTrigger
{
	private APC apc;
	void Start ()
	{
		apc = gameObject.GetComponent<APC>();
		if (apc == null)
		{
			Logger.LogError("Unable to find APC component!", Category.Electrical);
		}
	}
}
