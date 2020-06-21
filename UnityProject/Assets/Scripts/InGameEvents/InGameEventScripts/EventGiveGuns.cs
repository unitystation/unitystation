using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventGiveGuns : EventScriptBase
	{

		public List<GameObject> GunList = new List<GameObject>();

		public override void OnEventStart()
		{
			SpawnGuns();

			base.OnEventStart();
		}

		private void SpawnGuns()
		{
			if (GunList.Count == 0) return;

			foreach (var player in PlayerList.Instance.InGamePlayers)
			{
				if (player.Script.IsDeadOrGhost) continue;

				var slot = player.Script.Equipment.ItemStorage.GetActiveHandSlot();

				if (slot == null) continue;

				if (slot.Item == null)
				{
					var gun = Instantiate(GunList[UnityEngine.Random.Range(1, GunList.Count)], player.Script.gameObject.transform.parent);
					Inventory.ServerAdd(gun , slot);
				}
				else
				{
					Instantiate(GunList[UnityEngine.Random.Range(1, GunList.Count)], player.Script.WorldPos, player.Script.transform.rotation, player.Script.gameObject.transform.parent);
				}
			}
		}

		public override void OnEventEndTimed()
		{
			if (AnnounceEvent)
			{
				var text = "Why are the metal detectors going crazy?";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}
		}
	}
}