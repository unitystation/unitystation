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
			if (!FakeEvent)
			{
				SpawnGuns();
			}

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
					var gun = Spawn.ServerPrefab(GunList[UnityEngine.Random.Range(1, GunList.Count)], player.Script.WorldPos, player.Script.gameObject.transform.parent, player.Script.transform.rotation);

					Inventory.ServerAdd(gun.GameObject.GetComponent<Pickupable>(), slot);
				}
				else
				{
					Spawn.ServerPrefab(GunList[UnityEngine.Random.Range(1, GunList.Count)], player.Script.WorldPos, player.Script.gameObject.transform.parent, player.Script.transform.rotation);
				}
			}
		}

		public override void OnEventEndTimed()
		{
			if (AnnounceEvent)
			{
				var text = "Incoming Report:\nA weapons convoy got caught in a blue space anomaly near your location. ";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}
		}
	}
}