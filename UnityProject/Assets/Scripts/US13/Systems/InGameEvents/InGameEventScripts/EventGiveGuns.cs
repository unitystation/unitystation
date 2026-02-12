using Logs;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Player;
using US13.ScriptableObjects;
using US13.Strings;
using US13.Systems.Inventory;

namespace US13.Systems.InGameEvents.InGameEventScripts
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
				Loggy.Error($"No guns in gun list! Cannot spawn guns for {nameof(EventGiveGuns)}.", Category.Event);
				return;
			}

			foreach (PlayerInfo player in PlayerList.Instance.InGamePlayers)
			{
				if (player.Script.IsDeadOrGhost || player.Script.IsNormal == false) continue;

				HandlePlayer(player.Mind);
			}
		}

		protected virtual void HandlePlayer(Mind player)
		{
			GiveGunToPlayer(player);
		}

		protected void GiveGunToPlayer(Mind player)
		{
			GameObject gun = Spawn.ServerPrefab(gunList.GetRandom(),
						player.Body.WorldPos, player.Body.transform.parent, player.Body.transform.rotation).GameObject;

			ItemSlot slot = player.Body.DynamicItemStorage.GetBestHandOrSlotFor(gun);
			if (slot != null && slot.IsEmpty)
			{
				Inventory.Inventory.ServerAdd(gun, slot);
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
