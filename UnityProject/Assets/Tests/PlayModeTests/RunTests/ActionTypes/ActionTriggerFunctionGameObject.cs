using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;
using Util;

// public class ActionTriggerFunctionGameObject : MonoBehaviour
// {
public partial class TestAction
{
	public bool ShowFunctionGameObject => SpecifiedAction == ActionType.TriggerFunctionGameObject;

	[AllowNesting] [ShowIf(nameof(ShowFunctionGameObject))] public FunctionGameObject FunctionGameObjectData;

	[System.Serializable]
	public class FunctionGameObject
	{
		public GameObject Prefab;
		public Vector3 PositionToCheck;
		public string ComponentName;

		public string MatrixName;

		public ClassFunctionInvoke ClassFunctionInvoke;

		public bool Initiate(TestRunSO TestRunSO)
		{
			var Magix = UsefulFunctions.GetCorrectMatrix(MatrixName, PositionToCheck);
			var List = Magix.Matrix.ServerObjects.Get(PositionToCheck.ToLocal(Magix).RoundToInt());

			var OriginalID = Prefab.GetComponent<PrefabTracker>().ForeverID;

			foreach (var Object in List)
			{
				var PrefabTracker = Object.GetComponent<PrefabTracker>();
				if (PrefabTracker != null)
				{
					if (PrefabTracker.ForeverID == OriginalID)
					{
						var mono = Object.GetComponent(ComponentName);
						ClassFunctionInvoke.Invoke(mono.GetType(), mono);
						return true;
					}
				}
			}
			TestRunSO.Report.AppendLine($"Could not find prefab {Prefab}");
			return false;
		}
	}

	public bool InitiateTriggerFunctionGameObject(TestRunSO TestRunSO)
	{
		return FunctionGameObjectData.Initiate(TestRunSO);
	}
}