using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


public class SubsystemManager : NetworkBehaviour
{
	private List<SubsystemBehaviour> systems = new List<SubsystemBehaviour>();

	public void Start()
	{
		systems = systems.OrderByDescending(s => s.Priority).ToList();
		Initialize();
	}

	private void Initialize()
	{
		for (int i = 0; i < systems.Count; i++)
		{
			systems[i].Initialize();
		}
	}

	public void Register(SubsystemBehaviour subsystem)
	{
		systems.Add(subsystem);
	}

	public void UpdateAt(Vector3Int position)
	{
		for (int i = 0; i < systems.Count; i++)
		{
			systems[i].UpdateAt(position);
		}
	}
}