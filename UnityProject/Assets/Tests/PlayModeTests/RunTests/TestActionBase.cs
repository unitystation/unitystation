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
		RespawnPlayer,
		ActionWaite,
		SetTile,
		HasTile,
		SetMousePosition,
		IsInPlayerInventory,
		CheckValueGameObjectAt,
		SetValueGameObjectAt,
		AssessMetaDataNode,
		TriggerFunctionGameObject,
		DEBUG_Editor_Pause
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
			case ActionType.ActionWaite:
				return InitiateActionWaite(TestRunSO);
			case ActionType.SetTile:
				return InitiateSetTile(TestRunSO);
			case ActionType.HasTile:
				return InitiateHasTile(TestRunSO);
			case ActionType.SetMousePosition:
				return InitiateSetMousePosition(TestRunSO);
			case ActionType.IsInPlayerInventory:
				return InitiateIsInPlayerInventory(TestRunSO);
			case ActionType.CheckValueGameObjectAt:
				return InitiateCheckValueGameObjectAt(TestRunSO);
			case ActionType.SetValueGameObjectAt:
				return InitiateSetValueGameObjectAt(TestRunSO);
			case ActionType.AssessMetaDataNode:
				return InitiateAssessMetaDataNode(TestRunSO);
			case ActionType.TriggerFunctionGameObject:
				return InitiateTriggerFunctionGameObject(TestRunSO);
			case ActionType.DEBUG_Editor_Pause:
				return InitiateDEBUG_Editor_Pause(TestRunSO);
			default:

				Logger.LogError($"Unset {SpecifiedAction}");
				return false;
		}

		return true;
	}

	public class UsefulFunctions
	{
		public static MatrixInfo GetCorrectMatrix(string Name, Vector3 WorldPosition)
		{
			if (string.IsNullOrEmpty(Name))
			{
				return  MatrixManager.AtPoint(WorldPosition.RoundToInt(), true);
			}
			else
			{
				return  MatrixManager.GetByName_DEBUG_ONLY(Name);
			}

		}
	}
}