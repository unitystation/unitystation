using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_APC : NetTab
{
	private APCInteract apcInteract;
	private APCInteract APCInteract
	{
		get
		{
			if ( !apcInteract)
			{
				apcInteract = Provider.GetComponent<APCInteract>();
			}
			return apcInteract;
		}
	}
}
