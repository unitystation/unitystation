using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventGiveGuns : EventScriptBase
	{
		[SerializeField]
		private List<GameObject> gunList = new List<GameObject>();

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
			if (gunList.Count == 0) return;

			foreach (var player in PlayerList.Instance.InGamePlayers)
			{
				if (player.Script.IsDeadOrGhost) continue;

				var slot = player.Script.Equipment.ItemStorage.GetActiveHandSlot();

				if (slot == null) continue;

				if (slot.Item == null)
				{
					var gun = Spawn.ServerPrefab(gunList[UnityEngine.Random.Range(0, gunList.Count)], player.Script.WorldPos, player.Script.gameObject.transform.parent, player.Script.transform.rotation);

					Inventory.ServerAdd(gun.GameObject.GetComponent<Pickupable>(), slot);
				}
				else
				{
					Spawn.ServerPrefab(gunList[UnityEngine.Random.Range(0, gunList.Count)], player.Script.WorldPos, player.Script.gameObject.transform.parent, player.Script.transform.rotation);
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