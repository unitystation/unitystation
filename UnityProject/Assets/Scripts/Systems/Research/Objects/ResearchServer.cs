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
	public class ResearchServer : NetworkBehaviour, IMultitoolMasterable, IServerSpawn, IServerDespawn
	{
		public int RP {
			get
			{
				if (techweb == null) return 0;
				else return techweb.researchPoints;
			}
		}

		[Header("Base functionality"), Space(10)]

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

		[Header("Ordnance"), Space(10)]

		[SerializeField] private ExplosiveBountySO explosiveBountyList = null;
		[SerializeField] private int bountiesOnStart = 10; //How many bounties will be generated on round start.

		public List<ExplosiveBounty> ExplosiveBounties = new List<ExplosiveBounty>();

		private void Start()
		{
			diskStorage = GetComponent<ItemStorage>();
			if (diskStorage == null || diskStorage.GetIndexedItemSlot(0).Item == null)
			{
				Logger.LogError("Research server spawned without a disk to hold data!");
				return;
			}

			if (diskStorage.GetIndexedItemSlot(0).Item.TryGetComponent<HardDriveBase>(out var disk))
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
			for(int i = 0; i < bountiesOnStart; i++)
			{
				AddRandomExplosiveBounty();
			}

			StartCoroutine(TrickleResources());
			UpdateAvailableDesigns();

			TechWebUpdateEvent?.Invoke(1, AvailableDesigns);

		}

		//TODO
		//Once Techweb is fully implemented:
		//TechWebUpdateEvent should be invoked with (1, UpdateAvailiableDesigns()) whenever a new node is researched

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
				TechWebUpdateEvent?.Invoke(0, null);
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
				TechWebUpdateEvent?.Invoke(1, AvailableDesigns);
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

		public bool AddArtifactIDtoTechWeb(string ID)
		{
			if (techweb == null) return false;

			if (techweb.researchedSliverIDs.Contains(ID)) return false;

			techweb.researchedSliverIDs.Add(ID);

			return true;
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
		/// Randomises values on a bounty if it is set to be randomised
		/// </summary>
		private ExplosiveBounty RandomiseBountyTarget(ExplosiveBounty bounty)
		{
			if(bounty.RequiredYield.RandomiseRequirement == true)
			{
				bounty.RequiredYield.requiredAmount = (int)Random.Range(bounty.RequiredYield.MinAmount, bounty.RequiredYield.MaxAmount);
			}

			foreach(ReagentBountyEntry reagentEntry in bounty.RequiredReagents)
			{
				if (reagentEntry.RandomiseRequirement == true)
				{
					reagentEntry.requiredAmount = (int)Random.Range(reagentEntry.MinAmount, reagentEntry.MaxAmount);
				}
			}

			foreach(ReactionBountyEntry reactionEntry in bounty.RequiredReactions)
			{
				if (reactionEntry.RandomiseRequirement == true)
				{
					reactionEntry.requiredAmount = (int)Random.Range(reactionEntry.MinAmount, reactionEntry.MaxAmount);
				}
			}

			return bounty;		
		}

		/// <summary>
		/// The RP awarded for completing an explosive bounty.
		/// </summary>
		private const int BOUNTY_AWARD = 15;

		/// <summary>
		/// Marks an explosive bounty as complete and awards RP for its completion.
		/// </summary>
		/// <param name="bountyToComplete">The bounty to be marked as completed</param>
		public void CompleteBounty(ExplosiveBounty bountyToComplete)
		{
			AddResearchPoints(BOUNTY_AWARD);
			Chat.AddLocalMsgToChat($"Bounty completed, {BOUNTY_AWARD} points gained. Current RP: {techweb?.researchPoints}", gameObject);
			ExplosiveBounties.Remove(bountyToComplete);
		}

		/// <summary>
		/// Adds a random bounty to this servers bounties
		/// </summary>
		public void AddRandomExplosiveBounty()
		{
			var newBounty = Instantiate(explosiveBountyList.PossibleBounties.PickRandom()); //Instantiates the SO, this is so when we edit the values of one bounty for RNG, it doesnt share amongst all bounties of same type.

			newBounty = RandomiseBountyTarget(newBounty);

			ExplosiveBounties.Add(newBounty);
		}

		#endregion
	}
}
