using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;


namespace GameRunTests
{
	[CreateAssetMenu(fileName = "TestRunSO", menuName = "ScriptableObjects/TestRunSO")]
	public class TestRunSO : ScriptableObject
	{
		public List<TestAction> TestActions = new List<TestAction>();

		[NonSerialized] public StringBuilder Report = new StringBuilder("\n");

		public IEnumerator RunTest(TestSingleton TestSingleton)
		{
			bool fail = false;
			foreach (var Action in TestActions)
			{
				yield return null;
				var Status = Action.InitiateAction(this);
				if (Status == false)
				{
					fail = true;
					break;
				}
			}
			TestSingleton.Results[this] = new Tuple<bool, StringBuilder>(fail, Report);
			//Assert.Fail(report.ToString());
		}
	}
}