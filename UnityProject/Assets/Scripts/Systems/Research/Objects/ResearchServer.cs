using System;
using System.Collections;
using System.IO;
using Mirror;
using ScriptableObjects.Research;
using UnityEngine;

namespace Systems.Research.Objects
{
	public class ResearchServer : NetworkBehaviour
	{
		//TODO: PLACE HOLDER UNTIL WE GET A TECHWEB EDITOR OF SOME SORT
		[SerializeField] private DefaultTechwebData defaultTechwebData;
		//TODO: PLACEHOLDER, TECHWEBS SHOULD BE STORED LOCALLY ON IN-GAME DISKS/CIRCUITS TO BE STOLEN AND MERGED
		[SyncVar] private Techweb techweb = new Techweb();
		//TODO : PLACEHOLDER, THIS PATH MUST BE ASSIGNED ON THE CIRCUIT/DISK INSTEAD OF ON THE SERVER PREFAB
		[SerializeField] private string techWebPath = "/GameData/Research/";
		[SerializeField] private string techWebFileName = "TechwebData.json";
		[SerializeField] private int researchPointsTrickl = 25;
		[SerializeField] private int TrickleTime = 60; //seconds

		private void Awake()
		{
			if (File.Exists($"{techWebPath}{techWebFileName}") == false) defaultTechwebData.GenerateDefaultData();
			techweb.LoadTechweb($"{techWebPath}{techWebFileName}");
			StartCoroutine(TrickleResources());
		}

		private void OnDisable()
		{
			StopCoroutine(TrickleResources());
		}

		private IEnumerator TrickleResources()
		{
			while (this != null || techweb != null)
			{
				yield return WaitFor.Seconds(TrickleTime);
				techweb.AddResearchPoints(researchPointsTrickl);
			}
		}
	}
}