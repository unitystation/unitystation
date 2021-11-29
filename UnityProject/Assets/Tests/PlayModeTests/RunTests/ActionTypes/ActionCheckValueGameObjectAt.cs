using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

// public class ActionCheckValueGameObjectAt : MonoBehaviour
// {
public partial class TestAction
{
    public bool ShowCheckValueGameObjectAt => SpecifiedAction == ActionType.CheckValueGameObjectAt;

    [AllowNesting] [ShowIf("ShowCheckValueGameObjectAt")] public CheckValueGameObjectAt CheckValueGameObjectAtData;

    [System.Serializable]
    public class CheckValueGameObjectAt
    {

        public GameObject Prefab;
        public Vector3 PositionToCheck;
        public string ComponentName;

        public ClassVariableRead Parameters = new ClassVariableRead();


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