using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using Logs;
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
		DEBUG_Editor_Pause,
		ManipulatePlayersInventory,
	}

	public bool InitiateAction(TestRunSO TestRunSO)
	{
		switch (SpecifiedAction)
		{
			case ActionType.SpawnX:
				return SpawnXData.InitiateSpawnX(TestRunSO);
			case ActionType.KeyInput:
				return DataKeyInput.InitiateKeyInput(TestRunSO);
			case ActionType.PrefabAt:
				return DataShowPrefab.Initiate(TestRunSO);
			case ActionType.RespawnPlayer:
				return RespawnPlayerData.Initiate(TestRunSO);
			case ActionType.ActionWaite:
				return DataActionWaite.Initiate(TestRunSO);
			case ActionType.SetTile:
				return SetTileData.Initiate(TestRunSO);
			case ActionType.HasTile:
				return HasTileData.Initiate(TestRunSO);
			case ActionType.SetMousePosition:
				return DataSetMousePosition.Initiate(TestRunSO);
			case ActionType.IsInPlayerInventory:
				return DataIsInPlayerInventory.Initiate(TestRunSO);
			case ActionType.CheckValueGameObjectAt:
				return CheckValueGameObjectAtData.Initiate(TestRunSO);
			case ActionType.SetValueGameObjectAt:
				return SetValueGameObjectAtData.Initiate(TestRunSO);
			case ActionType.AssessMetaDataNode:
				return DataAssessMetaDataNode.Initiate(TestRunSO);
			case ActionType.TriggerFunctionGameObject:
				return FunctionGameObjectData.Initiate(TestRunSO);
			case ActionType.DEBUG_Editor_Pause:
				return InitiateDEBUG_Editor_Pause(TestRunSO);
			case ActionType.ManipulatePlayersInventory:
				return InManipulatePlayersInventory.Initiate(TestRunSO);
			default:
				Loggy.LogError($"Unset {SpecifiedAction}");
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