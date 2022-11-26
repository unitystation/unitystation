using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGhost : MonoBehaviour
{
	public void OnClick()
	{
		Ghost();
	}

	public static void Ghost()
	{
		if (PlayerManager.LocalPlayerScript == null) return;

		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdAGhost();
	}
}
