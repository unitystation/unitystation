using UnityEngine;
using Managers;
using Antagonists;
using Strings;
using System;
using System.Collections.Generic;

namespace SyndicateOps
{
	public class SyndicateOpConsole : MonoBehaviour
	{
		[NonSerialized] public static SyndicateOpConsole Instance;

		[NonSerialized] public int TcReserve;

		public int TcIncrement = 14;

		private bool warDeclared = false;
		private bool rewardGiven = false;
		[SerializeField] private int timer = 1200;

		[SerializeField] private int tcToGive = 280;

		[NonSerialized] public List<SpawnedAntag> Operatives = new List<SpawnedAntag>();

		public int Timer => timer;

		private void Awake()
		{
			if ( Instance == null )
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(ServerUpdateTimer, 1f);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateTimer);
			}
		}

		public void AnnounceWar(string declarationMessage)
		{
			if (warDeclared == false)
			{
				warDeclared = true;

				GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Red, true);
				CentComm.MakeAnnouncement(ChatTemplates.PriorityAnnouncement, 
				$"Attention all crew! An open message from the syndicate has been picked up on local radiowaves! Message Reads:\n" +
				$"{declarationMessage}" ,CentComm.UpdateSound.Alert);

				var antagPlayers = AntagManager.Instance.ActiveAntags;

				foreach (var antag in antagPlayers )
				{
					if (antag.Antagonist.AntagJobType == JobType.SYNDICATE)
					{
						Operatives.Add(antag);
					}
				}
			}
		}

		public void ServerUpdateTimer()
		{
			if (warDeclared == false || rewardGiven) return;

			if (timer > 0)
			{
				timer--;
			}

			if (timer % 60 == 0)
			{
				RewardTelecrystals();
			}
		}
		public void RewardTelecrystals()
		{
			if (tcToGive <= TcIncrement)
			{
				rewardGiven = true;
			}

			if (tcToGive >= TcIncrement)
			{	
				TcReserve += TcIncrement;
				tcToGive -= TcIncrement;
			}

			if (tcToGive < TcIncrement)
			{
				TcReserve += tcToGive;
				tcToGive = 0;
			}
		}
	}
}