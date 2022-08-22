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

	[AllowNesting] [ShowIf(nameof(ShowSetMousePosition))] public SetMousePosition DataSetMousePosition;

	[System.Serializable]
	public class SetMousePosition
	{
		public Vector3 WorldPosition;

		public bool Initiate(TestRunSO TestRunSO)
		{
			if (TestRunSO.DebugThis)
			{
				ColorUtility.TryParseHtmlString("#ea9335", out var Orange);
				Debug.DrawLine(WorldPosition + (Vector3.right * 0.09f), WorldPosition + (Vector3.left * 0.09f), Orange, 30);
				Debug.DrawLine(WorldPosition + (Vector3.up * 0.09f), WorldPosition + (Vector3.down * 0.09f), Orange, 30);
			}


			InputManagerWrapper.MousePosition = Camera.main.WorldToScreenPoint(WorldPosition);
			return true;
		}
	}

	public bool InitiateSetMousePosition(TestRunSO TestRunSO)
	{
		return DataSetMousePosition.Initiate(TestRunSO);
	}
}