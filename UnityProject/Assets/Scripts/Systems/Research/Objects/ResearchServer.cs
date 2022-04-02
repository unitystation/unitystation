using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Communications;
using Items.Storage.VirtualStorage;
using Mirror;
using ScriptableObjects.Research;
using UnityEngine;
using Systems.Research.Data;
using Systems.ObjectConnection;

namespace Systems.Research.Objects
{
	public class ResearchServer : NetworkBehaviour, IMultitoolMasterable
	{
		//TODO: PLACE HOLDER UNTIL WE GET A TECHWEB EDITOR OF SOME SORT
		[SerializeField] private DefaultTechwebData defaultTechwebData;
		//TODO: PLACEHOLDER, TECHWEBS SHOULD BE STORED LOCALLY ON IN-GAME DISKS/CIRCUITS TO BE STOLEN AND MERGED
		private Techweb techweb = new Techweb();
		//TODO : PLACEHOLDER, THIS PATH MUST BE ASSIGNED ON THE CIRCUIT/DISK INSTEAD OF ON THE SERVER PREFAB
		[SerializeField] private string techWebPath = "/GameData/Research/";
		[SerializeField] private string techWebFileName = "TechwebData.json";
		[SerializeField] private int researchPointsTrickl = 25;
		[SerializeField] private int TrickleTime = 60; //seconds

		public List<string> AvailableDesigns = new List<string>();

		[NonSerialized] public Action<int,List<string>> TechWebUpdateEvent;

		private void Awake()
		{
			if (File.Exists($"{techWebPath}{techWebFileName}") == false) defaultTechwebData.GenerateDefaultData();
			techweb.LoadTechweb($"{techWebPath}{techWebFileName}");
			StartCoroutine(TrickleResources());
			UpdateAvailableDesigns();
		}

		private void Start()
		{
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
			}
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
		//TechWebUpdateEvent should be invoked with (1, UpdateAvailiableDesigns()) whenever a new dirve is added to a server

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
				if (hardDisk.DataOnStorage[0] is TechwebFiles c) techweb = c.Techweb;
			}
		}

		#region MultitoolInteraction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.ResearchServer;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion
	}
}