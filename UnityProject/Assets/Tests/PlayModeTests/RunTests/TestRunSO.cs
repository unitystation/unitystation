using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "TestRunSO", menuName = "ScriptableObjects/TestRunSO")]
public class TestRunSO : ScriptableObject
{
	[ReorderableList]
	public List<TestAction> TestActions = new List<TestAction>();


	public bool RunTest()
	{

		foreach (var Action in TestActions)
		{
			Action.InitiateAction();
		}

		//Assert.Fail(report.ToString());
		return true;
	}
}
