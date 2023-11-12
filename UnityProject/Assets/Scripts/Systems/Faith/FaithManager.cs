using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine;

namespace Systems.Faith
{
	public class FaithManager : SingletonManager<FaithManager>
	{
		public Faith DefaultFaith;
		public List<FaithData> CurrentFaiths { get; private set; } = new List<FaithData>();
		public float FaithEventsCheckTimeInSeconds = 390f;
		public float FaithPerodicCheckTimeInSeconds = 12f;
		public List<Action> FaithPropertiesEventUpdate { get; set; } = new List<Action>();
		public List<Action> FaithPropertiesConstantUpdate { get; set; } = new List<Action>();
		[field: SerializeField] public List<FaithSO> AllFaiths { get; private set; } = new List<FaithSO>();

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundEnded, ResetReligion);
			UpdateManager.Add(LongUpdate, FaithEventsCheckTimeInSeconds);
			UpdateManager.Add(PeriodicUpdate, FaithPerodicCheckTimeInSeconds);
			Loggy.Log("[FaithManager/Awake] - Setting stuff.");
		}

		private void ResetReligion()
		{
			Loggy.Log("[FaithManager/ResetReligion] - Resetting faiths.");
			var defaultFaith = new FaithData()
			{
				Faith = DefaultFaith,
				Points = 0,
				FaithLeaders = new List<PlayerScript>(),
				FaithMembers = new List<PlayerScript>(),
			};
			CurrentFaiths.Add(defaultFaith);
			FaithPropertiesConstantUpdate.Clear();
			FaithPropertiesEventUpdate.Clear();
		}

		private void LongUpdate()
		{
			if (CustomNetworkManager.IsServer == false) return;

			foreach (var update in FaithPropertiesEventUpdate)
			{
				update?.Invoke();
			}

			if (DMMath.Prob(35) == false) return;
			foreach (var faith in CurrentFaiths)
			{
				if (faith.FaithMembers.Count == 0) continue;
				if (faith.Faith.FaithProperties.Count == 0) continue;
				if (faith.Points.IsBetween(-250, 250)) continue;
				faith.Faith.FaithProperties.PickRandom()?.RandomEvent();
			}
		}

		private void PeriodicUpdate()
		{
			if (CustomNetworkManager.IsServer == false) return;
			foreach (var property in FaithPropertiesConstantUpdate)
			{
				property?.Invoke();
			}
		}

		public static void AwardPoints(int points, string faithName)
		{
			foreach (var faith in Instance.CurrentFaiths.Where(faith => faith.Faith.FaithName == faithName))
			{
				faith.Points += points;
			}
		}

		public static void TakePoints(int points, string faithName)
		{
			foreach (var faith in Instance.CurrentFaiths.Where(faith => faith.Faith.FaithName == faithName))
			{
				faith.Points -= points;
			}
		}

		public static void AddLeaderToFaith(string targetFaith, PlayerScript newLeader)
		{
			foreach (var faith in Instance.CurrentFaiths.Where(faith => faith.Faith.FaithName == targetFaith))
			{
				faith.AddMember(newLeader);
				faith.FaithLeaders.Add(newLeader);
			}
		}

		public void AddFaithToActiveList(Faith faith)
		{
			if (CustomNetworkManager.IsServer == false) return;
			FaithData data = new FaithData()
			{
				Faith = faith,
				Points = 0,
				FaithLeaders = new List<PlayerScript>(),
				FaithMembers = new List<PlayerScript>(),
			};
			CurrentFaiths.Add(data);
			foreach (var property in faith.FaithProperties)
			{
				property.Setup(data);
			}
		}

		public static void JoinFaith(Faith faith, PlayerScript player)
		{
			foreach (var faithData in Instance.CurrentFaiths.Where(x => x.FaithMembers.Contains(player)))
			{
				faithData.AddMember(player);
			}
		}

		public static void LeaveFaith(PlayerScript playerScript)
		{
			foreach (var faith in Instance.CurrentFaiths.Where(faith => faith.FaithMembers.Contains(playerScript)))
			{
				faith.RemoveMember(playerScript);
			}
		}

		public static List<PlayerScript> GetAllMembersOfFaith(string faith)
		{
			var result = new List<PlayerScript>();
			foreach (var f in Instance.CurrentFaiths.Where(f => f.Faith.FaithName == faith))
			{
				result.AddRange(f.FaithMembers);
			}
			return result;
		}

		public static int GetPointsOfFaith(string faith)
		{
			var result = 0;
			foreach (var f in Instance.CurrentFaiths.Where(f => f.Faith.FaithName == faith))
			{
				result += f.Points;
				break;
			}
			return result;
		}
	}
}