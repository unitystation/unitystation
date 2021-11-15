using System.Collections;
using System.Collections.Generic;
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


	public void InitiateAction()
	{
		switch (SpecifiedAction)
		{
			case ActionType.SpawnX:
				InitiateSpawnX();
				break;
			case ActionType.KeyInput:
				InitiateKeyInput();
				break;
		}
	}
}