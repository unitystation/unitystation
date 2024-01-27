using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Systems.Faith
{
	public class FaithManager : SingletonManager<FaithManager>
	{
		public List<FaithData> CurrentFaiths { get; private set; } = new List<FaithData>();
		public float FaithEventsCheckTimeInSeconds = 390f;
		public float FaithPerodicCheckTimeInSeconds = 24f;
		public List<Action> FaithPropertiesEventUpdate { get; set; } = new List<Action>();
		public List<Action> FaithPropertiesConstantUpdate { get; set; } = new List<Action>();
		[field: SerializeField] public List<FaithSO> AllFaiths { get; private set; } = new List<FaithSO>();

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundEnded, ResetReligion);
			EventManager.AddHandler(Event.RoundStarted, SetupFaiths);
			Loggy.Log("[FaithManager/Awake] - Setting stuff.");
		}

		private void SetupFaiths()
		{
			if (CustomNetworkManager.IsServer == false) return;
			foreach (var faith in AllFaiths)
			{
				AddFaithToActiveList(faith.Faith);
			}
			UpdateManager.Add(LongUpdate, FaithEventsCheckTimeInSeconds);
			UpdateManager.Add(PeriodicUpdate, FaithPerodicCheckTimeInSeconds);
			Chat.AddGameWideSystemMsgToChat(
				$"Faiths have been setup successfully! {CurrentFaiths.Count} faiths are now active.".Color(Color.green));
		}

		private void ResetReligion()
		{
			Loggy.Log("[FaithManager/ResetReligion] - Resetting faiths.");
			CurrentFaiths.Clear();
			FaithPropertiesConstantUpdate.Clear();
			FaithPropertiesEventUpdate.Clear();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, LongUpdate);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			Chat.AddGameWideSystemMsgToChat($"Faiths have been reset. Awaiting new round.".Color(Color.blue));
		}

		private void LongUpdate()
		{
			Loggy.Log($"[FaithManager/LongUpdate] - Events check.");
			foreach (var update in FaithPropertiesEventUpdate)
			{
				update?.Invoke();
			}

			if (DMMath.Prob(35) == false) return;
			foreach (var faith in CurrentFaiths)
			{
				if (faith.Faith.FaithProperties.Count == 0) continue;
				if (faith.Points.IsBetween(-75, 75)) continue;
				faith.Faith.FaithProperties.PickRandom()?.RandomEvent();
			}
		}

		private void PeriodicUpdate()
		{
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
			if (CustomNetworkManager.IsServer == false)
			{
				Loggy.LogError("[FaithManager/AddFaithToActiveList] - Attempted to call a server function on the client.");
				return;
			}
			FaithData data = new FaithData()
			{
				Faith = faith,
				Points = 0,
				FaithLeaders = new List<PlayerScript>(),
				FaithMembers = new List<PlayerScript>(),
			};
			CurrentFaiths.Add(data);
			data.SetupFaith();
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