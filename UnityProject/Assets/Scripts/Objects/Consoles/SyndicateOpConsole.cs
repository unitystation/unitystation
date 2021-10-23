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

		[SerializeField] private int timer = 1200;

		[SerializeField] private int tcToGive = 280;

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

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RewardTelecrystals);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CountDown);
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
				UpdateManager.Add(RewardTelecrystals, 60);
				UpdateManager.Add(CountDown, 1);
			}
		}

		public void RewardTelecrystals()
		{
			var amount = Mathf.Min(TcIncrement, tcToGive);
			TcReserve += amount;
			tcToGive -= amount;
			if (tcToGive == 0)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RewardTelecrystals);
			}
		}
		public void CountDown()
		{
			timer -= 1;
			if(timer == 0)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CountDown);
			}
		}
	}
}