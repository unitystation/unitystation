using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using Managers;
using Strings;
using Systems.Cargo;
using Systems.Spawns;
using UnityEngine;

namespace Systems.Faith.Miracles
{
	public class GhostOfCarmenMiranda : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Call upon Space Station 3's ghost";
		[SerializeField] private string faithMiracleDesc = "The best odds say she likes the rhythms of the stations drive \n" +
		                                                   "They didn’t have phase generators while she was alive";
		[SerializeField] private SpriteDataSO miracleIcon;

		[SerializeField] private List<GameObject> ghosts = new List<GameObject>();
		[SerializeField] private List<GameObject> fruits = new List<GameObject>();
		[SerializeField] private List<GameObject> rum = new List<GameObject>();
		[SerializeField] private List<GameObject> tangerine = new List<GameObject>();
		[SerializeField] private SpawnPointCategory spawnPointCategory = SpawnPointCategory.MaintSpawns;

		string IFaithMiracle.FaithMiracleName
		{
			get => faithMiracleName;
			set => faithMiracleName = value;
		}

		string IFaithMiracle.FaithMiracleDesc
		{
			get => faithMiracleDesc;
			set => faithMiracleDesc = value;
		}

		SpriteDataSO IFaithMiracle.MiracleIcon
		{
			get => miracleIcon;
			set => miracleIcon = value;
		}

		public int MiracleCost { get; set; } = 200;
		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null)
		{
			GameManager.Instance.StartCoroutine(SongEvents());
		}

		private IEnumerator SongEvents()
		{
			yield return WaitFor.Seconds(12);
			foreach (var crewMember in PlayerList.Instance.InGamePlayers)
			{
				if (DMMath.Prob(50))
				{
					Chat.AddExamineMsg(crewMember.GameObject, "You feel like you've seen a feminine ghostly figure right around the corner..");
				}
				else
				{
					Spawn.ServerPrefab(rum.PickRandom(), crewMember.GameObject.AssumedWorldPosServer());
				}
			}
			yield return WaitFor.Seconds(14);
			foreach (var crewMember in PlayerList.Instance.InGamePlayers)
			{
				Spawn.ServerPrefab(fruits.PickRandom(), crewMember.GameObject.AssumedWorldPosServer());
			}

			yield return WaitFor.Seconds(10);
			if (CargoManager.Instance.ShuttleStatus is not ShuttleStatus.DockedStation)
			{
				foreach (var crewMember in PlayerList.Instance.InGamePlayers)
				{
					if (DMMath.Prob(65))
					{
						crewMember.Script.RegisterPlayer.ServerStun();
						crewMember.Script.playerHealth.IndicatePain(900, true);
						Chat.AddExamineMsg(crewMember.GameObject, "A sudden fear overwhelms you!");
					}
				}
			}
			yield return WaitFor.Seconds(5);
			foreach (var crewMember in PlayerList.Instance.InGamePlayers)
			{
				Spawn.ServerPrefab(tangerine.PickRandom(), crewMember.GameObject.AssumedWorldPosServer());
			}
			yield return WaitFor.Seconds(9);
			foreach (var crewMember in PlayerList.Instance.InGamePlayers)
			{
				if (DMMath.Prob(50))
				{
					Chat.AddExamineMsg(crewMember.GameObject, "You feel like you've seen a feminine ghostly figure right around the corner..");
				}
			}
			yield return WaitFor.Seconds(13);
			var randomCount = (int)Random.Range(2, 6);
			var spawnPoints = SpawnPoint.GetPointsForCategory(spawnPointCategory).ToList();
			for (int i = 0; i < randomCount; i++)
			{
				Spawn.ServerPrefab(ghosts.PickRandom(), spawnPoints.PickRandom().position);
			}
			yield return WaitFor.Seconds(4);
			CentComm.MakeAnnouncement(ChatTemplates.CommandNewReport, "We've received reports of haunting activities that were first reported on Space Station 3. " +
			                                                          "We'd like to remind all crew members that pool bets are illegal " +
			                                                          "under some sectors due to it being considered gambling.",
				CentComm.UpdateSound.Alert);
			yield return WaitFor.Seconds(10);
			foreach (var crewMember in PlayerList.Instance.InGamePlayers)
			{
				Spawn.ServerPrefab(fruits[0], crewMember.GameObject.AssumedWorldPosServer());
			}
		}
	}
}