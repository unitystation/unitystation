using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class SubsystemManager : NetworkBehaviour
{
	private List<SubsystemBehaviour> systems = new List<SubsystemBehaviour>();
	private bool initialized;

	private void Start()
	{
		systems = systems.OrderByDescending(s => s.Priority).ToList();
		StartCoroutine(Initialize());
	}

	IEnumerator Initialize()
	{
		while (!MatrixManager.IsInitialized)
		{
			yield return WaitFor.EndOfFrame;
		}

		for (int i = 0; i < systems.Count; i++)
		{
			systems[i].Initialize();
		}

		initialized = true;
	}

	public void Register(SubsystemBehaviour system)
	{
		systems.Add(system);
	}

	public void UpdateAt(Vector3Int localPosition)
	{
		if (!initialized)
		{
			return;
		}

		//ensuring no metadata tiles are created at non-zero Z
		localPosition.z = 0;

		for (int i = 0; i < systems.Count; i++)
		{
			systems[i].UpdateAt(localPosition);
		}
	}
}