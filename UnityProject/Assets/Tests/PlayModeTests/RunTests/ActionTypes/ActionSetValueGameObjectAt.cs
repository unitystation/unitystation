using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

// public class ActionSetValueGameObjectAt : MonoBehaviour
// {
public partial class TestAction
{
	public bool ShowSetValueGameObjectAt => SpecifiedAction == ActionType.SetValueGameObjectAt;

	[AllowNesting] [ShowIf("ShowSetValueGameObjectAt")] public SetValueGameObjectAt SetValueGameObjectAtData;

	[System.Serializable]
	public class SetValueGameObjectAt
	{
		public GameObject Prefab;
		public Vector3 PositionToCheck;
		public string ComponentName;

		public ClassVariableWriter ClassVariableWriter;

		public string CustomFailedText;

		public bool Initiate(TestRunSO TestRunSO)
		{
			var Magix = MatrixManager.AtPoint(PositionToCheck.RoundToInt(), true);
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
						ClassVariableWriter.SetValue(mono.GetType(), mono);
						return true;
					}
				}
			}
			TestRunSO.Report.AppendLine(CustomFailedText);
			TestRunSO.Report.AppendLine($"Could not find prefab {Prefab}");
			return false;
		}

	}

	public bool InitiateSetValueGameObjectAt(TestRunSO TestRunSO)
	{
		return SetValueGameObjectAtData.Initiate(TestRunSO);
	}
}
