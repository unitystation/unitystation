using System;
using System.Collections.Generic;
using Logs;
using Shared.Managers;
using UnityEngine;

namespace Systems.Faith
{
	public class FaithManager : SingletonManager<FaithManager>
	{
		public Faith DefaultFaith;
		public Faith CurrentFaith { get; private set; }
		public int FaithPoints { get; private set; }
		public float FaithEventsCheckTimeInSeconds = 390f;
		public float FaithPerodicCheckTimeInSeconds = 12f;
		public List<PlayerScript> FaithMembers { get; private set; } = new List<PlayerScript>();
		public List<PlayerScript> FaithLeaders { get; private set; } = new List<PlayerScript>();
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
			CurrentFaith = DefaultFaith;
			FaithPoints = 0;
			FaithLeaders.Clear();
			FaithMembers.Clear();
			FaithPropertiesConstantUpdate.Clear();
			FaithPropertiesEventUpdate.Clear();
		}

		private void LongUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;
			foreach (var update in FaithPropertiesEventUpdate)
			{
				update?.Invoke();
			}
			//disabled for now until point shops/gameplay are properly implemented in the next chaplain expanded PR
			//go away codacey, shoo. if (FaithPoints.IsBetween(-500, 500) && Application.isEditor == false) return;

			if (DMMath.Prob(35))
			{
				CurrentFaith.FaithProperties.PickRandom()?.RandomEvent();
			}

			CheckTolerance();
		}

		private void PeriodicUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;
			foreach (var property in FaithPropertiesConstantUpdate)
			{
				property?.Invoke();
			}
		}

		private void CheckTolerance()
		{
			if (FaithMembers.Count < 3 || CurrentFaith.ToleranceToOtherFaiths is ToleranceToOtherFaiths.Accepting) return;
			//TODO: Logic me up daddy uwu
		}

		public static void AwardPoints(int points)
		{
			Instance.FaithPoints += points;
		}

		public static void TakePoints(int points)
		{
			Instance.FaithPoints -= points;
		}

		public void SetMainFaith(Faith faith)
		{
			if (CustomNetworkManager.IsServer == false) return;
			CurrentFaith = faith;
			foreach (var property in CurrentFaith.FaithProperties)
			{
				property.Setup();
			}
		}
	}
}