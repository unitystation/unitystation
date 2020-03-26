using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGhost : MonoBehaviour
{
	public void OnClick()
	{
		if (!PlayerManager.LocalPlayerScript.playerNetworkActions) return;
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAGhost();
	}
}
