using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Logs;
using UnityEngine;

public class UpdateManagerTestsScript : MonoBehaviour
{

	public int Number = 10000;
	private List<Action> Actions = new List<Action>();

	public void Awake()
	{
		for (int i = 0; i < Number; i++)
		{
			var i1 = i;
			Actions.Add(() => { ThatOneUpdate(i1);} );
		}


	}

	[NaughtyAttributes.Button()]
	public void AddAlot()
	{
		var Stopwatch = new Stopwatch();

		Stopwatch.Start();

		foreach (var Action in Actions)
		{
			UpdateManager.Add(CallbackType.UPDATE, Action);
		}
		Stopwatch.Stop();

		Loggy.Log(Stopwatch.ElapsedTicks.ToString() + " < ElapsedTicks for AddAlot ");
		Stopwatch.Reset();

	}

	[NaughtyAttributes.Button()]
	public void RemoveAlot()
	{
		var Stopwatch = new Stopwatch();

		Stopwatch.Start();

		foreach (var Action in Actions)
		{
			UpdateManager.Remove(CallbackType.UPDATE, Action);
		}
		Stopwatch.Stop();
		Loggy.Log(Stopwatch.ElapsedMilliseconds.ToString() + " < ElapsedMilliseconds for RemoveAlot ");
		Stopwatch.Reset();

	}

	public void ThatOneUpdate(int nuvewrt)
	{

	}
}
