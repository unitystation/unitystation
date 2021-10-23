using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class SubsystemManager : MonoBehaviour
{
	private List<SubsystemBehaviour> systems = new List<SubsystemBehaviour>();
	private bool initialized;

	[Server]
	public void Initialize()
	{
		systems = systems.OrderByDescending(s => s.Priority).ToList();
		foreach (var system in systems)
		{
			system.Initialize();
		}
		initialized = true;
	}

	public void Register(SubsystemBehaviour system)
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
			if (ToUpDate.HasFlag(systems[i].SubsystemType))
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