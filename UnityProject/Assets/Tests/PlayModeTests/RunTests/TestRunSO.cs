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
		public bool DebugThis = false; // TODO Test for this , since it would slow stuff down

		public bool DebugBreak = false;

		public float DebugSecondsPerAction;

		public bool RunThisone = false;


		public List<TestAction> TestActions = new List<TestAction>();

		[NonSerialized] public StringBuilder Report = new StringBuilder("\n");

		public YieldInstruction YieldInstruction;
		[NonSerialized] public bool BoolYieldInstruction;

		public IEnumerator RunTest(TestSingleton TestSingleton)
		{
			bool fail = false;
			foreach (var Action in TestActions)
			{
				yield return null;
				bool Status = true;

				Status = Action.InitiateAction(this);
				if (Status == false)
				{
					fail = true;
					break;
				}

				if (YieldInstruction != null)
				{
					yield return YieldInstruction;
					YieldInstruction = null;
					BoolYieldInstruction = false;
				}

				if (DebugThis)
				{
					yield return WaitFor.Seconds(DebugSecondsPerAction);
				}

				if (DebugBreak)
				{
					Debug.Break();
				}
			}

			TestSingleton.Results[this] = new Tuple<bool, StringBuilder>(fail, Report);
			//Assert.Fail(report.ToString());
		}
	}
}