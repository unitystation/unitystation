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
		KeyInput
	}


	public bool InitiateAction(TestRunSO TestRunSO)
	{
		switch (SpecifiedAction)
		{
			case ActionType.SpawnX:
				return InitiateSpawnX(TestRunSO);
				break;
			case ActionType.KeyInput:
				return InitiateKeyInput(TestRunSO);
				break;
		}

		return true;
	}
}