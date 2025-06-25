using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;

public class MindManager : SingletonManager<MindManager>
{

	public Dictionary<uint, Mind> minds = new Dictionary<uint, Mind>();

	public uint MindID;


	public  List<AdminMindEntryData> GetMindStates()
	{

		List<AdminMindEntryData> Minds = new List<AdminMindEntryData>();

		foreach (var mind in minds)
		{
			Minds.Add(mind.Value.GetAdminMindEntryData());
		}


		return Minds;
	}

}
