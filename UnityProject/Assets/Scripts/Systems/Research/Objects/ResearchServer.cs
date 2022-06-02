using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using ScriptableObjects.Research;
using UnityEngine;
using Systems.Research.Data;
using Systems.ObjectConnection;
using Random = UnityEngine.Random;

namespace Systems.Research.Objects
{
	public class ResearchServer : NetworkBehaviour, IMultitoolMasterable
	{
		//TODO: PLACE HOLDER UNTIL WE GET A TECHWEB EDITOR OF SOME SORT
		[SerializeField] private DefaultTechwebData defaultTechwebData;
		//TODO: PLACEHOLDER, TECHWEBS SHOULD BE STORED LOCALLY ON IN-GAME DISKS/CIRCUITS TO BE STOLEN AND MERGED
		[SyncVar] public Techweb techweb = new Techweb();
		//TODO : PLACEHOLDER, THIS PATH MUST BE ASSIGNED ON THE CIRCUIT/DISK INSTEAD OF ON THE SERVER PREFAB
		[SerializeField] private string techWebPath = "/GameData/Research/";
		[SerializeField] private string techWebFileName = "TechwebData.json";
		[SerializeField] private int researchPointsTrickl = 25;
		[SerializeField] private int TrickleTime = 60; //seconds

		public List<string> AvailableDesigns = new List<string>();

		[NonSerialized] public Action<int,List<string>> TechWebUpdateEvent;

		private void Awake()
		{
			SetYieldTargets();
			if (File.Exists($"{techWebPath}{techWebFileName}") == false) defaultTechwebData.GenerateDefaultData();
			techweb.LoadTechweb($"{techWebPath}{techWebFileName}");
			StartCoroutine(TrickleResources());
			UpdateAvailableDesigns();
		}

		private void OnDisable()
		{
			StopCoroutine(TrickleResources());
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			TechWebUpdateEvent?.Invoke(0, null);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			TechWebUpdateEvent?.Invoke(1, AvailableDesigns);
		}

		//TODO
		//Once Techweb is fully implemented:
		//TechWebUpdateEvent should be invoked with (1, UpdateAvailiableDesigns()) whenever a new node is researched
		//TechWebUpdateEvent should be invoked with (0, null) when a Techweb Drive is removed from its server
		//TechWebUpdateEvent should be invoked with (1, UpdateAvailiableDesigns()) whenever a new drive is added to a server

		public List<string> UpdateAvailableDesigns()
		{
			List<string> availableDesigns = AvailableDesigns;

			foreach (Technology tech in techweb.researchedTech)
			{
				foreach(string str in tech.DesignIDs)
				{
					if (!availableDesigns.Contains(str))
					{
						availableDesigns.Add(str);
					}
				}
			}

			AvailableDesigns = availableDesigns;

			return AvailableDesigns;
		}
		private IEnumerator TrickleResources()
		{
			while (this != null || techweb != null)
			{
				yield return WaitFor.Seconds(TrickleTime);
				techweb.AddResearchPoints(researchPointsTrickl);
			}
		}

		public void AddResearchPoints(int points)
		{
			techweb.AddResearchPoints(points);
		}

		#region MultitoolInteraction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.ResearchServer;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion

		#region Blast Yield Detector

		/// <summary>
		/// Target ExplosionStrength required to award easy point amount
		/// </summary>
		public int easyBlastYieldDetectorTarget;
		/// <summary>
		/// Target ExplosionStrength required to award hard point amount, max amount
		/// </summary>
		public int hardBlastYieldDetectorTarget;

		/// <summary>
		/// ExplosionBase ExplosionStrength threshold to be considered a significant enough
		/// explosion to warrant measuring
		/// </summary>
		public int yieldTargetRangeMinimum;

		/// <summary>
		/// ExplosionBase ExplosionStrength limit close to the highest base static explosion of
		/// obtainable ExplosionBase items, currently the syndicate macrobomb
		/// </summary>
		public int yieldTargetRangeMaximum;


		/// <summary>
		/// Sets yield targets to randomised values set between 1000 and 19000, modelled after values for
		/// ExplosionBase ExplosionStrengths
		/// </summary>
		private void SetYieldTargets()
		{
			if (hardBlastYieldDetectorTarget == 0 || easyBlastYieldDetectorTarget == 0)
			{
				hardBlastYieldDetectorTarget = Random.Range(yieldTargetRangeMinimum, yieldTargetRangeMaximum);
				easyBlastYieldDetectorTarget = Random.Range(yieldTargetRangeMinimum, yieldTargetRangeMaximum);
			}
		}
		#endregion
	}
}