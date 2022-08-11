using System;
using System.Collections;
using System.Collections.Generic;
using Items.Storage.VirtualStorage;
using Mirror;
using UnityEngine;
using Systems.Research.Data;
using Shared.Systems.ObjectConnection;
using Random = UnityEngine.Random;

namespace Systems.Research.Objects
{
	public class ResearchServer : NetworkBehaviour, IMultitoolMasterable
	{
		[SerializeField] private int researchPointsTrickle = 25;
		[SerializeField] private int TrickleTime = 60; //seconds

		private ItemStorage diskStorage;

		public List<string> AvailableDesigns = new List<string>();

		[NonSerialized] public Action<int,List<string>> TechWebUpdateEvent;
		//Keep a cached reference to the techweb so we dont spam the server with signal requests
		//Only send signals to the Research Server when issuing commands and changing values, not reading the data everytime we access it.
		private Techweb techweb = new Techweb();

		/// <summary>
		/// Used to hold reference to how many points have been awarded, by source.
		/// </summary>
		public Dictionary<string, int> PointTotalSourceList = new Dictionary<string, int>();

		private void Start()
		{
			diskStorage = GetComponent<ItemStorage>();
			if (diskStorage == null || diskStorage.GetTopOccupiedIndexedSlot() == null)
			{
				Logger.LogError("Research server spawned without a disk to hold data!");
				return;
			}

			if (diskStorage.GetTopOccupiedIndexedSlot().ItemObject.TryGetComponent<HardDriveBase>(out var disk))
			{
				var newTechwebFile = new TechwebFiles();
				newTechwebFile.Techweb = techweb;
				disk.AddDataToStorage(newTechwebFile);
			}
			else
			{
				Logger.LogError("Could not find correct disk to hold Techweb data!!");
				return;
			}

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
				techweb.AddResearchPoints(researchPointsTrickle);
			}
		}

		private void RemoveHardDisk()
		{
			if (diskStorage.GetTopOccupiedIndexedSlot().ItemObject.TryGetComponent<HardDriveBase>(out var disk))
			{
				Inventory.ServerDrop(disk.gameObject.PickupableOrNull().ItemSlot);
				techweb = null;
			}
		}

		private void AddHardDisk(ItemSlot disk)
		{
			if (disk.ItemObject.TryGetComponent<HardDriveBase>(out var hardDisk) == false) return;
			if (Inventory.ServerTransfer(disk, diskStorage.GetNextFreeIndexedSlot()))
			{
				//the techweb disk will only have one file so its fine if we just get the first ever one.
				//if for whatever reason it has more; it's going to be a bug thats not possible.
				if (hardDisk.DataOnStorage[0] is TechwebFiles c) techweb = c.Techweb;
			}
		}

		/// <summary>
		/// Awards points by the difference above tracked total. E.G. if the total points of a source is 35
		/// and if that source would attempt to award 40 points, then 5 points are added.
		/// Recommended for labs that are intended to have a point cap.
		/// </summary>
		/// <param name="source">Source of points, used as reference for tracking points.</param>
		/// <param name="points">Points to be checked against total.</param>
		public int AddResearchPointsDifference(ResearchPointMachine source, int points)
		{
			string sourceTypeName = source.GetType().Name;
			if (!PointTotalSourceList.ContainsKey(sourceTypeName))
			{
				PointTotalSourceList.Add(sourceTypeName, points);
				techweb.AddResearchPoints(points);
				Debug.Log($"Awarding {points.ToString()} pure points from {source}.");
				return points;
			}
			if (points > PointTotalSourceList[sourceTypeName])
			{
				int difference = points - PointTotalSourceList[sourceTypeName];
				techweb.AddResearchPoints(difference);
				PointTotalSourceList[sourceTypeName] = points;
				Debug.Log($"Awarding {difference.ToString()} points difference from {source}.");
				return difference;
			}
			return 0;
		}

		/// <summary>
		/// Adds extra untracked Research Points to the techWeb total, not tied to any source.
		/// </summary>
		/// <param name="points">The amount to be added.</param>
		public void AddResearchPoints(int points)
		{
			techweb.AddResearchPoints(points);
		}

		/// <summary>
		/// Adds Research Points to the techWeb total, tracked according to source.
		/// </summary>
		/// <param name="source">Machine type that's adding the points. </param>
		/// <param name="points">The amount to be added.</param>
		/// <returns></returns>
		public int AddResearchPoints(ResearchPointMachine source, int points)
		{
			string sourcename = source.GetType().Name;
			techweb.AddResearchPoints(points);
			PointTotalSourceList[sourcename] += points;
			return points;
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
		public int easyBlastYieldDetectorTarget = 0;
		/// <summary>
		/// Target ExplosionStrength required to award hard point amount, max amount
		/// </summary>
		public int hardBlastYieldDetectorTarget = 0;

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
		public void SetBlastYieldTargets()
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
