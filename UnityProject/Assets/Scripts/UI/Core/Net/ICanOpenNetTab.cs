using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Called before server adds new player to netTab, this interface should be put on scripts on the provider
//NOT ON THE GUI script
public interface ICanOpenNetTab
{
	bool CanOpenNetTab(GameObject playerObject);
}
