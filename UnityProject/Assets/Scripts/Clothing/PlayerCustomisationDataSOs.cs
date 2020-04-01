﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCustomisationDataSOs", menuName = "Singleton/PlayerCustomisationData")]
public class PlayerCustomisationDataSOs : SingletonScriptableObject<PlayerCustomisationDataSOs>
{
	[SerializeField]
	private List<PlayerCustomisationData> DataPCD;

	/// <summary>
	/// Returns a PlayerCustomisationData using the type and name.
	/// Returns null if not found.
	/// </summary>
	public PlayerCustomisationData Get(CustomisationType type, string customisationName)
	{
		return DataPCD.FirstOrDefault(data => data.Type == type && data.Name == customisationName);
	}

	/// <summary>
	/// Returns all PlayerCustomisationDatas of a certain type.
	/// </summary>
	public IEnumerable<PlayerCustomisationData> GetAll(CustomisationType type)
	{
		return DataPCD.Where(data => data.Type == type);
	}
}
