using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using Mirror;

public class MatrixSystemManager : MonoBehaviour
{
	private List<MatrixSystemBehaviour> systems = new List<MatrixSystemBehaviour>();
	private bool initialized;


	public void SelfInitialize()
	{
		StartCoroutine(Initialize());
	}



	public IEnumerator Initialize()
	{
		systems = systems.OrderByDescending(s => s.Priority).ToList();
		foreach (var system in systems)
		{
			try
			{
				system.Initialize();
			}
			catch (Exception e)
			{
				Chat.AddGameWideSystemMsgToChat($"<color=red>Error when initialising  {nameof(system)} on {this.name} Weird stuff might happen, check logs for error..</color>");
				Loggy.LogError(e.ToString());
			}

			yield return null;
		}
		initialized = true;
	}

	public void Register(MatrixSystemBehaviour system)
	{
		systems.Add(system);
	}

	public void UpdateAt(Vector3Int localPosition, SystemType ToUpDate = SystemType.All)
	{
		if (!initialized)
		{
			return;
		}

		//ensuring no metadata tiles are created at non-zero Z
		localPosition.z = 0;

		for (int i = 0; i < systems.Count; i++)
		{
			if ((ToUpDate & systems[i].SubsystemType) != 0)
			{
				systems[i].UpdateAt(localPosition);
			}
		}
	}
}
[Flags]
public enum SystemType
{
	None = 0,
	AtmosSystem = 1 << 0,
	MetaDataSystem = 1 << 1,
	All = ~None
}