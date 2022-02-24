using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;
//
// public class ActionDebugPause : MonoBehaviour
// {

public partial class TestAction
{

	public bool InitiateDEBUG_Editor_Pause(TestRunSO TestRunSO)
	{
		Debug.Break();
		return true;
	}
}
