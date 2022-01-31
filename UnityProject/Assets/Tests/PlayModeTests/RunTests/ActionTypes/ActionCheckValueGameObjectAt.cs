using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;
using Util;

// public class ActionCheckValueGameObjectAt : MonoBehaviour
// {
public partial class TestAction
{
	public bool ShowCheckValueGameObjectAt => SpecifiedAction == ActionType.CheckValueGameObjectAt;

	[AllowNesting] [ShowIf(nameof(ShowCheckValueGameObjectAt))] public CheckValueGameObjectAt CheckValueGameObjectAtData;

	[System.Serializable]
	public class CheckValueGameObjectAt
	{

		public GameObject Prefab;
		public Vector3 WorldPosition;
		public string ComponentName;

		public string MatrixName;

		public ClassVariableRead Parameters = new ClassVariableRead();


		public string CustomFailedText;

		public bool Initiate(TestRunSO TestRunSO)
		{
			var Magix =  UsefulFunctions.GetCorrectMatrix(MatrixName, WorldPosition);
			var List = Magix.Matrix.ServerObjects.Get(WorldPosition.ToLocal(Magix).RoundToInt());

			var OriginalID = Prefab.GetComponent<PrefabTracker>().ForeverID;

			foreach (var Object in List)
			{
				var PrefabTracker = Object.GetComponent<PrefabTracker>();
				if (PrefabTracker != null)
				{
					if (PrefabTracker.ForeverID == OriginalID)
					{
						if (string.IsNullOrEmpty(ComponentName))
						{
							TestRunSO.Report.AppendLine(CustomFailedText);
							TestRunSO.Report.AppendLine($" ComponentName was not Set ");
							return false;
						}
						var mono = Object.GetComponent(ComponentName);
						if (Parameters.SatisfiesConditions(mono.GetType(), mono, out var ReportString))
						{
							return true;
						}
						else
						{
							TestRunSO.Report.AppendLine(CustomFailedText);
							TestRunSO.Report.AppendLine($" on the Game object {PrefabTracker.name} the Component Variable check on {ComponentName} Failed because of ");
							TestRunSO.Report.AppendLine(ReportString);
							return false;
						}

					}
				}
			}
			TestRunSO.Report.AppendLine(CustomFailedText);
			TestRunSO.Report.AppendLine($"Could not find prefab {Prefab}");
			return false;
		}
	}

	public bool InitiateCheckValueGameObjectAt(TestRunSO TestRunSO)
	{
		return CheckValueGameObjectAtData.Initiate(TestRunSO);
	}
}