using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableCoil : InputTrigger
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		Logger.Log("oh cool");
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}

		return true;
	}
	public override void UI_Interact(GameObject originator, string hand)
	{
		Logger.Log("tttt");
	}
}
