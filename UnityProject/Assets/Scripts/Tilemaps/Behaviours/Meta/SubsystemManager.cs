using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Initialisation;
using UnityEngine;

public class SubsystemManager : MonoBehaviour
{
	private List<SubsystemBehaviour> systems = new List<SubsystemBehaviour>();
	private bool initialized;

	private void Start() => LoadManager.RegisterAction(Init);

	void Init()
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
		
		yield return null; //So objects/doors can register themselves before atmospherics system scans for rooms
		for (int i = 0; i < systems.Count; i++)
		{
			systems[i].Initialize();
			yield return null;
		}

		initialized = true;
	}

	public void Register(SubsystemBehaviour system) => systems.Add(system);

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