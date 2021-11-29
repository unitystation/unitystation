using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using UnityEngine;

[System.Serializable]
public partial class TestAction {
  public ActionType SpecifiedAction;

  public enum ActionType {
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
    SetValueGameObjectAt
  }

  public bool InitiateAction(TestRunSO TestRunSO) {
    switch (SpecifiedAction) {
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
    default:

      Logger.LogError($"Unset {SpecifiedAction}");
      return false;
    }

    return true;
  }
}