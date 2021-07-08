using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using ScriptableObjects;
using Strings;

namespace InGameEvents
{
	public class EventGiveGuns : EventScriptBase
	{
		[SerializeField]
		private GameObjectList gunList = default;

		public override void OnEventStart()
		{
			if (!FakeEvent)
			{
				SpawnGuns();
			}

			base.OnEventStart();
		}

		protected void SpawnGuns()
		{
			if (gunList == null || gunList.GameObjectPrefabs.Length == 0)
			{
				Logger.LogError($"No guns in gun list! Cannot spawn guns for {nameof(EventGiveGuns)}.", Category.Event);
				return;
			}

			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
			{
				if (player.Script.IsDeadOrGhost) continue;

				HandlePlayer(player);
			}
		}

		protected virtual void HandlePlayer(ConnectedPlayer player)
		{
			GiveGunToPlayer(player);
		}

		protected void GiveGunToPlayer(ConnectedPlayer player)
		{
			GameObject gun = Spawn.ServerPrefab(gunList.GetRandom(),
						player.Script.WorldPos, player.Script.transform.parent, player.Script.transform.rotation).GameObject;

			ItemSlot slot = player.Script.DynamicItemStorage.GetBestHandOrSlotFor(gun);
			if (slot != null && slot.IsEmpty)
			{
				Inventory.ServerAdd(gun, slot);
			}
		}

		public override void OnEventEndTimed()
		{
			if (AnnounceEvent)
			{
				var text = "Incoming Report:\nA weapons convoy got caught in a blue space anomaly near your location. ";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}
		}
	}
}
