using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;


// public class ActionSetMousePosition : MonoBehaviour
// {
public partial class TestAction
{
    public bool ShowSetMousePosition => SpecifiedAction == ActionType.SetMousePosition;

    [AllowNesting] [ShowIf("ShowSetMousePosition")] public SetMousePosition DataSetMousePosition;

    [System.Serializable]
    public class SetMousePosition
    {
        public Vector3 WorldPosition;

        public bool Initiate(TestRunSO TestRunSO)
        {
            InputManagerWrapper.MousePosition = Camera.main.WorldToScreenPoint(WorldPosition);
            return true;
        }
    }

    public bool InitiateSetMousePosition(TestRunSO TestRunSO)
    {
        return DataSetMousePosition.Initiate(TestRunSO);
    }
}