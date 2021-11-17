using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using UnityEngine;

[System.Serializable]
public partial class TestAction
{
	public ActionType SpecifiedAction;

	public enum ActionType
	{
		None,
		SpawnX,
		KeyInput,
		PrefabAt,
		RespawnPlayer
	}


	public bool InitiateAction(TestRunSO TestRunSO)
	{
		switch (SpecifiedAction)
		{
			case ActionType.SpawnX:
				return InitiateSpawnX(TestRunSO);
			case ActionType.KeyInput:
				return InitiateKeyInput(TestRunSO);
			case ActionType.PrefabAt:
				return InitiatePrefabAt(TestRunSO);
			case ActionType.RespawnPlayer:
				return InitiateRespawnPlayer(TestRunSO);
		}

		return true;
	}
}