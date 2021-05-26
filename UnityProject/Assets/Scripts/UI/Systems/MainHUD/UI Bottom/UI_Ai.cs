using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using UnityEngine;

public class UI_Ai : MonoBehaviour
{
	[HideInInspector]
	public AiPlayer aiPlayer = null;

	[HideInInspector]
	public AiMouseInputController controller = null;

	public void SetUp(AiPlayer player)
	{
		aiPlayer = player;
		controller = aiPlayer.GetComponent<AiMouseInputController>();
	}

	public void JumpToCore()
	{
		if (aiPlayer == null) return;

		aiPlayer.CmdTeleportToCore();
	}
}
