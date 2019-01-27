using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class SubsystemManager : NetworkBehaviour
{
	private List<SubsystemBehaviour> systems = new List<SubsystemBehaviour>();
	private bool initialized;

	private void Start()
	{
		if (isServer)
		{
			systems = systems.OrderByDescending(s => s.Priority).ToList();
			Initialize();
		}
	}

	private void Initialize()
	{
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

	public void UpdateAt(Vector3Int position)
	{
		if (!initialized)
		{
			return;
		}

		for (int i = 0; i < systems.Count; i++)
		{
			systems[i].UpdateAt(position);
		}
	}
}