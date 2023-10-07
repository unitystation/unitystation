using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Items.Storage.VirtualStorage;
using Logs;
using Mirror;
using UnityEngine;
using Systems.Research.Data;
using Shared.Systems.ObjectConnection;
using Systems.Score;
using Random = UnityEngine.Random;

namespace Systems.Research.Objects
{
	public class ResearchServer : NetworkBehaviour, IMultitoolMasterable, IServerSpawn, IServerDespawn
	{
		public int RP => Techweb?.researchPoints ?? 0;

		[field: SerializeField, Header("Base functionality"), Space(10)] public int ResearchPointsTrickle { get; private set; } = 1;

		private ItemStorage diskStorage;
		[SerializeField] private GameObject techWebDisk;

		//Keep a cached reference to the techweb so we dont spam the server with signal requests
		//Only send signals to the Research Server when issuing commands and changing values, not reading the data everytime we access it.
		public Techweb Techweb { get; private set; } = new Techweb();

		/// <summary>
		/// Used to hold reference to how many points have been awarded, by source.
		/// </summary>
		public Dictionary<string, int> PointTotalSourceList = new Dictionary<string, int>();

		[Header("Ordnance"), Space(10)]

		[SerializeField] private ExplosiveBountySO explosiveBountyList = null;
		[SerializeField] private int bountiesOnStart = 10; //How many bounties will be generated on round start.

		public readonly SyncList<ExplosiveBounty> ExplosiveBounties = new SyncList<ExplosiveBounty>();

		[NonSerialized, SyncVar(hook = nameof(SyncFocus))] public int UIselectedFocus = 1; //The current Focus selected in menu, not nesscarily confirmed.
		[SerializeField] private RegisterTile registerTile;

		/// <summary>
		/// How many research points the techweb has acquired?
		/// </summary>
		public Action<int> ResearchPointsChanged;


		private void Awake()
		{
			registerTile ??= GetComponent<RegisterTile>();
			ResearchPointsChanged += TrackResearchPointsScore;
		}

		private void InitialiseDisk()
		{
			diskStorage = GetComponent<ItemStorage>();
			if (diskStorage == null || (diskStorage.GetIndexedItemSlot(0).Item == null && techWebDisk == null))
			{
				Loggy.LogError("Research server spawned without a disk to hold data!");
				return;
			}
			if(techWebDisk != null) diskStorage.ServerTrySpawnAndAdd(techWebDisk);

			if (diskStorage.GetIndexedItemSlot(0).ItemObject.TryGetComponent<HardDriveBase>(out var disk) == true)
			{

				string path = Path.Combine("TechWeb", "TechwebData.json");
				Techweb.LoadTechweb(path);

				var newTechwebFile = new TechwebFiles();
				newTechwebFile.Techweb = Techweb;
				disk.AddDataToStorage(newTechwebFile);
			}
			else
			{
				Loggy.LogError("Could not find correct disk to hold Techweb data!!");
				return;
			}

		}

		private void OnDisable()
		{
			StopCoroutine(TrickleResources());
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			Techweb.TechWebDesignUpdateEvent?.Invoke(0, null);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			InitialiseDisk();

			ExplosiveBounties.Clear();

			for(int i = 0; i < bountiesOnStart; i++)
			{
				AddRandomExplosiveBounty();
			}

			StartCoroutine(TrickleResources());

			Techweb.TechWebDesignUpdateEvent?.Invoke(1, Techweb.AvailableDesigns);
		}

		private IEnumerator TrickleResources()
		{
			while (this != null || Techweb != null)
			{
				yield return WaitFor.Minutes(1);
				Techweb.AddResearchPoints(ResearchPointsTrickle);
				Techweb.UIupdate?.Invoke();
			}
		}

		private void RemoveHardDisk()
		{
			if (diskStorage.GetTopOccupiedIndexedSlot().ItemObject.TryGetComponent<HardDriveBase>(out var disk))
			{
				Inventory.ServerDrop(disk.gameObject.PickupableOrNull().ItemSlot);
				Techweb = null;
				Techweb.TechWebDesignUpdateEvent?.Invoke(0, null);
			}
		}

		private void AddHardDisk(ItemSlot disk)
		{
			if (disk.ItemObject.TryGetComponent<HardDriveBase>(out var hardDisk) == false) return;
			if (Inventory.ServerTransfer(disk, diskStorage.GetNextFreeIndexedSlot()))
			{
				//the techweb disk will only have one file so its fine if we just get the first ever one.
				//if for whatever reason it has more; it's going to be a bug thats not possible.
				if (hardDisk.DataOnStorage[0] is TechwebFiles c) Techweb = c.Techweb;
				Techweb.TechWebDesignUpdateEvent?.Invoke(1, Techweb.AvailableDesigns);
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
				Techweb.AddResearchPoints(points);
				Debug.Log($"Awarding {points.ToString()} pure points from {source}.");
				return points;
			}
			if (points > PointTotalSourceList[sourceTypeName])
			{
				int difference = points - PointTotalSourceList[sourceTypeName];
				Techweb.AddResearchPoints(difference);
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
			Techweb?.AddResearchPoints(points);
			ResearchPointsChanged?.Invoke(points);
		}

		#region RightClickMethods

		[RightClickMethod()]
		public void AddFiveRP()
		{
			AddResearchPoints(5);
		}

		[RightClickMethod()]
		public void AddTenRP()
		{
			AddResearchPoints(10);
		}

		[RightClickMethod()]
		public void RemoveFiveRP()
		{
			Techweb?.SubtractResearchPoints(5);
		}

		[RightClickMethod()]
		public void RemoveTenRP()
		{
			Techweb?.SubtractResearchPoints(10);
		}

		#endregion


		/// <summary>
		/// Adds Research Points to the techWeb total, tracked according to source.
		/// </summary>
		/// <param name="source">Machine type that's adding the points. </param>
		/// <param name="points">The amount to be added.</param>
		/// <returns></returns>
		public int AddResearchPoints(ResearchPointMachine source, int points)
		{
			string sourcename = source.GetType().Name;
			if (PointTotalSourceList.ContainsKey(sourcename) == false)
			{
				PointTotalSourceList[sourcename] = 0;
			}

			Techweb.AddResearchPoints(points);
			PointTotalSourceList[sourcename] += points;
			ResearchPointsChanged?.Invoke(points);
			return points;
		}

		public bool AddArtifactIDtoTechWeb(string ID)
		{
			if (Techweb == null) return false;

			if (Techweb.researchedSliverIDs.Contains(ID)) return false;

			Techweb.researchedSliverIDs.Add(ID);

			return true;
		}

		#region MultitoolInteraction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.ResearchServer;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => false;
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
				bounty.RequiredYield.RequiredAmount = (int)Random.Range(bounty.RequiredYield.MinAmount, bounty.RequiredYield.MaxAmount);
			}

			foreach(ReagentBountyEntry reagentEntry in bounty.RequiredReagents)
			{
				if (reagentEntry.RandomiseRequirement == true)
				{
					reagentEntry.RequiredAmount = (int)Random.Range(reagentEntry.MinAmount, reagentEntry.MaxAmount);
				}
			}

			foreach(ReactionBountyEntry reactionEntry in bounty.RequiredReactions)
			{
				if (reactionEntry.RandomiseRequirement == true)
				{
					reactionEntry.RequiredAmount = (int)Random.Range(reactionEntry.MinAmount, reactionEntry.MaxAmount);
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
			Chat.AddActionMsgToChat(gameObject, $"Bounty completed, {BOUNTY_AWARD} points gained. Current RP: {Techweb?.researchPoints}.");
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

		[Command(requiresAuthority = false)]
		internal void CmdSetFocus(int FocusClient, NetworkConnectionToClient sender = null)
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;
			UIselectedFocus = FocusClient;
		}

		[Server]
		internal void SetFocusServer(int FocusServer)
		{
			UIselectedFocus = FocusServer;
		}

		private void SyncFocus(int oldData, int newData)
		{
			UIselectedFocus = newData;
			Techweb.UIupdate?.Invoke();
		}

		private void TrackResearchPointsScore(int newPoints)
		{
			if (registerTile.Matrix != MatrixManager.MainStationMatrix.Matrix) return;
			ScoreMachine.AddToScoreInt(newPoints, RoundEndScoreBuilder.COMMON_SCORE_SCIENCEPOINTS);
		}
	}
}
